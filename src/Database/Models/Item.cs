#pragma warning disable 1591

namespace Sanakan.Database.Models
{
    public enum ItemType
    {
        AffectionRecoverySmall,
        AffectionRecoveryNormal,
        AffectionRecoveryBig,
        IncreaseUpgradeCnt,
        CardParamsReRoll,
        DereReRoll,
        RandomBoosterPackSingleE,
        RandomNormalBoosterPackB,
        RandomTitleBoosterPackSingleE,
    }

    public class Item
    {
        public ulong Id { get; set; }
        public long Count { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }

        public ulong GameDeckId { get; set; }
        public virtual GameDeck GameDeck { get; set; }
    }
}
