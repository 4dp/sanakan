#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Profil"), RequireUserRole]
    public class Profile : SanakanModuleBase<SocketCommandContext>
    {
        private Database.UserContext _dbUserContext;
        private Services.Profile _profile;
        private SessionManager _session;

        public Profile(Database.UserContext userContext, Services.Profile prof, SessionManager session)
        {
            _profile = prof;
            _session = session;
            _dbUserContext = userContext;
        }

        [Command("portfel", RunMode = RunMode.Async)]
        [Alias("wallet")]
        [Summary("wyświetla portfel użytkownika")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowWalletAsync()
        {
            var botuser = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
            await ReplyAsync("", embed: $"**Portfel** {Context.User.Mention}:\n\n {botuser?.ScCnt} **SC**\n{botuser?.TcCnt} **TC**".ToEmbedMessage(EMType.Info).Build());
        }

        [Command("subskrypcje", RunMode = RunMode.Async)]
        [Alias("sub")]
        [Summary("wyświetla daty zakończenia subskrypcji")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowSubsAsync()
        {
            var botuser = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
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

        [Command("statystyki", RunMode = RunMode.Async)]
        [Alias("stats")]
        [Summary("wyświetla statystyki użytkownika")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowStatsAsync()
        {
            var botuser = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
            await ReplyAsync("", embed: $"**Statystyki** {Context.User.Mention}:\n\n{botuser.Stats.ToView().TrimToLength(1950)}".ToEmbedMessage(EMType.Info).Build());
        }

        [Command("topka", RunMode = RunMode.Async)]
        [Alias("top")]
        [Summary("wyświetla topke użytkowników")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowTopAsync([Summary("rodzaj topki(poziom/sc/tc/posty(m/ms)/karty)")]Services.TopType type = Services.TopType.Level)
        {
            var session = new ListSession<string>(Context.User, Context.Client.CurrentUser);
            await _session.KillSessionIfExistAsync(session);

            var users = await _dbUserContext.GetCachedAllUsersAsync();
            session.ListItems = _profile.BuildListView(_profile.GetTopUsers(users, type), type, Context.Guild);

            session.Embed = new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Title = $"Topka {type.Name()}"
            };

            var msg = await ReplyAsync("", embed: session.BuildPage(0));
            await msg.AddReactionsAsync( new [] { new Emoji("⬅"), new Emoji("➡") });

            session.Message = msg;
            await _session.TryAddSession(session);
        }

        [Command("profil", RunMode = RunMode.Async)]
        [Alias("profile")]
        [Summary("wyświetla profil użytkownika")]
        [Remarks("karna")]
        public async Task ShowUserProfileAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            var allUsers = await _dbUserContext.GetCachedAllUsersAsync();
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
}