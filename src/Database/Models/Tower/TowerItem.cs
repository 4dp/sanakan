#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public enum ItemType
    {
        Helmet,
        Gloves,
        Ring,
        Necklace,
        Shoes,
        Pants,
        Armor,
        Weapon,
        Shield,
        General
    }

    public enum ItemUseType
    {
        Wearable,
        Usable
    }

    public enum ItemRairty
    {
        Legendary,
        Rare,
        Magickal,
        Common
    }

    public class TowerItem
    {
        public ulong Id { get; set; }
        public ulong Level { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public ItemRairty Rarity { get; set; }
        public ItemUseType UseType { get; set; }

        public ulong EffectId { get; set; }
        [JsonIgnore]
        public virtual Effect Effect { get; set; }
    }
}
