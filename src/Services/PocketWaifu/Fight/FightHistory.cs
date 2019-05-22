#pragma warning disable 1591

using System.Collections.Generic;

namespace Sanakan.Services.PocketWaifu.Fight
{
    public class FightHistory
    {
        public FightHistory(PlayerInfo winner)
        {
            Winner = winner;
            Rounds = new List<RoundInfo>();
        }

        public PlayerInfo Winner { get; set; }
        public List<RoundInfo> Rounds { get; set; }
    }
}
