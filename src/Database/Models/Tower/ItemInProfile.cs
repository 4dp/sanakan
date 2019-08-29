#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class ItemInProfile
    {
        public ulong Id { get; set; }
        public long Count { get; set; }
        public bool Active { get; set; }

        public ulong ItemId { get; set; }
        public ulong TowerProfileId { get; set; }

        [JsonIgnore]
        public virtual TowerItem Item { get; set; }
        [JsonIgnore]
        public virtual TowerProfile Profile { get; set; }
    }
}
