#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Shinden;
using Z.EntityFramework.Plus;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class WaifuController : ControllerBase
    {
        private readonly Waifu _waifu;
        private readonly IConfig _config;
        private readonly IExecutor _executor;
        private readonly ShindenClient _shClient;
        private readonly Database.UserContext _dbUserContext;

        public WaifuController(Database.UserContext dbUserContext, ShindenClient shClient, Waifu waifu, IExecutor executor, IConfig config)
        {
            _waifu = waifu;
            _config = config;
            _executor = executor;
            _shClient = shClient;
            _dbUserContext = dbUserContext;
        }

        /// <summary>
        /// Pobiera użytkowników posiadających karte postaci
        /// </summary>
        /// <param name="id">id postaci z bazy shindena</param>
        /// <returns>lista id</returns>
        [HttpGet("users/owning/character/{id}"), Authorize]
        public async Task<IActionResult> GetUsersOwningCharacterCardAsync(ulong id)
        {
            var shindenIds = await _dbUserContext.Cards.Include(x => x.GameDeck).ThenInclude(x => x.User)
                .Where(x => x.Character == id && x.GameDeck.User.Shinden != 0).Select(x => x.GameDeck.User.Shinden).Distinct().ToListAsync();

            if (shindenIds.Count > 0)
                return Ok(shindenIds);

            return "User not found".ToResponse(404);
        }

        /// <summary>
        /// Pobiera liste kart użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>lista kart</returns>
        /// <response code="404">User not found</response>
        [HttpGet("user/{id}/cards"), Authorize]
        public async Task<IEnumerable<Database.Models.Card>> GetUserCardsAsync(ulong id)
        {
            var user = await _dbUserContext.Users.Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).FirstOrDefaultAsync(x => x.Shinden == id);
            if (user == null)
            {
                await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return new List<Database.Models.Card>();
            }

            return user.GameDeck.Cards;
        }

        /// <summary>
        /// Zastępuje id postaci w kartach
        /// </summary>
        /// <param name="oldId">id postaci z bazy shindena, która została usunięta</param>
        /// <param name="newId">id nowej postaci z bazy shindena</param>
        /// <response code="500">New character ID is invalid!</response>
        [HttpPost("character/repair/{oldId}/{newId}"), Authorize]
        public async Task RepairCardsAsync(ulong oldId, ulong newId)
        {
            var response = await _shClient.GetCharacterInfoAsync(newId);
            if (!response.IsSuccessStatusCode())
            {
                await "New character ID is invalid!".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var exe = new Executable(new Task<bool>(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var cards = db.Cards.Where(x => x.Character == oldId);
                    foreach (var card in cards)
                    {
                        card.Character = newId;
                    }

                    db.SaveChanges();

                    QueryCacheManager.ExpireTag(new string[] { "users" });
                    return true;
                }
            }));

            _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
            await "Success".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Generuje na nowo karty danej postaci
        /// </summary>
        /// <param name="id">id postaci z bazy shindena</param>
        /// <response code="404">Cards not found</response>
        [HttpPost("users/make/character/{id}"), Authorize]
        public async Task GenerateCharacterCardAsync(ulong id)
        {
            var cards = await _dbUserContext.Cards.Where(x => x.Character == id).ToListAsync();
            if (cards.Count < 1)
            {
                await "Cards not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return;
            }

            _ = Task.Run(async () =>
            {
                foreach (var card in cards)
                {
                    _ = await _waifu.GenerateAndSaveCardAsync(card);
                }
            });

            await "Started!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Wymusza na bocie wygenerowanie obrazka jeśli nie istnieje
        /// </summary>
        /// <param name="id">id karty (wid)</param>
        /// <response code="403">Card already exist</response>
        /// <response code="404">Card not found</response>
        [HttpGet("card/{id}")]
        public async Task GetCardAsync(ulong id)
        {
            if (!System.IO.File.Exists($"./GOut/Cards/Small/{id}.png") || !System.IO.File.Exists($"./GOut/Cards/{id}.png"))
            {
                var card = await _dbUserContext.Cards.FirstOrDefaultAsync(x => x.Id == id);
                if (card == null)
                {
                    await "Card not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var cardImage = await _waifu.GenerateAndSaveCardAsync(card);
                await ControllerContext.HttpContext.Response.SendFileAsync(cardImage);
            }
            else
            {
                await "Card already exist!".ToResponse(403).ExecuteResultAsync(ControllerContext);
            }
        }

        /// <summary>
        /// Daje użytkownikowi pakiet kart (wymagany Bearer od użytkownika)
        /// </summary>
        /// <param name="boosterPack">model pakietu</param>
        /// <response code="403">The appropriate claim was not found</response>
        /// <response code="404">User not found</response>
        /// <response code="405">This user isn't a player</response>
        /// <response code="500">Model is Invalid</response>
        [HttpPost("boosterpack"), Authorize(Policy = "Player")]
        public async Task GiveUserAPack([FromBody]Models.CardBoosterPack boosterPack)
        {
            if (boosterPack == null)
            {
                await "Model is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var pack = boosterPack.ToRealPack();
            if (pack == null)
            {
                await "Data is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var currUser = ControllerContext.HttpContext.User;
            if (currUser.HasClaim(x => x.Type == "DiscordId"))
            {
                if (ulong.TryParse(currUser.Claims.First(x => x.Type == "DiscordId").Value, out var discordId))
                {
                    var exe = new Executable(new Task<bool>(() =>
                    {
                        using (var db = new Database.UserContext(_config))
                        {
                            var botUser = db.GetUserOrCreateAsync(discordId).Result;
                            botUser.GameDeck.BoosterPacks.Add(pack);

                            db.SaveChanges();

                            QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                            return true;
                        }
                    }));

                    _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                    await "Booster pack added!".ToResponse(200).ExecuteResultAsync(ControllerContext);
                    return;
                }
            }
            await "The appropriate claim was not found".ToResponse(403).ExecuteResultAsync(ControllerContext);
        }
    }
}