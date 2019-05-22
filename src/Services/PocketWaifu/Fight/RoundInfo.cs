#pragma warning disable 1591

using System.Collections.Generic;

namespace Sanakan.Services.PocketWaifu.Fight
{
    public class RoundInfo
    {
        public RoundInfo()
        {
            Cards = new List<HpSnapshot>();
            Fights = new List<AttackInfo>();
        }

        public List<HpSnapshot> Cards { get; set; }
        public List<AttackInfo> Fights { get; set; }
    }
}
