#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Profil"), RequireUserRole]
    public class Profile : SanakanModuleBase<SocketCommandContext>
    {
        private Services.Profile _profile;
        private SessionManager _session;

        public Profile(Services.Profile prof, SessionManager session)
        {
            _profile = prof;
            _session = session;
        }

        [Command("portfel", RunMode = RunMode.Async)]
        [Alias("wallet")]
        [Summary("wyświetla portfel użytkownika")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowWalletAsync([Summary("użytkownik(opcjonalne)")]SocketUser user = null)
        {
            var usr = user ?? Context.User;
            if (usr == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetCachedFullUserAsync(usr.Id);
                if (botuser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                await ReplyAsync("", embed: $"**Portfel** {usr.Mention}:\n\n {botuser?.ScCnt} **SC**\n{botuser?.TcCnt} **TC**".ToEmbedMessage(EMType.Info).Build());
            }
        }

        [Command("subskrypcje", RunMode = RunMode.Async)]
        [Alias("sub")]
        [Summary("wyświetla daty zakończenia subskrypcji")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowSubsAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetCachedFullUserAsync(Context.User.Id);
                var rsubs = botuser.TimeStatuses.Where(x => x.Type.IsSubType());

                string subs = "brak";
                if (rsubs.Count() > 0)
                {
                    subs = "";
                    foreach (var sub in rsubs)
                        subs += $"{sub.ToView()}\n";
                }

                await ReplyAsync("", embed: $"**Subskrypcje** {Context.User.Mention}:\n\n{subs.TrimToLength(1950)}".ToEmbedMessage(EMType.Info).Build());
            }
        }

        [Command("przyznaj role", RunMode = RunMode.Async)]
        [Alias("add role")]
        [Summary("dodaje samozarządzaną role")]
        [Remarks("newsy"), RequireCommandChannel]
        public async Task AddRoleAsync([Summary("nazwa roli z wypisz role")]string name)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                var selfRole = config.SelfRoles.FirstOrDefault(x => x.Name == name);
                var gRole = Context.Guild.GetRole(selfRole?.Role ?? 0);

                if (gRole == null)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono roli `{name}`".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!user.Roles.Contains(gRole))
                    await user.AddRoleAsync(gRole);

                await ReplyAsync("", embed: $"{user.Mention} przyznano role: `{name}`".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("zdejmij role", RunMode = RunMode.Async)]
        [Alias("remove role")]
        [Summary("zdejmuje samozarządzaną role")]
        [Remarks("newsy"), RequireCommandChannel]
        public async Task RemoveRoleAsync([Summary("nazwa roli z wypisz role")]string name)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                var selfRole = config.SelfRoles.FirstOrDefault(x => x.Name == name);
                var gRole = Context.Guild.GetRole(selfRole?.Role ?? 0);

                if (gRole == null)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono roli `{name}`".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (user.Roles.Contains(gRole))
                    await user.RemoveRoleAsync(gRole);

                await ReplyAsync("", embed: $"{user.Mention} zdjęto role: `{name}`".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("wypisz role", RunMode = RunMode.Async)]
        [Summary("wypisuje samozarządzane role")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowRolesAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config.SelfRoles.Count < 1)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono roli.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                string stringRole = "";
                foreach (var selfRole in config.SelfRoles)
                {
                    var gRole = Context.Guild.GetRole(selfRole?.Role ?? 0);
                    stringRole += $" `{selfRole.Name}` ";
                }

                await ReplyAsync($"**Dostępne role:**\n{stringRole}\n\nUżyj `s.przyznaj role [nazwa]` aby dodać lub `s.zdejmij role [nazwa]` odebrać sobie role.");
            }
        }

        [Command("statystyki", RunMode = RunMode.Async)]
        [Alias("stats")]
        [Summary("wyświetla statystyki użytkownika")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowStatsAsync([Summary("użytkownik(opcjonalne)")]SocketUser user = null)
        {
            var usr = user ?? Context.User;
            if (usr == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetCachedFullUserAsync(usr.Id);
                if (botuser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                await ReplyAsync("", embed: botuser.GetStatsView(usr).Build());
            }
        }

        [Command("topka", RunMode = RunMode.Async)]
        [Alias("top")]
        [Summary("wyświetla topke użytkowników")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowTopAsync([Summary("rodzaj topki(poziom/sc/tc/posty(m/ms)/kart(a/y/ym)/karma(-))")]TopType type = TopType.Level)
        {
            var session = new ListSession<string>(Context.User, Context.Client.CurrentUser);
            await _session.KillSessionIfExistAsync(session);

            using (var db = new Database.UserContext(Config))
            {
                var users = await db.GetCachedAllUsersAsync();
                session.ListItems = _profile.BuildListView(_profile.GetTopUsers(users, type), type, Context.Guild);
            }

            session.Event = ExecuteOn.ReactionAdded;
            session.Embed = new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Title = $"Topka {type.Name()}"
            };

            var msg = await ReplyAsync("", embed: session.BuildPage(0));
            await msg.AddReactionsAsync(new[] { new Emoji("⬅"), new Emoji("➡") });

            session.Message = msg;
            await _session.TryAddSession(session);
        }

        [Command("widok waifu")]
        [Alias("waifu view")]
        [Summary("przełącza widoczność waifu na pasku bocznym profilu użytkownika")]
        [Remarks(""), RequireCommandChannel]
        public async Task ToggleWaifuViewInProfileAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                botuser.ShowWaifuInProfile = !botuser.ShowWaifuInProfile;

                string result = botuser.ShowWaifuInProfile ? "załączony" : "wyłączony";

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"Podgląd waifu w profilu {Context.User.Mention} został {result}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("profil", RunMode = RunMode.Async)]
        [Alias("profile")]
        [Summary("wyświetla profil użytkownika")]
        [Remarks("karna")]
        public async Task ShowUserProfileAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var allUsers = await db.GetCachedAllUsersAsync();
                var botUser = allUsers.FirstOrDefault(x => x.Id == usr.Id);
                if (botUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var stream = await _profile.GetProfileImageAsync(usr, botUser, allUsers.OrderByDescending(x => x.ExpCnt).ToList().IndexOf(botUser) + 1))
                {
                    await Context.Channel.SendFileAsync(stream, $"{usr.Id}.png");
                }
            }
        }

        [Command("styl")]
        [Alias("style")]
        [Summary("zmienia styl profilu (koszt 3000 SC)")]
        [Remarks("1 https://i.imgur.com/8UK8eby.png"), RequireCommandChannel]
        public async Task ChangeStyleAsync([Summary("typ stylu ( 0 - statystyki, 1 - obrazek, 2 - brzydkie)")]ProfileType type, [Summary("bezpośredni adres do obrazka gdy wybrany styl 1 lub 2(325 x 272)")]string imgUrl = null)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.ScCnt < 3000)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby SC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                switch (type)
                {
                    case ProfileType.Img:
                    case ProfileType.StatsWithImg:
                        if (await _profile.SaveProfileImageAsync(imgUrl, $"./GOut/Saved/SR{botuser.Id}.png", 325, 272))
                        {
                            botuser.StatsReplacementProfileUri = $"./GOut/Saved/SR{botuser.Id}.png";
                            break;
                        }
                        await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                        return;

                    default:
                        break;
                }

                botuser.ScCnt -= 3000;
                botuser.ProfileType = type;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"Zmieniono styl profilu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("tło")]
        [Alias("tlo", "bg", "background")]
        [Summary("zmienia obrazek tła profilu (koszt 5000 SC)")]
        [Remarks("https://i.imgur.com/LjVxiv8.png"), RequireCommandChannel]
        public async Task ChangeBackgroundAsync([Summary("bezpośredni adres do obrazka (450 x 150)")]string imgUrl)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.ScCnt < 5000)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby SC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (await _profile.SaveProfileImageAsync(imgUrl, $"./GOut/Saved/BG{botuser.Id}.png", 450, 150, true))
                {
                    botuser.BackgroundProfileUri = $"./GOut/Saved/BG{botuser.Id}.png";
                }
                else
                {
                    await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.ScCnt -= 5000;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"Zmieniono tło profilu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("globalki")]
        [Alias("global")]
        [Summary("nadaje na miesiąc rangę od globalnych emotek (4000 TC)")]
        [Remarks(""), RequireCommandChannel]
        public async Task AddGlobalEmotesAsync()
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                if (botuser.TcCnt < 4000)
                {
                    await ReplyAsync("", embed: $"{user.Mention} nie posiadasz wystarczającej liczby TC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var cdb = new Database.GuildConfigContext(Config))
                {
                    var gConfig = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                    var gRole = Context.Guild.GetRole(gConfig.GlobalEmotesRole);
                    if (gRole == null)
                    {
                        await ReplyAsync("", embed: "Serwer nie ma ustawionej roli globalnych emotek.".ToEmbedMessage(EMType.Bot).Build());
                        return;
                    }

                    var global = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Globals && x.Guild == Context.Guild.Id);
                    if (global == null)
                    {
                        global = new Database.Models.TimeStatus
                        {
                            Type = StatusType.Globals,
                            Guild = Context.Guild.Id,
                            EndsAt = DateTime.Now,
                        };
                        botuser.TimeStatuses.Add(global);
                    }

                    if (!user.Roles.Contains(gRole))
                        await user.AddRoleAsync(gRole);

                    global.EndsAt = global.EndsAt.AddMonths(1);
                    botuser.TcCnt -= 4000;
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} wykupił miesiąc globalnych emotek!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("kolor")]
        [Alias("color", "colour")]
        [Summary("zmienia kolor użytkownika (koszt TC na liście/30000 SC)")]
        [Remarks("pink"), RequireCommandChannel]
        public async Task ToggleColorRoleAsync([Summary("kolor z listy(brak - lista)")]FColor color = FColor.None, [Summary("waluta(SC/TC)")]SCurrency currency = SCurrency.Tc)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            if (color == FColor.None)
            {
                using (var img = _profile.GetColorList(currency))
                {
                    await Context.Channel.SendFileAsync(img, "list.png");
                    return;
                }
            }

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(user.Id);
                var points = currency == SCurrency.Tc ? botuser.TcCnt : botuser.ScCnt;
                if (points < color.Price(currency))
                {
                    await ReplyAsync("", embed: $"{user.Mention} nie posiadasz wystarczającej liczby {currency.ToString().ToUpper()}!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var colort = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Color && x.Guild == Context.Guild.Id);
                if (colort == null)
                {
                    colort = new Database.Models.TimeStatus
                    {
                        Type = Database.Models.StatusType.Color,
                        Guild = Context.Guild.Id,
                        EndsAt = DateTime.Now,
                    };
                    botuser.TimeStatuses.Add(colort);
                }

                await _profile.RomoveUserColorAsync(user);

                if (color == FColor.CleanColor)
                {
                    colort.EndsAt = DateTime.Now;
                }
                else
                {
                    using (var cdb = new Database.GuildConfigContext(Config))
                    {
                        var gConfig = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                        if (!await _profile.SetUserColorAsync(user, gConfig.AdminRole, color))
                        {
                            await ReplyAsync("", embed: $"Coś poszło nie tak!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }

                        colort.EndsAt = colort.EndsAt.AddMonths(1);

                        if (currency == SCurrency.Tc)
                            botuser.TcCnt -= color.Price(currency);
                        else
                            botuser.ScCnt -= color.Price(currency);
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} wykupił kolor!".ToEmbedMessage(EMType.Success).Build());
            }
        }
    }
}