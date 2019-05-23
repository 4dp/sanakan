#pragma warning disable 1591

using Discord;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.PocketWaifu;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.Session.Models
{
    public class AcceptDuel : IAcceptActions
    {
        public IMessage Message { get; set; }
        public string DuelName { get; set; }
        public PlayerInfo P1 { get; set; }
        public PlayerInfo P2 { get; set; }

        private Waifu _waifu;
        private IConfig _config;

        public AcceptDuel(Waifu waifu, IConfig config)
        {
            _waifu = waifu;
            _config = config;
        }

        public async Task<bool> OnAccept(SessionContext context)
        {
            var fight = await _waifu.MakeFightAsync(new List<PlayerInfo> { P1, P2 });
            string deathLog = "";
            int roundCnt = 1;

            foreach (var log in fight.Rounds)
            {
                var de = log.Cards.Where(x => x.Hp <= 0);
                if (de.Count() > 0)
                {
                    deathLog += $"**Runda {roundCnt}**:\n";
                    foreach (var d in de)
                    {
                        var thisCard = P1.Cards.FirstOrDefault(x => x.Id == d.CardId);
                        if (thisCard == null) thisCard = P2.Cards.FirstOrDefault(x => x.Id == d.CardId);
                        deathLog += $"❌ {thisCard.GetString(true, false, true)}\n";
                    }
                    deathLog += "\n";
                }
                ++roundCnt;
            }

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = $"{DuelName}{deathLog.TrimToLength(1400)} Zwycięża {fight.Winner.User.Mention}!".ToEmbedMessage(EMType.Error).Build());
            }

            using (var db = new Database.UserContext(_config))
            {
                var user1 = await db.GetUserOrCreateAsync(P1.User.Id);
                var user2 = await db.GetUserOrCreateAsync(P2.User.Id);

                user1.GameDeck.PvPStats.Add(new CardPvPStats
                {
                    Type = FightType.Versus,
                    Result = fight.Winner.User.Id == user1.Id ? FightResult.Win : FightResult.Lose
                });

                user2.GameDeck.PvPStats.Add(new CardPvPStats
                {
                    Type = FightType.Versus,
                    Result = fight.Winner.User.Id == user2.Id ? FightResult.Win : FightResult.Lose
                });

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{user1.Id}", $"user-{user2.Id}","users" });
            }

            Dispose();
            return true;
        }

        public async Task<bool> OnDecline(SessionContext context)
        {
            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = $"{DuelName}{context.User.Mention} odrzucił wyzwanie!".ToEmbedMessage(EMType.Error).Build());
            }

            Dispose();
            return true;
        }

        private void Dispose()
        {
            P1 = null;
            P2 = null;
            DuelName = null;
        }
    }
}
