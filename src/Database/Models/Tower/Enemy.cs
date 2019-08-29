#pragma warning disable 1591

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sanakan.Database.Models.Tower
{
    public enum EnemyType
    {
        Normall,
        Boss
    }

    public class Enemy
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public EnemyType Type { get; set; }
        public long Level { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Energy { get; set; }

        public virtual ICollection<SpellInEnemy> Spells { get; set; }

        public ulong ProfileId { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
