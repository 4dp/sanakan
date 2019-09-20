#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class SpellInProfile
    {
        public ulong Id { get; set; }
        public int UsesCount { get; set; }

        public ulong SpellId { get; set; }
        public ulong TowerProfileId { get; set; }

        [JsonIgnore]
        public virtual Spell Spell { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
