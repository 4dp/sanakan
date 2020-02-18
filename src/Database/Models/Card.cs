#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sanakan.Extensions;

namespace Sanakan.Database.Models
{
    public enum Rarity
    {
        SSS, SS, S, A, B, C, D, E
    }

    public enum Dere
    {
        Tsundere, Kamidere, Deredere, Yandere, Dandere, Kuudere, Mayadere, Bodere, Yami, Raito, Yato
    }

    public enum CardSource
    {
        Activity, Safari, Shop, GodIntervention, Api, Other, Migration, PvE, Daily, Crafting
    }

    public enum StarStyle
    {
        Full, White, Black, Empty, Pig, Snek
    }

    public class Card
    {
        public ulong Id { get; set; }
        public bool Active { get; set; }
        public bool InCage { get; set; }
        public bool IsTradable { get; set; }
        public double ExpCnt { get; set; }
        public double Affection { get; set; }
        public int UpgradesCnt { get; set; }
        public int RestartCnt { get; set; }
        public Rarity Rarity { get; set; }
        public Rarity RarityOnStart { get; set; }
        public Dere Dere { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public string Name { get; set; }
        public ulong Character { get; set; }
        public DateTime CreationDate { get; set; }
        public CardSource Source { get; set; }
        public string Title { get; set; }
        public string Tags { get; set; }
        public string Image { get; set; }
        public string CustomImage { get; set; }
        public ulong FirstIdOwner { get; set; }
        public ulong LastIdOwner { get; set; }
        public bool Unique { get; set; }
        public StarStyle StarStyle { get; set; }
        public string CustomBorder { get; set; }

        public virtual ICollection<CardTag> TagList { get; set; }

        public virtual CardArenaStats ArenaStats { get; set; }

        public ulong GameDeckId { get; set; }
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }

        public override string ToString()
        {
            var marks = new[]
            {
                Unique ? "[U]" : "",
                InCage ? "[C]" : "",
                Active ? "[A]" : "",
                this.IsBroken() ? "[B]" : (this.IsUnusable() ? "[N]" : ""),
            };

            string mark = marks.Any(x => x != "") ? $"**{string.Join("", marks)}** " : "";
            return $"{mark}{this.GetString(false, false, true)}";
        }
    }
}
