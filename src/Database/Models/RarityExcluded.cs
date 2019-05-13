#pragma warning disable 1591

namespace Sanakan.Database.Models
{
    public class RarityExcluded
    {
        public ulong Id { get; set; }
        public Rarity Rarity { get; set; }

        public ulong BoosterPackId { get; set; }
        public virtual BoosterPack BoosterPack { get; set; }
    }
}
