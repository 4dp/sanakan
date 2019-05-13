#pragma warning disable 1591

namespace Sanakan.Database.Models.Management
{
    public class OwnedRole
    {
        public ulong Id { get; set; }
        public ulong Role { get; set; }

        public ulong PenaltyInfoId { get; set; }
        public virtual PenaltyInfo PenaltyInfo { get; set; }
    }
}
