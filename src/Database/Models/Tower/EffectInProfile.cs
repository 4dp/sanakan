#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class EffectInProfile
    {
        public ulong Id { get; set; }
        public long Remaining { get; set; }
        public int Multiplier { get; set; }

        public ulong EffectId { get; set; }
        public ulong TowerProfileId { get; set; }

        [JsonIgnore]
        public virtual Effect Effect { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
