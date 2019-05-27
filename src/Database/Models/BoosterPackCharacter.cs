#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public class BoosterPackCharacter
    {
        public ulong Id { get; set; }
        public ulong Character { get; set; }

        public ulong BoosterPackId { get; set; }
        [JsonIgnore]
        public virtual BoosterPack BoosterPack { get; set; }
    }
}
