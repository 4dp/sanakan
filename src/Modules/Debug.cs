#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Debug"), Group("dev"), DontAutoLoad, RequireDev]
    public class Debug : SanakanModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;
        private Services.ImageProcessing _img;
        private Database.UserContext _dbUserContext;

        public Debug(Database.UserContext userContext, Services.Helper helper, Services.ImageProcessing img)
        {
            _dbUserContext = userContext;
            _helper = helper;
            _img = img;
        }

        [Command("lvlbadge", RunMode = RunMode.Async)]
        [Summary("generuje przykładowy obrazek otrzymania poziomu")]
        [Remarks("")]
        public async Task GenerateLevelUpBadgeAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            using (var badge = await _img.GetLevelUpBadgeAsync("Very very long nickname of trolly user", 
                2154, usr.GetAvatarUrl(), usr.Roles.OrderByDescending(x => x.Position).First().Color))
            {
                using (var badgeStream = badge.ToPngStream())
                {
                    await Context.Channel.SendFileAsync(badgeStream, $"{usr.Id}.png");
                }
            }
        }

        [Command("sc")]
        [Summary("zmienia SC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserScAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba SC")]long amount)
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            botuser.ScCnt += amount;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ma teraz {botuser.ScCnt} SC".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tc")]
        [Summary("zmienia TC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserTcAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba TC")]long amount)
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            botuser.TcCnt += amount;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ma teraz {botuser.TcCnt} TC".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("help", "h")]
        [Summary("wypisuje polecenia")]
        [Remarks("kasuj"), RequireAdminOrModRole]
        public async Task SendHelpAsync([Summary("nazwa polecenia(opcjonalne)")][Remainder]string command = null)
        {
            if (command != null)
            {
                try
                {
                    await ReplyAsync(_helper.GiveHelpAboutPrivateCmd("Debug", command));
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
