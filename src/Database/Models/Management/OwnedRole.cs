#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Management
{
    public class OwnedRole
    {
        public ulong Id { get; set; }
        public ulong Role { get; set; }

        public ulong PenaltyInfoId { get; set; }
        [JsonIgnore]
        public virtual PenaltyInfo PenaltyInfo { get; set; }
    }
}
