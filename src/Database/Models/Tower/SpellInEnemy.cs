#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class SpellInEnemy
    {
        public ulong Id { get; set; }

        public ulong SpellId { get; set; }
        public ulong EnemyId { get; set; }

        [JsonIgnore]
        public virtual Spell Spell { get; set; }
        [JsonIgnore]
        public virtual Enemy Enemy { get; set; }
    }
}
