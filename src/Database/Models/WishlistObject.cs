#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public enum WishlistObjectType
    {
        Card, Title, Character
    }

    public class WishlistObject
    {
        public ulong Id { get; set; }
        public ulong ObjectId { get; set; }
        public string ObjectName { get; set; }
        public WishlistObjectType Type { get; set; }

        public ulong GameDeckId { get; set; }
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }
    }
}
