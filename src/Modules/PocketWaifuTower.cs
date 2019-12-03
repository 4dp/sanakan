#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;
using Sden = Shinden;

namespace Sanakan.Modules
{
    [Name("PocketWaifu - Wieża"), RequireUserRole]
    public class PocketWaifuTower : SanakanModuleBase<SocketCommandContext>
    {
        private Sden.ShindenClient _shclient;
        private SessionManager _session;
        private IExecutor _executor;
        private ILogger _logger;
        private IConfig _config;
        private Waifu _waifu;

        public PocketWaifuTower(Waifu waifu, Sden.ShindenClient client, ILogger logger,
            SessionManager session, IConfig config, IExecutor executor)
        {
            _waifu = waifu;
            _logger = logger;
            _config = config;
            _shclient = client;
            _session = session;
            _executor = executor;
        }

        [Command("podejmij wyzwanie")]
        [Alias("challenge accepted")]
        [Summary("tworzy profil karty do wieży wyzwań")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task SetCardProfileAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var floorOne = await db.GetOrCreateFloorAsync(1);
                var startRoom = floorOne.Rooms.FirstOrDefault(x => x.Type == RoomType.Start);

                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var thisCard = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard.Profile != null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta znajduje się już w wieży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!thisCard.HasImage())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} karta musi posiadać obrazek.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var cardsInTower = bUser.GameDeck.Cards.Count(x => x.Profile != null);
                if (cardsInTower > 0)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} na chwilę obecną możesz mieć tylko jedną kartę w więży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                thisCard.Profile = thisCard.GenerateTowerProfile();
                thisCard.Profile.ConqueredRoomsFromFloor = $"{startRoom.Id}";
                thisCard.Profile.CurrentRoom = startRoom;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{Context.User.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} wybrał się do więży kartą: {thisCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("wieża")]
        [Alias("tower", "wieza")]
        [Summary("wyświetla status karty w wieży")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ShowStatusInTowerAsync([Summary("WID")]ulong wid = 0)
        {
            var user = Context.User as SocketGuildUser;
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(user.Id);
                if (bUser == null && wid == 0)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var session = new TowerSession(user, _config, _waifu);
                if (_session.SessionExist(session))
                {
                    await ReplyAsync("", embed: $"{user.Mention} już masz otwartą sesje wieży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var towerCards = bUser.GameDeck.Cards.Where(x => x.Profile != null).ToList();
                if (towerCards.Count > 1)
                {
                    await ReplyAsync("", embed: $"{user.Mention} posiadasz więcej jak jedną kartę w więży, sprezycuj jej WID.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }
                if (towerCards.Count < 1)
                {
                    await ReplyAsync("", embed: $"{user.Mention} żadna z twoich kart nie znajduje się w wieży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }
                var thisCard = towerCards.FirstOrDefault();
                if (wid != 0) thisCard = towerCards.FirstOrDefault(x => x.Id == wid);

                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{user.Mention} ta karta nie istnieje lub nie znajduje się w wieży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                session.PlayingCard = thisCard;
                session.P1 = new PlayerInfo
                {
                    User = user,
                    Dbuser = bUser,
                    Accepted = false,
                    Cards = new List<Card>(),
                    Items = new List<Item>()
                };

                var msg = await ReplyAsync("", embed: session.BuildEmbed());
                await msg.AddReactionsAsync(session.StartReactions);
                session.Message = msg;

                await _session.TryAddSession(session);
            }
        }

        [Command("tprofil", RunMode = RunMode.Async)]
        [Alias("tprofile")]
        [Summary("wyświetla profil PocketWaifu - Wieża")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ShowProfileAsync([Summary("WID")]ulong wid = 0)
        {
            var user = Context.User as SocketGuildUser;
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(user.Id);
                if (bUser == null && wid == 0)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                Card thisCard = null;
                if (wid == 0)
                {
                    var towerCards = bUser.GameDeck.Cards.Where(x => x.Profile != null).ToList();
                    if (towerCards.Count > 1)
                    {
                        await ReplyAsync("", embed: $"{user.Mention} posiadasz więcej jak jedną kartę w więży, sprezycuj jej WID.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    if (towerCards.Count < 1)
                    {
                        await ReplyAsync("", embed: $"{user.Mention} żadna z twoich kart nie znajduje się w wieży.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    thisCard = towerCards.FirstOrDefault();
                }
                else thisCard = await db.GetCachedFullCardAsync(wid);

                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{user.Mention} ta karta nie istnieje lub nie znajduje się w wieży.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var embed = new EmbedBuilder().WithColor(EMType.Info.Color()).WithDescription(thisCard.GetTowerProfile());
                using (var cdb = new Database.GuildConfigContext(Config))
                {
                    var config = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                    var channel = Context.Guild.GetTextChannel(config.WaifuConfig.TrashCommandsChannel);
                    embed.WithThumbnailUrl(await _waifu.GetWaifuProfileImageAsync(thisCard, channel));
                }

                await ReplyAsync("", embed: embed.Build());
            }

            //TODO: equip items, restore action points
        }
    }
}
