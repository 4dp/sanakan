#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Zabawy"), RequireUserRole]
    public class Fun : SanakanModuleBase<SocketCommandContext>
    {
        private Database.UserContext _dbUserContext;

        public Fun(Database.UserContext userContext)
        {
            _dbUserContext = userContext;
        }

        [Command("drobne")]
        [Alias("daily")]
        [Summary("dodaje dzienną dawkę drobniaków do twojego portfela")]
        [Remarks(""), RequireCommandChannel]
        public async Task GivedDailyScAsync()
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var daily = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Daily);
            if (daily == null)
            {
                daily = new Database.Models.TimeStatus
                {
                    Type = Database.Models.StatusType.Daily,
                    EndsAt = DateTime.MinValue
                };
                botuser.TimeStatuses.Add(daily);
            }

            if (daily.IsActive())
            {
                var timeTo = (int)daily.RemainingMinutes();
                await ReplyAsync("", embed: $"{Context.User.Mention} następne drobne możesz otrzymać dopiero za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            daily.EndsAt = DateTime.Now.AddHours(20);
            botuser.ScCnt += 100;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}" });

            await ReplyAsync("", embed: $"{Context.User.Mention} łap drobne na waciki!".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("zaskórniaki")]
        [Alias("hourly", "zaskorniaki")]
        [Summary("upadłeś tak nisko, że prosisz o SC pod marketem")]
        [Remarks(""), RequireCommandChannel]
        public async Task GivedHourlyScAsync()
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var hourly = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Hourly);
            if (hourly == null)
            {
                hourly = new Database.Models.TimeStatus
                {
                    Type = Database.Models.StatusType.Hourly,
                    EndsAt = DateTime.MinValue
                };
                botuser.TimeStatuses.Add(hourly);
            }

            if (hourly.IsActive())
            {
                var timeTo = (int)hourly.RemainingSeconds();
                await ReplyAsync("", embed: $"{Context.User.Mention} następne zaskórniaki możesz otrzymać dopiero za {timeTo / 60}m {timeTo % 60}s!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            hourly.EndsAt = DateTime.Now.AddHours(1);
            botuser.ScCnt += 5;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{Context.User.Mention} łap piątaka!".ToEmbedMessage(EMType.Success).Build());
        }
    }
}