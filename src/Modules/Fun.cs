﻿#pragma warning disable 1591

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
        private Services.Fun _fun;

        public Fun(Database.UserContext userContext, Services.Fun fun)
        {
            _dbUserContext = userContext;
            _fun = fun;
        }

        [Command("drobne")]
        [Alias("daily")]
        [Summary("dodaje dzienną dawkę drobniaków do twojego portfela")]
        [Remarks(""), RequireCommandChannel]
        public async Task GiveDailyScAsync()
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
        public async Task GiveHourlyScAsync()
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

        [Command("rzut")]
        [Alias("beat", "toss")]
        [Summary("bot wykonuje rzut monetą, wygrywasz kwotę o którą się założysz")]
        [Remarks("reszka 10"), RequireCommandChannel]
        public async Task TossCoinAsync([Summary("strona monety(orzeł/reszka)")]Services.CoinSide side, [Summary("ilość SC (maks. stawka 10000)")]int amount)
        {
            if (amount <= 0 || amount > 10000)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} możesz rzucić za maksymalnie 10000 SC!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var botuser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            if (botuser.ScCnt < amount)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby SC!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            botuser.ScCnt -= amount;
            var thrown = _fun.RandomizeSide();
            var embed = $"{Context.User.Mention} pudło! Obecnie posiadasz {botuser.ScCnt} SC.".ToEmbedMessage(EMType.Error);

            botuser.Stats.Tail += (thrown == Services.CoinSide.Tail) ? 1 : 0;
            botuser.Stats.Head += (thrown == Services.CoinSide.Head) ? 1 : 0;

            if (thrown == side)
            {
                ++botuser.Stats.Hit;
                botuser.ScCnt += amount * 2;
                botuser.Stats.IncomeInSc += amount;
                embed = $"{Context.User.Mention} trafiony zatopiony! Obecnie posiadasz {botuser.ScCnt} SC.".ToEmbedMessage(EMType.Success);
            }
            else
            {
                ++botuser.Stats.Misd;
                botuser.Stats.ScLost += amount;
                botuser.Stats.IncomeInSc -= amount;
            }

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: embed.Build());
            await Context.Channel.SendFileAsync($"./Pictures/coin{(int)thrown}.png");
        }
    }
}