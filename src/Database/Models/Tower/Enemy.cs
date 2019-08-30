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

    public enum LootType
    {
        None,
        Character,
        Title,
        WaifuItem,
        TowerItem
    }

    public class Enemy
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public EnemyType Type { get; set; }
        public ulong Level { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public int Energy { get; set; }
        public LootType LootType { get; set; }
        public string Loot { get; set; }
        public Dere Dere { get; set; }

        public virtual ICollection<SpellInEnemy> Spells { get; set; }

        public ulong? ProfileId { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
