#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Profil"), RequireUserRole]
    public class Profile : SanakanModuleBase<SocketCommandContext>
    {
        private Database.UserContext _dbUserContext;

        public Profile(Database.UserContext userContext)
        {
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
    }
}