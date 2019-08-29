#pragma warning disable 1591

using Newtonsoft.Json;

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

        public ulong ProfileId { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
