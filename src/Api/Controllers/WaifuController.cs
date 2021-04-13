#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanakan.Api.Models;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Shinden;
using Z.EntityFramework.Plus;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaifuController : ControllerBase
    {
        private readonly Waifu _waifu;
        private readonly IConfig _config;
        private readonly IExecutor _executor;
        private readonly ShindenClient _shClient;

        public WaifuController(ShindenClient shClient, Waifu waifu, IExecutor executor, IConfig config)
        {
            _waifu = waifu;
            _config = config;
            _executor = executor;
            _shClient = shClient;
        }

        /// <summary>
        /// Pobiera użytkowników posiadających karte postaci
        /// </summary>
        /// <param name="id">id postaci z bazy shindena</param>
        /// <returns>lista id</returns>
        /// <response code="404">Users not found</response>
        [HttpGet("users/owning/character/{id}"), Authorize(Policy = "Site")]
        public async Task<IEnumerable<ulong>> GetUsersOwningCharacterCardAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var shindenIds = await db.Cards.Include(x => x.GameDeck).ThenInclude(x => x.User)
                    .Where(x => x.Character == id && x.GameDeck.User.Shinden != 0).AsNoTracking().Select(x => x.GameDeck.User.Shinden).Distinct().ToListAsync();

                if (shindenIds.Count > 0)
                    return shindenIds;

                await "Users not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return null;
            }
        }

        /// <summary>
        /// Pobiera liste kart użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>lista kart</returns>
        /// <response code="404">User not found</response>
        [HttpGet("user/{id}/cards"), Authorize(Policy = "Site")]
        public async Task<IEnumerable<Database.Models.Card>> GetUserCardsAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.Users.AsQueryable().Where(x => x.Shinden == id).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                    .ThenInclude(x => x.Cards).ThenInclude(x => x.TagList).AsNoTracking().AsSplitQuery().FirstOrDefaultAsync();

                if (user == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return new List<Database.Models.Card>();
                }

                return user.GameDeck.Cards;
            }
        }

        /// <summary>
        /// Pobiera x kart z przefiltrowanej listy użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <param name="offset">przesunięcie</param>
        /// <param name="count">liczba kart</param>
        /// <param name="filter">filtry listy</param>
        /// <returns>lista kart</returns>
        /// <response code="404">User not found</response>
        [HttpPost("user/{id}/cards/{offset}/{count}")]
        public async Task<FilteredCards> GetUsersCardsByShindenIdWithOffsetAndFilterAsync(ulong id, uint offset, uint count, [FromBody]CardsQueryFilter filter)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.Users.AsQueryable().Where(x => x.Shinden == id).Include(x => x.GameDeck).AsNoTracking().AsSplitQuery().FirstOrDefaultAsync();

                if (user == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return new FilteredCards{TotalCards = 0, Cards = new List<CardFinalView>()};
                }

                var query = db.Cards.AsQueryable().AsSplitQuery().Where(x => x.GameDeckId == user.GameDeck.Id).Include(x=> x.ArenaStats).Include(x => x.TagList).AsNoTracking();

                if (!string.IsNullOrEmpty(filter.SearchText))
                {
                    query = query.Where(x => x.Name.Contains(filter.SearchText) || x.Title.Contains(filter.SearchText));
                }

                query = CardsQueryFilter.Use(filter.OrderBy, query);

                var cards = await query.ToListAsync();
                if (filter.IncludeTags != null)
                {
                    foreach (var iTag in filter.IncludeTags)
                        cards = cards.Where(x => x.HasTag(iTag)).ToList();
                }

                if (filter.ExcludeTags != null)
                {
                    foreach (var eTag in filter.ExcludeTags)
                        cards = cards.Where(x => !x.HasTag(eTag)).ToList();
                }

                return new FilteredCards{TotalCards = cards.Count, Cards = cards.Skip((int)offset).Take((int)count).ToView()};
            }
        }

        /// <summary>
        /// Pobiera x kart z listy użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <param name="offset">przesunięcie</param>
        /// <param name="count">liczba kart</param>
        /// <returns>lista kart</returns>
        /// <response code="404">User not found</response>
        [HttpGet("user/{id}/cards/{offset}/{count}")]
        public async Task<IEnumerable<CardFinalView>> GetUsersCardsByShindenIdWithOffsetAsync(ulong id, uint offset, uint count)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.Users.AsQueryable().AsSplitQuery().Where(x => x.Shinden == id).Include(x => x.GameDeck).AsNoTracking().FirstOrDefaultAsync();

                if (user == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return new List<CardFinalView>();
                }

                var cards = await db.Cards.AsQueryable().AsSplitQuery().Where(x => x.GameDeckId == user.GameDeck.Id).Include(x=> x.ArenaStats).Include(x => x.TagList).Skip((int)offset).Take((int)count).AsNoTracking().ToListAsync();
                return cards.ToView();
            }
        }

        /// <summary>
        /// Pobiera surową listę życzeń użtykownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>lista życzeń</returns>
        /// <response code="404">User not found</response>
        /// <response code="401">User wishlist is private</response>
        [HttpGet("user/shinden/{id}/wishlist/raw")]
        public async Task<IEnumerable<WishlistObject>> GetUsersRawWishlistByShindenIdAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.Users.AsQueryable().AsSplitQuery().Where(x => x.Shinden == id).Include(x => x.GameDeck).ThenInclude(x => x.Wishes).AsNoTracking().FirstOrDefaultAsync();

                if (user == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return new List<WishlistObject>();
                }

                if (user.GameDeck.WishlistIsPrivate)
                {
                    await "User wishlist is private".ToResponse(401).ExecuteResultAsync(ControllerContext);
                    return new List<WishlistObject>();
                }

                return user.GameDeck.Wishes;
            }
        }

        /// <summary>
        /// Pobiera profil użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <returns>profil</returns>
        /// <response code="404">User not found</response>
        [HttpGet("user/{id}/profile")]
        public async Task<UserSiteProfile> GetUserWaifuProfileAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.Users.AsQueryable().AsSplitQuery().Where(x => x.Shinden == id).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                    .ThenInclude(x => x.Cards).ThenInclude(x => x.TagList).AsNoTracking().FirstOrDefaultAsync();

                if (user == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return new UserSiteProfile();
                }

                var tagList = new List<string>();
                var tags = user.GameDeck.Cards.Where(x => x.TagList != null).Select(x => x.TagList.Select(c => c.Name));
                foreach(var tag in tags) tagList.AddRange(tag);

                var cardCount = new Dictionary<string, long>
                {
                    {Rarity.SSS.ToString(), user.GameDeck.Cards.Count(x => x.Rarity == Rarity.SSS)},
                    {Rarity.SS.ToString(),  user.GameDeck.Cards.Count(x => x.Rarity == Rarity.SS)},
                    {Rarity.S.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.S)},
                    {Rarity.A.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.A)},
                    {Rarity.B.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.B)},
                    {Rarity.C.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.C)},
                    {Rarity.D.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.D)},
                    {Rarity.E.ToString(),   user.GameDeck.Cards.Count(x => x.Rarity == Rarity.E)},
                    {"max",                 user.GameDeck.MaxNumberOfCards},
                    {"total",               user.GameDeck.Cards.Count}
                };

                var wallet = new Dictionary<string, long>
                {
                    {"PC", user.GameDeck.PVPCoins},
                    {"CT", user.GameDeck.CTCnt},
                    {"AC", user.AcCnt},
                    {"TC", user.TcCnt},
                    {"SC", user.ScCnt},
                };

                return new UserSiteProfile()
                {
                    Wallet = wallet,
                    CardsCount = cardCount,
                    Karma = user.GameDeck.Karma,
                    TagList = tagList.Distinct().ToList(),
                    UserTitle = user.GameDeck.GetUserNameStatus(),
                    ForegroundColor = user.GameDeck.ForegroundColor,
                    ForegroundPosition = user.GameDeck.ForegroundPosition,
                    BackgroundPosition = user.GameDeck.BackgroundPosition,
                    ExchangeConditions = user.GameDeck.ExchangeConditions,
                    BackgroundImageUrl = user.GameDeck.BackgroundImageUrl,
                    ForegroundImageUrl = user.GameDeck.ForegroundImageUrl,
                    Expeditions = user.GameDeck.Cards.Where(x => x.Expedition != CardExpedition.None).ToExpeditionView(user.GameDeck.Karma),
                    Waifu = user.GameDeck.Cards.Where(x => x.Character == user.GameDeck.Waifu).OrderBy(x => x.Rarity).ThenByDescending(x => x.Quality).FirstOrDefault().ToView(),
                    Gallery = user.GameDeck.Cards.Where(x => x.HasTag("galeria")).Take(user.GameDeck.CardsInGallery).OrderBy(x => x.Rarity).ThenByDescending(x => x.Quality).ToView()
                };
            }
        }

        /// <summary>
        /// Zastępuje id postaci w kartach
        /// </summary>
        /// <param name="oldId">id postaci z bazy shindena, która została usunięta</param>
        /// <param name="newId">id nowej postaci z bazy shindena</param>
        /// <response code="500">New character ID is invalid!</response>
        [HttpPost("character/repair/{oldId}/{newId}"), Authorize(Policy = "Site")]
        public async Task RepairCardsAsync(ulong oldId, ulong newId)
        {
            var response = await _shClient.GetCharacterInfoAsync(newId);
            if (!response.IsSuccessStatusCode())
            {
                await "New character ID is invalid!".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var exe = new Executable($"api-repair oc{oldId} c{newId}", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var userRelease = new List<string>() { "users" };
                    var cards = db.Cards.AsQueryable().AsSplitQuery().Where(x => x.Character == oldId);

                    foreach (var card in cards)
                    {
                        card.Character = newId;
                        userRelease.Add($"user-{card.GameDeckId}");
                    }

                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(userRelease.ToArray());
                }
            }), Priority.High);

            await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
            await "Success".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Podmienia dane na karcie danej postaci
        /// </summary>
        /// <param name="id">id postaci z bazy shindena</param>
        /// <param name="newData">nowe dane karty</param>
        [HttpPost("cards/character/{id}/update"), Authorize(Policy = "Site")]
        public async Task UpdateCardInfoAsync(ulong id, [FromBody]Models.CharacterCardInfoUpdate newData)
        {
            var exe = new Executable($"update cards-{id} img", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var userRelease = new List<string>() { "users" };
                    var cards = db.Cards.AsQueryable().AsSplitQuery().Where(x => x.Character == id);

                    foreach (var card in cards)
                    {
                        if (newData?.ImageUrl != null)
                            card.Image = newData.ImageUrl;

                        if (newData?.CharacterName != null)
                            card.Name = newData.CharacterName;

                        if (newData?.CardSeriesTitle != null)
                            card.Title = newData.CardSeriesTitle;

                        try
                        {
                            _waifu.DeleteCardImageIfExist(card);
                            _ = _waifu.GenerateAndSaveCardAsync(card).Result;
                        }
                        catch (Exception) { }

                        userRelease.Add($"user-{card.GameDeckId}");
                    }

                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(userRelease.ToArray());
                }
            }), Priority.High);

            await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
            await "Started!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Generuje na nowo karty danej postaci
        /// </summary>
        /// <param name="id">id postaci z bazy shindena</param>
        /// <response code="404">Character not found</response>
        /// <response code="405">Image in character date not found</response>
        [HttpPost("users/make/character/{id}"), Authorize(Policy = "Site")]
        public async Task GenerateCharacterCardAsync(ulong id)
        {
            var response = await _shClient.GetCharacterInfoAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await "Character not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                return;
            }

            if (!response.Body.HasImage)
            {
                await "There is no character image!".ToResponse(405).ExecuteResultAsync(ControllerContext);
                return;
            }

            var exe = new Executable($"update cards-{id}", new Task<Task>(async () =>
                {
                    using (var db = new Database.UserContext(_config))
                    {
                        var userRelease = new List<string>() { "users" };
                        var cards = db.Cards.AsQueryable().AsSplitQuery().Where(x => x.Character == id);

                        foreach (var card in cards)
                        {
                            card.Image = response.Body.PictureUrl;

                            try
                            {
                                _waifu.DeleteCardImageIfExist(card);
                                _ = _waifu.GenerateAndSaveCardAsync(card).Result;
                            }
                            catch (Exception) { }

                            userRelease.Add($"user-{card.GameDeckId}");
                        }

                        await db.SaveChangesAsync();

                        QueryCacheManager.ExpireTag(userRelease.ToArray());
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                await "Started!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Pobiera listę życzeń użytkownika
        /// </summary>
        /// <param name="id">id użytkownika discorda</param>
        /// <response code="404">User not found</response>
        [HttpGet("user/discord/{id}/wishlist"), Authorize(Policy = "Site")]
        public async Task<IEnumerable<Database.Models.Card>> GetUserWishlistAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.GetCachedFullUserAsync(id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                if (user.GameDeck.Wishes.Count < 1)
                {
                    await "Wishlist not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                var p = user.GameDeck.GetCharactersWishList();
                var t = user.GameDeck.GetTitlesWishList();
                var c = user.GameDeck.GetCardsWishList();

                return await _waifu.GetCardsFromWishlist(c, p ,t, db, user.GameDeck.Cards);
            }
        }

        /// <summary>
        /// Pobiera listę życzeń użytkownika
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <response code="404">User not found</response>
        [HttpGet("user/shinden/{id}/wishlist"), Authorize(Policy = "Site")]
        public async Task<IEnumerable<Database.Models.Card>> GetShindenUserWishlistAsync(ulong id)
        {
            using (var db = new Database.UserContext(_config))
            {
                var user = await db.GetCachedFullUserByShindenIdAsync(id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                if (user.GameDeck.Wishes.Count < 1)
                {
                    await "Wishlist not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                var p = user.GameDeck.GetCharactersWishList();
                var t = user.GameDeck.GetTitlesWishList();
                var c = user.GameDeck.GetCardsWishList();

                return await _waifu.GetCardsFromWishlist(c, p ,t, db, user.GameDeck.Cards);
            }
        }

        /// <summary>
        /// Pobiera liste kart z danym tagiem
        /// </summary>
        /// <param name="tag">tag na karcie</param>
        [HttpGet("cards/tag/{tag}"), Authorize(Policy = "Site")]
        public async Task<IEnumerable<Database.Models.Card>> GetCardsWithTagAsync(string tag)
        {
            using (var db = new Database.UserContext(_config))
            {
                return await db.Cards.Include(x => x.ArenaStats).Include(x => x.TagList).Where(x => x.TagList.Any(c => c.Name.Equals(tag, StringComparison.CurrentCultureIgnoreCase))).AsNoTracking().ToListAsync();
            }
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
            if (!System.IO.File.Exists($"{Services.Dir.CardsMiniatures}/{id}.png") || !System.IO.File.Exists($"{Services.Dir.Cards}/{id}.png") || !System.IO.File.Exists($"{Services.Dir.CardsInProfiles}/{id}.png"))
            {
                using (var db = new Database.UserContext(_config))
                {
                    var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (card == null)
                    {
                        await "Card not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                        return;
                    }

                    _waifu.DeleteCardImageIfExist(card);
                    var cardImage = await _waifu.GenerateAndSaveCardAsync(card);
                    await ControllerContext.HttpContext.Response.SendFileAsync(cardImage);
                }
            }
            else
            {
                await "Card already exist!".ToResponse(403).ExecuteResultAsync(ControllerContext);
            }
        }

        /// <summary>
        /// Daje użytkownikowi pakiety kart
        /// </summary>
        /// <param name="id">id użytkownika discorda</param>
        /// <param name="boosterPacks">model pakietu</param>
        /// <returns>użytkownik bota</returns>
        /// <response code="404">User not found</response>
        /// <response code="500">Model is Invalid</response>
        [HttpPost("discord/{id}/boosterpack"), Authorize(Policy = "Site")]
        public async Task GiveUserAPacksAsync(ulong id, [FromBody]List<Models.CardBoosterPack> boosterPacks)
        {
            if (boosterPacks?.Count < 1)
            {
                await "Model is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            var packs = new List<BoosterPack>();
            foreach (var pack in boosterPacks)
            {
                var rPack = pack.ToRealPack();
                if (rPack != null) packs.Add(rPack);
            }

            if (packs.Count < 1)
            {
                await "Data is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return;
            }

            using (var db = new Database.UserContext(_config))
            {
                var user = await db.GetCachedFullUserAsync(id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return;
                }

                var exe = new Executable($"api-packet u{id}", new Task<Task>(async () =>
                {
                    using (var dbs = new Database.UserContext(_config))
                    {
                        var botUser = await dbs.GetUserOrCreateAsync(id);

                        foreach (var pack in packs)
                            botUser.GameDeck.BoosterPacks.Add(pack);

                        await dbs.SaveChangesAsync();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                await "Boosterpack added!".ToResponse(200).ExecuteResultAsync(ControllerContext);
            }
        }

        /// <summary>
        /// Daje użytkownikowi pakiety kart
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <param name="boosterPacks">model pakietu</param>
        /// <returns>użytkownik bota</returns>
        /// <response code="404">User not found</response>
        /// <response code="500">Model is Invalid</response>
        [HttpPost("shinden/{id}/boosterpack"), Authorize(Policy = "Site")]
        public async Task<UserWithToken> GiveShindenUserAPacksAsync(ulong id, [FromBody]List<Models.CardBoosterPack> boosterPacks)
        {
            if (boosterPacks?.Count < 1)
            {
                await "Model is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return null;
            }

            var packs = new List<BoosterPack>();
            foreach (var pack in boosterPacks)
            {
                var rPack = pack.ToRealPack();
                if (rPack != null) packs.Add(rPack);
            }

            if (packs.Count < 1)
            {
                await "Data is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return null;
            }

            using (var db = new Database.UserContext(_config))
            {
                var user = await db.GetCachedFullUserByShindenIdAsync(id);
                if (user == null)
                {
                    await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }

                var discordId = user.Id;
                var exe = new Executable($"api-packet u{discordId}", new Task<Task>(async () =>
                {
                    using (var dbs = new Database.UserContext(_config))
                    {
                        var botUser = await dbs.GetUserOrCreateAsync(discordId);

                        foreach (var pack in packs)
                            botUser.GameDeck.BoosterPacks.Add(pack);

                        await dbs.SaveChangesAsync();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                    }
                }));

                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));

                TokenData tokenData = null;
                var currUser = ControllerContext.HttpContext.User;
                if (currUser.HasClaim(x => x.Type == ClaimTypes.Webpage))
                {
                    tokenData = UserTokenBuilder.BuildUserToken(_config, user);
                }

                return new UserWithToken()
                {
                    Expire = tokenData?.Expire,
                    Token = tokenData?.Token,
                    User = user,
                };
            }
        }

        /// <summary>
        /// Otwiera pakiety i dodaje użytkownikowi karty wylosowane z nich
        /// </summary>
        /// <param name="id">id użytkownika shindena</param>
        /// <param name="boosterPacks">model pakietu</param>
        /// <returns>karty</returns>
        /// <response code="404">User not found</response>
        /// <response code="406">User has no space</response>
        /// <response code="500">Model/Data is Invalid</response>
        /// <response code="503">Command queue is full</response>
        [HttpPost("shinden/{id}/boosterpack/open"), Authorize(Policy = "Site")]
        public async Task<List<Card>> GiveShindenUserAPacksAndOpenAsync(ulong id, [FromBody]List<Models.CardBoosterPack> boosterPacks)
        {
            if (boosterPacks?.Count < 1)
            {
                await "Model is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return null;
            }

            var packs = new List<BoosterPack>();
            foreach (var pack in boosterPacks)
            {
                var rPack = pack.ToRealPack();
                if (rPack != null) packs.Add(rPack);
            }

            if (packs.Count < 1)
            {
                await "Data is Invalid".ToResponse(500).ExecuteResultAsync(ControllerContext);
                return null;
            }

            ulong discordId = 0;
            using (var db = new Database.UserContext(_config))
            {
                var bUser = await db.Users.AsQueryable().Where(x => x.Shinden == id).Include(x => x.GameDeck).ThenInclude(x => x.Cards).AsNoTracking().AsSplitQuery().FirstOrDefaultAsync();
                if (bUser == null)
                {
                    await "User not found".ToResponse(404).ExecuteResultAsync(ControllerContext);
                    return null;
                }
                if (bUser.GameDeck.Cards.Count + packs.Sum(x => x.CardCnt) > bUser.GameDeck.MaxNumberOfCards)
                {
                    await "User has no space left in deck".ToResponse(406).ExecuteResultAsync(ControllerContext);
                    return null;
                }
                discordId = bUser.Id;
            }

            var cards = new List<Card>();
            foreach (var pack in packs)
            {
                cards.AddRange(await _waifu.OpenBoosterPackAsync(null, pack));
            }

            var exe = new Executable($"api-packet-open u{discordId}", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    Console.WriteLine("START");
                    var botUser = await db.GetUserOrCreateAsync(discordId);

                    botUser.Stats.OpenedBoosterPacks += packs.Count;

                    foreach (var card in cards)
                    {
                        card.Affection += botUser.GameDeck.AffectionFromKarma();
                        card.FirstIdOwner = botUser.Id;

                        botUser.GameDeck.Cards.Add(card);
                        botUser.GameDeck.RemoveCharacterFromWishList(card.Character);
                    }

                    await db.SaveChangesAsync();

                    Console.WriteLine("KONIEC");

                    QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                }
            }));

            if (!await _executor.TryAdd(exe, TimeSpan.FromSeconds(1)))
            {
                await "Command queue is full".ToResponse(503).ExecuteResultAsync(ControllerContext);
                return null;
            }

            await exe.WaitAsync();
            Console.WriteLine("DALEJ");

            return cards;
        }

        /// <summary>
        /// Otwiera pakiet użytkownika (wymagany Bearer od użytkownika)
        /// </summary>
        /// <param name="packNumber">numer pakietu</param>
        /// <response code="403">The appropriate claim was not found</response>
        /// <response code="404">User not found</response>
        /// <response code="406">User has no space</response>
        [HttpPost("boosterpack/open/{packNumber}"), Authorize(Policy = "Player")]
        public async Task<List<Card>> OpenAPackAsync(int packNumber)
        {
            var currUser = ControllerContext.HttpContext.User;
            if (currUser.HasClaim(x => x.Type == "DiscordId"))
            {
                if (ulong.TryParse(currUser.Claims.First(x => x.Type == "DiscordId").Value, out var discordId))
                {
                    string bPackName = "";
                    var cards = new List<Card>();
                    using (var db = new Database.UserContext(_config))
                    {
                        var botUserCh = await db.GetCachedFullUserAsync(discordId);
                        if (botUserCh == null)
                        {
                            await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                            return null;
                        }

                        if (botUserCh.GameDeck.BoosterPacks.Count < packNumber || packNumber <= 0)
                        {
                            await "Boosterpack not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                            return null;
                        }

                        var pack = botUserCh.GameDeck.BoosterPacks.ToArray()[packNumber - 1];

                        if (botUserCh.GameDeck.Cards.Count + pack.CardCnt > botUserCh.GameDeck.MaxNumberOfCards)
                        {
                            await "User has no space left in deck!".ToResponse(406).ExecuteResultAsync(ControllerContext);
                            return null;
                        }

                        cards = await _waifu.OpenBoosterPackAsync(null, pack);
                        bPackName = pack.Name;
                    }

                    var exe = new Executable($"api-packet-open u{discordId}", new Task<Task>(async () =>
                    {
                        using (var db = new Database.UserContext(_config))
                        {
                            var botUser = await db.GetUserOrCreateAsync(discordId);

                            var bPack = botUser.GameDeck.BoosterPacks.ToArray()[packNumber - 1];
                            if (bPack?.Name != bPackName)
                            {
                                await "Boosterpack already opened!".ToResponse(500).ExecuteResultAsync(ControllerContext);
                                return;
                            }

                            botUser.GameDeck.BoosterPacks.Remove(bPack);

                            if (bPack.CardSourceFromPack == CardSource.Activity || bPack.CardSourceFromPack == CardSource.Migration)
                            {
                                botUser.Stats.OpenedBoosterPacksActivity += 1;
                            }
                            else
                            {
                                botUser.Stats.OpenedBoosterPacks += 1;
                            }

                            foreach (var card in cards)
                            {
                                card.Affection += botUser.GameDeck.AffectionFromKarma();
                                card.FirstIdOwner = botUser.Id;

                                botUser.GameDeck.Cards.Add(card);
                                botUser.GameDeck.RemoveCharacterFromWishList(card.Character);
                            }

                            await db.SaveChangesAsync();

                            QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                        }
                    }));

                    await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));

                    exe.Wait();

                    return cards;
                }
            }
            await "The appropriate claim was not found".ToResponse(403).ExecuteResultAsync(ControllerContext);
            return null;
        }

        /// <summary>
        /// Aktywuje lub dezaktywuje kartę (wymagany Bearer od użytkownika)
        /// </summary>
        /// <param name="wid">id karty</param>
        /// <response code="403">The appropriate claim was not found</response>
        /// <response code="404">Card not found</response>
        [HttpPut("deck/toggle/card/{wid}"), Authorize(Policy = "Player")]
        public async Task ToggleCardStatusAsync(ulong wid)
        {
            var currUser = ControllerContext.HttpContext.User;
            if (currUser.HasClaim(x => x.Type == "DiscordId"))
            {
                if (ulong.TryParse(currUser.Claims.First(x => x.Type == "DiscordId").Value, out var discordId))
                {
                    using (var db = new Database.UserContext(_config))
                    {
                        var botUserCh = await db.GetCachedFullUserAsync(discordId);
                        if (botUserCh == null)
                        {
                            await "User not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                            return;
                        }

                        var thisCardCh = botUserCh.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                        if (thisCardCh == null)
                        {
                            await "Card not found!".ToResponse(404).ExecuteResultAsync(ControllerContext);
                            return;
                        }

                        if (thisCardCh.InCage)
                        {
                            await "Card is in cage!".ToResponse(403).ExecuteResultAsync(ControllerContext);
                            return;
                        }
                    }

                    var exe = new Executable($"api-deck u{discordId}", new Task<Task>(async () =>
                    {
                        using (var db = new Database.UserContext(_config))
                        {
                            var botUser = await db.GetUserOrCreateAsync(discordId);
                            var thisCard = botUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                            thisCard.Active = !thisCard.Active;

                            await db.SaveChangesAsync();

                            QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });
                        }
                    }));

                    await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                    await "Card status toggled".ToResponse(200).ExecuteResultAsync(ControllerContext);
                    return;
                }
            }
            await "The appropriate claim was not found".ToResponse(403).ExecuteResultAsync(ControllerContext);
        }
    }
}