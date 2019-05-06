using System.Collections.Generic;

namespace Sanakan.Database.Models
{
    public class GameDeck
    {
        public ulong Id { get; set; }
        public ulong Waifu { get; set; }

        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<BoosterPack> BoosterPacks { get; set; }
        public virtual ICollection<CardPvPStats> PvPStats { get; set; }

        public ulong UserId { get; set; }
        public virtual User User { get; set; }
    }
}
