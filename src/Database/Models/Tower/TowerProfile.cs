#pragma warning disable 1591

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class TowerProfile
    {
        public ulong Id { get; set; }
        public long Level { get; set; }
        public long ExpCnt { get; set; }
        public int ActionPoints { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Energy { get; set; }
        public int Luck { get; set; }

        public ulong MaxFloor { get; set; }
        public string ConqueredRoomsFromFloor { get; set; }

        public virtual ICollection<Enemy> Enemies { get; set; }
        public virtual ICollection<ItemInProfile> Items { get; set; }
        public virtual ICollection<SpellInProfile> Spells { get; set; }
        public virtual ICollection<EffectInProfile> ActiveEffects { get; set; }

        public ulong CardId { get; set; }
        public ulong CurrentRoomId { get; set; }
        [JsonIgnore]
        public virtual Card Card { get; set; }
        [JsonIgnore]
        public virtual Room CurrentRoom { get; set; }
    }
}
