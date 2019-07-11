#pragma warning disable 1591

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public class GameDeck
    {
        public ulong Id { get; set; }
        public long CTCnt { get; set; }
        public ulong Waifu { get; set; }
        public double Karma { get; set; }
        public string Wishlist { get; set; }

        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<BoosterPack> BoosterPacks { get; set; }
        public virtual ICollection<CardPvPStats> PvPStats { get; set; }

        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
