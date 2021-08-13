#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Shinden;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Debug"), Group("dev"), DontAutoLoad, RequireDev]
    public class Debug : SanakanModuleBase<SocketCommandContext>
    {
        private Waifu _waifu;
        private IConfig _config;
        private IExecutor _executor;
        private Services.Helper _helper;
        private ShindenClient _shClient;
        private Services.ImageProcessing _img;

        public Debug(Waifu waifu, ShindenClient shClient, Services.Helper helper, Services.ImageProcessing img, IConfig config, IExecutor executor)
        {
            _shClient = shClient;
            _executor = executor;
            _helper = helper;
            _config = config;
            _waifu = waifu;
            _img = img;
        }

        [Command("poke", RunMode = RunMode.Async)]
        [Summary("generuje obrazek safari")]
        [Remarks("1")]
        public async Task GeneratePokeImageAsync([Summary("nr grafiki")]int index)
        {
            try
            {
                var reader = new JsonFileReader($"./Pictures/Poke/List.json");
                var images = reader.Load<List<SafariImage>>();

                var character = (await _shClient.GetCharacterInfoAsync(2)).Body;
                var channel = Context.Channel as ITextChannel;

                _ = await _waifu.GetSafariViewAsync(images[index], _waifu.GenerateNewCard(null, character), channel);
            }
            catch (Exception ex)
            {
                await ReplyAsync("", embed: $"Coś poszło nie tak: {ex.Message}".ToEmbedMessage(EMType.Error).Build());
            }
        }

        [Command("missingu", RunMode = RunMode.Async)]
        [Summary("generuje listę id użytkowników, których nie widzi bot na serwerach")]
        [Remarks("")]
        public async Task GenerateMissingUsersListAsync()
        {
            var allUsers = Context.Client.Guilds.SelectMany(x => x.Users).Distinct();
            using (var db = new Database.UserContext(Config))
            {
                var nonExistingIds = db.Users.AsQueryable().AsSplitQuery().Where(x => !allUsers.Any(u => u.Id == x.Id)).Select(x => x.Id).ToList();
                await ReplyAsync("", embed: string.Join("\n", nonExistingIds).ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("blacklist")]
        [Summary("dodaje/usuwa użytkownika do czarnej listy")]
        [Remarks("Karna")]
        public async Task TransferCardAsync([Summary("użytkownik")]SocketGuildUser user)
        {
            using (var db = new Database.UserContext(Config))
            {
                var targetUser = await db.GetUserOrCreateAsync(user.Id);

                targetUser.IsBlacklisted = !targetUser.IsBlacklisted;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{Context.User.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} - blacklist: {targetUser.IsBlacklisted}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("rmsg", RunMode = RunMode.Async)]
        [Summary("wysyła wiadomość na kanał w danym serwerze jako odpowiedź do podanej innej wiadomości")]
        [Remarks("15188451644 101155483 1231231 Nie masz racji!")]
        public async Task SendResponseMsgToChannelInGuildAsync([Summary("id serwera")]ulong gId, [Summary("id kanału")]ulong chId, [Summary("id wiadomości")]ulong msgId, [Summary("treść wiadomości")][Remainder]string msg)
        {
            try
            {
                var msg2r = await Context.Client.GetGuild(gId).GetTextChannel(chId).GetMessageAsync(msgId);
                if (msg2r is IUserMessage umsg) await umsg.ReplyAsync(msg);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("smsg", RunMode = RunMode.Async)]
        [Summary("wysyła wiadomość na kanał w danym serwerze")]
        [Remarks("15188451644 101155483 elo ziomki")]
        public async Task SendMsgToChannelInGuildAsync([Summary("id serwera")]ulong gId, [Summary("id kanału")]ulong chId, [Summary("treść wiadomości")][Remainder]string msg)
        {
            try
            {
                await Context.Client.GetGuild(gId).GetTextChannel(chId).SendMessageAsync(msg);
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("semsg", RunMode = RunMode.Async)]
        [Summary("wysyła wiadomość w formie embed na kanał w danym serwerze")]
        [Remarks("15188451644 101155483 bot elo ziomki")]
        public async Task SendEmbedMsgToChannelInGuildAsync([Summary("id serwera")]ulong gId, [Summary("id kanału")]ulong chId, [Summary("typ wiadomości(Neutral/Warning/Success/Error/Info/Bot)")]EMType type, [Summary("treść wiadomości")][Remainder]string msg)
        {
            try
            {
                await Context.Client.GetGuild(gId).GetTextChannel(chId).SendMessageAsync("", embed: msg.ToEmbedMessage(type).Build());
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("r2msg", RunMode = RunMode.Async)]
        [Summary("dodaje reakcje do wiadomości")]
        [Remarks("15188451644 101155483 825724399512453140 <:Redpill:455880209711759400>")]
        public async Task AddReactionToMsgOnChannelInGuildAsync([Summary("id serwera")]ulong gId, [Summary("id kanału")]ulong chId, [Summary("id wiadomości")]ulong msgId, [Summary("reakcja")]string reaction)
        {
            try
            {
                var msg = await Context.Client.GetGuild(gId).GetTextChannel(chId).GetMessageAsync(msgId);
                if (Emote.TryParse(reaction, out var emote))
                {
                    await msg.AddReactionAsync(emote);
                }
                else
                {
                    await msg.AddReactionAsync(new Emoji(reaction));
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(ex.Message);
            }
        }

        [Command("cup")]
        [Summary("wymusza update na kartach")]
        [Remarks("3123 121")]
        public async Task ForceUpdateCardsAsync([Summary("WID kart")]params ulong[] ids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var cards = db.Cards.AsQueryable().AsSplitQuery().Where(x => ids.Any(c => c == x.Id)).ToList();
                if (cards.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var card in cards)
                {
                    try
                    {
                        await card.Update(null, _shClient);
                        _waifu.DeleteCardImageIfExist(card);
                    }
                    catch (Exception) { }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"users" });

                await ReplyAsync("", embed: $"Zaktualizowano {cards.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("rozdajm", RunMode = RunMode.Async)]
        [Summary("rozdaje karty kilka razy")]
        [Remarks("1 10 5 10")]
        public async Task GiveawayCardsMultiAsync([Summary("id użytkownika")]ulong id, [Summary("liczba kart")]uint count, [Summary("czas w minutach")]uint duration = 5, [Summary("liczba powtórzeń")]uint repeat = 1)
        {
            for (uint i = 0; i < repeat; i++)
            {
                await GiveawayCardsAsync(id, count, duration);
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        [Command("rozdaj", RunMode = RunMode.Async)]
        [Summary("rozdaje karty")]
        [Remarks("1 10 5")]
        public async Task GiveawayCardsAsync([Summary("id użytkownika")]ulong id, [Summary("liczba kart")]uint count, [Summary("czas w minutach")]uint duration = 5)
        {
            var emote = Emote.Parse("<a:success:467493778752798730>");
            var time = DateTime.Now.AddMinutes(duration);

            var mention = "";
            SocketRole mutedRole = null;
            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config != null)
                {
                    var wRole = Context.Guild.GetRole(config.WaifuRole);
                    if (wRole != null) mention = wRole.Mention;

                    mutedRole = Context.Guild.GetRole(config.MuteRole);
                }
            }

            var msg = await ReplyAsync(mention, embed: $"Loteria kart. Zareaguj {emote}, aby wziąć udział.\n\nKoniec `{time.ToShortTimeString()}:{time.Second.ToString("00")}`".ToEmbedMessage(EMType.Bot).Build());
            await msg.AddReactionAsync(emote);

            await Task.Delay(TimeSpan.FromMinutes(duration));
            await msg.RemoveReactionAsync(emote, Context.Client.CurrentUser);

            var reactions = await msg.GetReactionUsersAsync(emote, 300).FlattenAsync();
            var users = reactions.Shuffle().ToList();

            IUser winner = null;
            using (var db = new Database.UserContext(_config))
            {
                var watch = Stopwatch.StartNew();
                while (winner == null)
                {
                    if (watch.ElapsedMilliseconds > 60000)
                        throw new Exception("Timeout");

                    if (users.Count < 1)
                    {
                        await msg.ModifyAsync(x => x.Embed = "Na loterie nie stawił się żaden użytkownik!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    bool muted = false;
                    var selected = Services.Fun.GetOneRandomFrom(users);
                    if (mutedRole != null && selected is SocketGuildUser su)
                    {
                        if (su.Roles.Any(x => x.Id == mutedRole.Id))
                            muted = true;
                    }

                    var dUser = await db.GetCachedFullUserAsync(selected.Id);
                    if (dUser != null && !muted)
                    {
                        if (!dUser.IsBlacklisted)
                            winner = selected;
                    }
                    users.Remove(selected);
                }
            }

            var exe = new Executable("lotery", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(Config))
                {
                    var user = await db.GetUserOrCreateAsync(id);
                    if (user == null)
                    {
                        await msg.ModifyAsync(x => x.Embed = "Nie odnaleziono kart do rozdania!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    var loteryCards = user.GameDeck.Cards.ToList();
                    if (loteryCards.Count < 1)
                    {
                        await msg.ModifyAsync(x => x.Embed = "Nie odnaleziono kart do rozdania!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    var winnerUser = await db.GetUserOrCreateAsync(winner.Id);
                    if (winnerUser == null)
                    {
                        await msg.ModifyAsync(x => x.Embed = "Nie odnaleziono docelowego użytkownika!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    var cardsIds = new List<string>();
                    var idsToSelect = loteryCards.Select(x => x.Id).ToList();

                    for (int i = 0; i < count; i++)
                    {
                        if (idsToSelect.Count < 1)
                            break;

                        var wid = Services.Fun.GetOneRandomFrom(idsToSelect);
                        var thisCard = loteryCards.FirstOrDefault(x => x.Id == wid);

                        cardsIds.Add(thisCard.GetString(false, false, true));

                        thisCard.Active = false;
                        thisCard.InCage = false;
                        thisCard.TagList.Clear();
                        thisCard.Expedition = CardExpedition.None;

                        thisCard.GameDeckId = winnerUser.GameDeck.Id;

                        winnerUser.GameDeck.RemoveCharacterFromWishList(thisCard.Character);
                        winnerUser.GameDeck.RemoveCardFromWishList(thisCard.Id);

                        idsToSelect.Remove(wid);
                    }

                    await db.SaveChangesAsync();
                    await msg.DeleteAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"user-{Context.User.Id}", "users", $"user-{id}" });

                    msg = await ReplyAsync(embed: $"Loterie wygrywa {winner.Mention}.\nOtrzymuje: {string.Join("\n", cardsIds)}".TrimToLength(2000).ToEmbedMessage(EMType.Success).Build());

                    try
                    {
                        var privEmb = new EmbedBuilder()
                        {
                            Color = EMType.Info.Color(),
                            Description = $"Na [loterii]({msg.GetJumpUrl()}) zdobyłeś {cardsIds.Count} kart."
                        };

                        var pw = await winner.GetOrCreateDMChannelAsync();
                        if (pw != null) await pw.SendMessageAsync("", embed: privEmb.Build());
                    }
                    catch(Exception){}
                }
            }), Priority.High);

            await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
            await msg.RemoveAllReactionsAsync();
        }

        [Command("tranc"), Priority(1)]
        [Summary("przenosi kartę między użytkownikami")]
        [Remarks("Sniku 41231 41232")]
        public async Task TransferUserCardAsync([Summary("użytkownik")]SocketUser user, [Summary("WIDs")]params ulong[] wids) => await TransferCardAsync(user.Id, wids);

        [Command("tranc"), Priority(1)]
        [Summary("przenosi kartę między użytkownikami")]
        [Remarks("Sniku 41231 41232")]
        public async Task TransferCardAsync([Summary("id użytkownika")]ulong userId, [Summary("WIDs")]params ulong[] wids)
        {
            using (var db = new Database.UserContext(Config))
            {
                if (!db.Users.Any(x => x.Id == userId))
                {
                    await ReplyAsync("", embed: "W bazie nie ma użytkownika o podanym id!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var thisCards = db.Cards.AsQueryable().Include(x => x.TagList).AsSingleQuery().Where(x => wids.Contains(x.Id)).ToList();
                if (thisCards.Count < 1)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono kart!".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                string reply = $"Karta {thisCards.First().GetString(false, false, true)} została przeniesiona.";
                if (thisCards.Count > 1) reply = $"Przeniesiono {thisCards.Count} kart.";

                foreach (var thisCard in thisCards)
                {
                    thisCard.Active = false;
                    thisCard.InCage = false;
                    thisCard.TagList.Clear();
                    thisCard.GameDeckId = userId;
                    thisCard.Expedition = CardExpedition.None;
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{userId}", "users" });

                await ReplyAsync("", embed: reply.ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("rmcards"), Priority(1)]
        [Summary("kasuje podane karty")]
        [Remarks("41231 41232")]
        public async Task RemoveCardsAsync([Summary("WIDs")]params ulong[] wids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var thisCards = db.Cards.AsQueryable().Include(x => x.TagList).Include(x => x.ArenaStats).AsSingleQuery().Where(x => wids.Contains(x.Id)).ToList();
                if (thisCards.Count < 1)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono kart!".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                string reply = $"Karta {thisCards.First().GetString(false, false, true)} została skasowana.";
                if (thisCards.Count > 1) reply = $"Skasowano {thisCards.Count} kart.";

                foreach (var thisCard in thisCards)
                {
                    _waifu.DeleteCardImageIfExist(thisCard);
                    db.Cards.Remove(thisCard);
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { "users" });

                await ReplyAsync("", embed: reply.ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("level")]
        [Summary("ustawia podany poziom użytkownikowi")]
        [Remarks("Karna 1")]
        public async Task ChangeUserLevelAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("poziom")]long level)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(user.Id);

                bUser.Level = level;
                bUser.ExpCnt = Services.ExperienceManager.CalculateExpForLevel(level);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{Context.User.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ustawiono {level} poziom.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("mkick", RunMode = RunMode.Async)]
        [Summary("wyrzuca użytkowników z serwera")]
        [Remarks("Jupson Moe")]
        public async Task MultiKickAsync([Summary("użytkownicy")]params SocketGuildUser[] users)
        {
            var count = 0;

            foreach (var user in users)
            {
                try
                {
                    await user.KickAsync("Multi kick - łamanie regulaminu");
                    ++count;
                }
                catch (Exception) {}
            }

            await ReplyAsync("", embed: $"Wyrzucono {count} użytkowników.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("mban", RunMode = RunMode.Async)]
        [Summary("banuje użytkowników z serwera")]
        [Remarks("Jupson Moe")]
        public async Task MultiBankAsync([Summary("użytkownicy")]params SocketGuildUser[] users)
        {
            var count = 0;

            foreach (var user in users)
            {
                try
                {
                    await Context.Guild.AddBanAsync(user, 0, "Multi ban - łamanie regulaminu");
                    ++count;
                }
                catch (Exception) {}
            }

            await ReplyAsync("", embed: $"Zbanowano {count} użytkowników.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("restore"), Priority(1)]
        [Summary("przenosi kartę na nowo do użytkownika")]
        [Remarks("Sniku")]
        public async Task RestoreCardsAsync([Summary("użytkownik")]SocketGuildUser user)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(user.Id);
                var thisCards = db.Cards.Include(x => x.TagList).Where(x => (x.LastIdOwner == user.Id || (x.FirstIdOwner == user.Id && x.LastIdOwner == 0)) && x.GameDeckId == 1).ToList();
                if (thisCards.Count < 1)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono kart!".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                string reply = $"Karta {thisCards.First().GetString(false, false, true)} została przeniesiona.";
                if (thisCards.Count > 1) reply = $"Przeniesiono {thisCards.Count} kart.";

                foreach (var thisCard in thisCards)
                {
                    thisCard.Active = false;
                    thisCard.InCage = false;
                    thisCard.TagList.Clear();
                    thisCard.GameDeckId = user.Id;
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{Context.User.Id}", "users" });

                await ReplyAsync("", embed: reply.ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("missingc", RunMode = RunMode.Async)]
        [Summary("generuje listę id kart, których właścicieli nie widzi bot na serwerach")]
        [Remarks("true")]
        public async Task GenerateMissingUsersCardListAsync([Summary("czy wypisać id'ki")]bool ids = false)
        {
            var allUsers = Context.Client.Guilds.SelectMany(x => x.Users).Distinct();
            using (var db = new Database.UserContext(Config))
            {
                var nonExistingIds = db.Cards.AsQueryable().AsSplitQuery().Where(x => !allUsers.Any(u => u.Id == x.GameDeckId)).Select(x => x.Id).ToList();
                await ReplyAsync("", embed: $"Kart: {nonExistingIds.Count}".ToEmbedMessage(EMType.Bot).Build());

                if (ids)
                    await ReplyAsync("", embed: string.Join("\n", nonExistingIds).ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("cstats", RunMode = RunMode.Async)]
        [Summary("generuje statystyki kart począwszy od podanej karty")]
        [Remarks("1")]
        public async Task GenerateCardStatsAsync([Summary("WID")]ulong wid)
        {
            var stats = new long[(int)Rarity.E + 1];
            using (var db = new Database.UserContext(Config))
            {
                foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
                    stats[(int)rarity] = db.Cards.Count(x => x.Rarity == rarity && x.Id >= wid);

                string info = "";
                for (int i = 0; i < stats.Length; i++)
                    info += $"{(Rarity)i}: `{stats[i]}`\n";

                await ReplyAsync("", embed: info.ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("dusrcards"), Priority(1)]
        [Summary("usuwa karty użytkownika o podanym id z bazy")]
        [Remarks("845155646123")]
        public async Task RemoveCardsUserAsync([Summary("id użytkownika")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                var user = await db.GetUserOrCreateAsync(id);
                user.GameDeck.Cards.Clear();

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { "users", $"user-{id}" });
            }

            await ReplyAsync("", embed: $"Karty użytkownika o id: `{id}` zostały skasowane.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("duser"), Priority(1)]
        [Summary("usuwa użytkownika o podanym id z bazy")]
        [Remarks("845155646123")]
        public async Task FactoryUserAsync([Summary("id użytkownika")]ulong id, [Summary("czy usunąć karty?")]bool cards = false)
        {
            using (var db = new Database.UserContext(Config))
            {
                var fakeu = await db.GetUserOrCreateAsync(1);
                var user = await db.GetUserOrCreateAsync(id);

                if (!cards)
                {
                    foreach (var card in user.GameDeck.Cards)
                    {
                        card.InCage = false;
                        card.TagList.Clear();
                        card.LastIdOwner = id;
                        card.GameDeckId = fakeu.GameDeck.Id;
                    }
                }

                db.Users.Remove(user);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { "users", $"user-{id}" });
            }

            await ReplyAsync("", embed: $"Użytkownik o id: `{id}` został wymazany.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tc duser"), Priority(1)]
        [Summary("usuwa dane użytkownika o podanym id z bazy i danej wartości tc")]
        [Remarks("845155646123 5000")]
        public async Task FactoryUserAsync([Summary("id użytkownika")]ulong id, [Summary("wartość tc")]long value)
        {
            using (var db = new Database.UserContext(Config))
            {
                var user = await db.GetUserOrCreateAsync(id);
                foreach (var card in user.GameDeck.Cards.OrderByDescending(x => x.CreationDate).ToList())
                {
                    value -= 50;
                    user.GameDeck.Cards.Remove(card);

                    if (value <= 0)
                        break;
                }

                if (value > 0)
                {
                    var kct = value / 50;
                    if (user.GameDeck.Karma > 0)
                    {
                        user.GameDeck.Karma -= kct;
                        if (user.GameDeck.Karma < 0)
                            user.GameDeck.Karma = 0;
                    }
                    else
                    {
                        user.GameDeck.Karma += kct;
                        if (user.GameDeck.Karma > 0)
                            user.GameDeck.Karma = 0;
                    }

                    user.GameDeck.CTCnt -= kct;
                    if (user.GameDeck.CTCnt < 0)
                    {
                        user.GameDeck.CTCnt = 0;
                        kct = 0;
                    }

                    if (kct > 0)
                    {
                        user.GameDeck.Items.Clear();
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { "users", $"user-{id}" });
            }

            await ReplyAsync("", embed: $"Użytkownik o id: `{id}` został zrównany.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("utitle"), Priority(1)]
        [Summary("aktualizuje tytuł karty")]
        [Remarks("ssało")]
        public async Task ChangeTitleCardAsync([Summary("WID")]ulong wid, [Summary("tytuł")][Remainder]string title = null)
        {
            using (var db = new Database.UserContext(Config))
            {
                var thisCard = db.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"Taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (title != null)
                {
                    thisCard.Title = title;
                }
                else
                {
                    var res = await _shClient.GetCharacterInfoAsync(thisCard.Character);
                    if (res.IsSuccessStatusCode())
                    {
                        thisCard.Title = res.Body?.Relations?.OrderBy(x => x.Id)?.FirstOrDefault()?.Title ?? "????";
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { "users" });

                await ReplyAsync("", embed: $"Nowy tytuł to: `{thisCard.Title}`".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("delq"), Priority(1)]
        [Summary("kasuje zagadkę o podanym id")]
        [Remarks("20}")]
        public async Task RemoveQuizAsync([Summary("id zagadki")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                var question = db.Questions.FirstOrDefault(x => x.Id == id);
                if (question == null)
                {
                    await ReplyAsync("", embed: $"Zagadka o ID: `{id}` nie istnieje!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                db.Questions.Remove(question);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"quiz" });
                await ReplyAsync("", embed: $"Zagadka o ID: `{id}` została skasowana!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("addq"), Priority(1)]
        [Summary("dodaje nową zagadkę")]
        [Remarks("{...}")]
        public async Task AddNewQuizAsync([Summary("zagadka w formie jsona")][Remainder]string json)
        {
            try
            {
                var question = JsonConvert.DeserializeObject<Question>(json);
                using (var db = new Database.UserContext(Config))
                {
                    db.Questions.Add(question);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"quiz" });
                    await ReplyAsync("", embed: $"Nowy zagadka dodana, jej ID to: `{question.Id}`".ToEmbedMessage(EMType.Success).Build());
                }
            }
            catch (Exception)
            {
                await ReplyAsync("", embed: $"Coś poszło nie tak przy parsowaniu jsona!".ToEmbedMessage(EMType.Error).Build());
            }
        }

        [Command("chpp"), Priority(1)]
        [Summary("ustawia liczbę znaków na pakiet")]
        [Remarks("true")]
        public async Task SetCharCntPerPacketAsync([Summary("liczba znaków")]long count, [Summary("true/false - czy zapisać")]bool save = false)
        {
            var config = Config.Get();
            config.CharPerPacket = count;
            if (save) Config.Save();

            await ReplyAsync("", embed: $"Ustawiono próg `{count}` znaków na pakiet. `Zapisano: {save.GetYesNo()}`".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("chpe"), Priority(1)]
        [Summary("ustawia liczbę znaków na punkt doświadczenia")]
        [Remarks("true")]
        public async Task SetCharCntPerExpAsync([Summary("liczba znaków")]long count, [Summary("true/false - czy zapisać")]bool save = false)
        {
            var config = Config.Get();
            config.Exp.CharPerPoint = count;
            if (save) Config.Save();

            await ReplyAsync("", embed: $"Ustawiono próg `{count}` znaków na punkt doświadczenia. `Zapisano: {save.GetYesNo()}`".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tsafari"), Priority(1)]
        [Summary("wyłącza/załącza safari")]
        [Remarks("true")]
        public async Task ToggleSafariAsync([Summary("true/false - czy zapisać")]bool save = false)
        {
            var config = Config.Get();
            config.SafariEnabled = !config.SafariEnabled;
            if (save) Config.Save();

            await ReplyAsync("", embed: $"Safari: `{config.SafariEnabled.GetYesNo()}` `Zapisano: {save.GetYesNo()}`".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("twevent"), Priority(1)]
        [Summary("wyłącza/załącza event waifu")]
        [Remarks("")]
        public async Task ToggleWaifuEventAsync()
        {
            var state = _waifu.GetEventSate();
            _waifu.SetEventState(!state);

            await ReplyAsync("", embed: $"Waifu event: `{(!state).GetYesNo()}`.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("wevent"), Priority(1)]
        [Summary("ustawia id eventu (kasowane są po restarcie)")]
        [Remarks("https://pastebin.com/raw/Y6a8gH5P")]
        public async Task SetWaifuEventIdsAsync([Summary("link do pliku z id postaci oddzielnymi średnikami")]string url)
        {
            using (var client = new HttpClient())
            {
                var ids = new List<ulong>();
                var res = await client.GetAsync(url);
                if (res.IsSuccessStatusCode)
                {
                    using (var body = await res.Content.ReadAsStreamAsync())
                    {
                        using (var sr = new StreamReader(body))
                        {
                            var content = await sr.ReadToEndAsync();

                            try
                            {
                                ids = content.Split(";").Select(x => ulong.Parse(x)).ToList();
                            }
                            catch (Exception ex)
                            {
                                await ReplyAsync("", embed: $"Format pliku jest niepoprawny! ({ex.Message})".ToEmbedMessage(EMType.Error).Build());
                                return;
                            }
                        }
                    }

                    if (ids.Count > 0)
                    {
                        _waifu.SetEventIds(ids);
                        await ReplyAsync("", embed: $"Ustawiono `{ids.Count}` id.".ToEmbedMessage(EMType.Success).Build());
                        return;
                    }
                }
            }

            await ReplyAsync("", embed: $"Nie udało się odczytać pliku.".ToEmbedMessage(EMType.Error).Build());
        }

        [Command("lvlbadge", RunMode = RunMode.Async)]
        [Summary("generuje przykładowy obrazek otrzymania poziomu")]
        [Remarks("")]
        public async Task GenerateLevelUpBadgeAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            using (var badge = await _img.GetLevelUpBadgeAsync("Very very long nickname of trolly user",
                2154, usr.GetUserOrDefaultAvatarUrl(), usr.Roles.OrderByDescending(x => x.Position).First().Color))
            {
                using (var badgeStream = badge.ToPngStream())
                {
                    await Context.Channel.SendFileAsync(badgeStream, $"{usr.Id}.png");
                }
            }
        }

        [Command("devr", RunMode = RunMode.Async)]
        [Summary("przyznaje lub odbiera role developera")]
        [Remarks("")]
        public async Task ToggleDeveloperRoleAsync()
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            var devr = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Developer");
            if (devr == null) return;

            if (user.Roles.Contains(devr))
            {
                await user.RemoveRoleAsync(devr);
                await ReplyAsync("", embed: $"{user.Mention} stracił role deva.".ToEmbedMessage(EMType.Success).Build());
            }
            else
            {
                await user.AddRoleAsync(devr);
                await ReplyAsync("", embed: $"{user.Mention} otrzymał role deva.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("gitem"), Priority(1)]
        [Summary("generuje przedmiot i daje go użytkownikowi")]
        [Remarks("Sniku 2 1")]
        public async Task GenerateItemAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("przedmiot")]ItemType itemType, [Summary("liczba przedmiotów")]uint count = 1,
            [Summary("jakość przedmiotu")]Quality quality = Quality.Broken)
        {
            var item = itemType.ToItem(count, quality);
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                var thisItem = botuser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type && x.Quality == item.Quality);
                if (thisItem == null)
                {
                    thisItem = item;
                    botuser.GameDeck.Items.Add(thisItem);
                }
                else thisItem.Count += count;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                string cnt = (count > 1) ? $" x{count}" : "";
                await ReplyAsync("", embed: $"{user.Mention} otrzymał _{item.Name}_{cnt}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("gcard"), Priority(1)]
        [Summary("generuje kartę i daje ją użytkownikowi")]
        [Remarks("Sniku 54861")]
        public async Task GenerateCardAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("id postaci na shinden (nie podanie - losowo)")]ulong id = 0,
            [Summary("jakość karty (nie podanie - losowo)")]Rarity rarity = Rarity.E)
        {
            var character = (id == 0) ? await _waifu.GetRandomCharacterAsync() : (await _shClient.GetCharacterInfoAsync(id)).Body;
            var card = (rarity == Rarity.E) ? _waifu.GenerateNewCard(user, character) : _waifu.GenerateNewCard(user, character, rarity);

            card.Source = CardSource.GodIntervention;
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.GameDeck.Cards.Add(card);

                botuser.GameDeck.RemoveCharacterFromWishList(card.Character);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} otrzymał {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ctou"), Priority(1)]
        [Summary("zamienia kartę na ultimate")]
        [Remarks("54861 Zeta 100 100 1000")]
        public async Task MakeUltimateCardAsync([Summary("wid karty")]ulong id, [Summary("jakość karty")]Quality quality,
            [Summary("dodatkowy atak")]int atk = 0, [Summary("dodatkowa obrona")]int def = 0, [Summary("dodatkowe hp")]int hp = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var card = await db.Cards.AsQueryable().Include(x => x.TagList).AsSingleQuery().FirstOrDefaultAsync(x => x.Id == id);
                if (card == null)
                {
                    await ReplyAsync("", embed: "W bazie nie ma takiej karty!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                card.PAS = PreAssembledFigure.None;
                card.Rarity = Rarity.SSS;
                card.FromFigure = true;
                card.Quality = quality;

                if (def != 0) card.DefenceBonus = def;
                if (atk != 0) card.AttackBonus = atk;
                if (hp != 0) card.HealthBonus = hp;

                await db.SaveChangesAsync();

                _waifu.DeleteCardImageIfExist(card);

                QueryCacheManager.ExpireTag(new string[] { $"user-{card.GameDeckId}", "users" });

                await ReplyAsync("", embed: $"Utworzono: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("sc"), Priority(1)]
        [Summary("zmienia SC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserScAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba SC")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.ScCnt += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.ScCnt} SC".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ac"), Priority(1)]
        [Summary("zmienia AC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserAcAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba AC")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.AcCnt += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.AcCnt} AC".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("tc"), Priority(1)]
        [Summary("zmienia TC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserTcAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba TC")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.TcCnt += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.TcCnt} TC".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("pc"), Priority(1)]
        [Summary("zmienia PC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserPcAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba PC")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.GameDeck.PVPCoins += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.GameDeck.PVPCoins} PC".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ct"), Priority(1)]
        [Summary("zmienia CT użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserCtAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba CT")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.GameDeck.CTCnt += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.GameDeck.CTCnt} CT".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("exp"), Priority(1)]
        [Summary("zmienia punkty doświadczenia użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserExpAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba punktów doświadczenia")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.ExpCnt += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.ExpCnt} punktów doświadczenia.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ost"), Priority(1)]
        [Summary("zmienia liczbę punktów ostrzeżeń")]
        [Remarks("Jeeda 10000")]
        public async Task ChangeUserOstAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba ostrzeżeń")]long amount)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                botuser.Warnings += amount;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.Warnings} punktów ostrzeżeń.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("sime"), Priority(1)]
        [Summary("symuluje wyprawę daną kartą")]
        [Remarks("12312 n")]
        public async Task SimulateExpeditionAsync([Summary("WID")]ulong wid, [Summary("typ wyprawy")]CardExpedition expedition = CardExpedition.None, [Summary("czas w minutach")]int time = -1)
        {
            if (expedition == CardExpedition.None)
                return;

            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetUserAndDontTrackAsync(Context.User.Id);
                var thisCard = botUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null) return;

                if (time > 0)
                {
                    thisCard.ExpeditionDate = DateTime.Now.AddMinutes(-time);
                }

                thisCard.Expedition = expedition;
                var message = _waifu.EndExpedition(botUser, thisCard, true);

                await ReplyAsync("", embed: $"Karta {thisCard.GetString(false, false, true)} wróciła z {expedition.GetName("ej")} wyprawy!\n\n{message}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("kill", RunMode = RunMode.Async)]
        [Summary("wyłącza bota")]
        [Remarks("")]
        public async Task TurnOffAsync()
        {
            await ReplyAsync("", embed: "To dobry czas by umrzeć.".ToEmbedMessage(EMType.Bot).Build());
            await Context.Client.LogoutAsync();
            await Task.Delay(1500);
            Environment.Exit(0);
        }

        [Command("update", RunMode = RunMode.Async)]
        [Summary("wyłącza bota z kodem 200")]
        [Remarks("")]
        public async Task TurnOffWithUpdateAsync()
        {
            await ReplyAsync("", embed: "To już czas?".ToEmbedMessage(EMType.Bot).Build());
            await Context.Client.LogoutAsync();
            System.IO.File.Create("./updateNow");
            await Task.Delay(1500);
            Environment.Exit(200);
        }

        [Command("rmconfig", RunMode = RunMode.Async)]
        [Summary("wyświetla konfiguracje powiadomień na obecnym serwerze")]
        [Remarks("")]
        public async Task ShowRMConfigAsync()
        {
            var serverConfig = Config.Get().RMConfig.Where(x => x.GuildId == Context.Guild.Id || x.GuildId == 0).ToList();
            if (serverConfig.Count > 0)
            {
                await ReplyAsync("", embed: $"**RMC:**\n{string.Join("\n\n", serverConfig)}".TrimToLength(1900).ToEmbedMessage(EMType.Bot).Build());
                return;
            }
            await ReplyAsync("", embed: $"**RMC:**\n\nBrak.".ToEmbedMessage(EMType.Bot).Build());
        }

        [Command("mrmconfig")]
        [Summary("zmienia istniejący wpis w konfiguracji powiadomień w odniesieniu do serwera, lub tworzy nowy gdy go nie ma")]
        [Remarks("News 1232321314 1232412323 tak")]
        public async Task ChangeRMConfigAsync([Summary("typ wpisu")]Api.Models.RichMessageType type, [Summary("id kanału")]ulong channelId, [Summary("id roli")]ulong roleId, [Summary("czy zapisać?")]bool save = false)
        {
            var config = Config.Get();
            var thisRM = config.RMConfig.FirstOrDefault(x => x.Type == type && x.GuildId == Context.Guild.Id);
            if (thisRM == null)
            {
                thisRM = new Config.Model.RichMessageConfig
                {
                    GuildId = Context.Guild.Id,
                    Type = type
                };
                config.RMConfig.Add(thisRM);
            }

            thisRM.ChannelId = channelId;
            thisRM.RoleId = roleId;

            if (save) Config.Save();

            await ReplyAsync("", embed: "Wpis został zmodyfikowany!".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("ignore"), Priority(1)]
        [Summary("dodaje serwer do ignorowanych lub usuwa go z listy")]
        [Remarks("News 1232321314 1232412323 tak")]
        public async Task IgnoreServerAsync()
        {
            var config = Config.Get();
            if (config.BlacklistedGuilds.Contains(Context.Guild.Id))
            {
                config.BlacklistedGuilds.Remove(Context.Guild.Id);
                await ReplyAsync("", embed: "Serwer został usunięty z czarnej listy.".ToEmbedMessage(EMType.Success).Build());
            }
            else
            {
                config.BlacklistedGuilds.Add(Context.Guild.Id);
                await ReplyAsync("", embed: "Serwer został dodany do czarnej listy.".ToEmbedMessage(EMType.Success).Build());
            }
            Config.Save();
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("help", "h")]
        [Summary("wypisuje polecenia")]
        [Remarks("kasuj")]
        public async Task SendHelpAsync([Summary("nazwa polecenia (opcjonalne)")][Remainder]string command = null)
        {
            if (command != null)
            {
                try
                {
                    string prefix = _config.Get().Prefix;
                    if (Context.Guild != null)
                    {
                        using (var db = new Database.GuildConfigContext(_config))
                        {
                            var gConfig = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                            if (gConfig?.Prefix != null) prefix = gConfig.Prefix;
                        }
                    }

                    await ReplyAsync(_helper.GiveHelpAboutPrivateCmd("Debug", command, prefix));
                }
                catch (Exception ex)
                {
                    await ReplyAsync("", embed: ex.Message.ToEmbedMessage(EMType.Error).Build());
                }

                return;
            }

            await ReplyAsync(_helper.GivePrivateHelp("Debug"));
        }
    }
}
