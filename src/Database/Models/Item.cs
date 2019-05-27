#pragma warning disable 1591

using Newtonsoft.Json;

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
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }
    }
}
