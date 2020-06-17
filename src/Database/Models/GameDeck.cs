#pragma warning disable 1591

using System;
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
        public ulong ItemsDropped { get; set; }
        public bool WishlistIsPrivate { get; set; }

        public long PVPCoins { get; set; }
        public long GlobalPVPRank { get; set; }
        public long SeasonalPVPRank { get; set; }
        public double MatachMakingRatio { get; set; }
        public ulong PVPDailyGamesPlayed { get; set; }
        public DateTime PVPSeasonBeginDate { get; set; }

        public virtual ICollection<Card> Cards { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<BoosterPack> BoosterPacks { get; set; }
        public virtual ICollection<CardPvPStats> PvPStats { get; set; }
        public virtual ICollection<WishlistObject> Wishes { get; set; }

        public virtual ExpContainer ExpContainer { get; set; }

        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
