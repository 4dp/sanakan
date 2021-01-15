#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;
using Sanakan.Extensions;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Informacje o karcie
    /// </summary>
    public class CardFinalView
    {
        public ulong Id { get; set; }
        public bool IsActive { get; set; }
        public bool IsInCage { get; set; }
        public bool IsTradable { get; set; }
        public bool IsUnique { get; set; }
        public bool IsUltimate { get; set; }
        public double ExpCnt { get; set; }
        public string Affection { get; set; }
        public int UpgradesCnt { get; set; }
        public int RestartCnt { get; set; }
        public Rarity Rarity { get; set; }
        public Dere Dere { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public int BaseHealth { get; set; }
        public int FinalHealth { get; set; }
        public string Name { get; set; }
        public ulong CharacterId { get; set; }
        public string Source { get; set; }
        public string AnimeTitle { get; set; }
        public string ImageUrl { get; set; }
        public Quality UltimateQuality { get; set; }

        public List<string> Tags { get; set; }

        public static CardFinalView ConvertFromRaw(Card card) => new CardFinalView
            {
                Id = card.Id,
                IsActive = card.Active,
                IsInCage = card.InCage,
                IsTradable = card.IsTradable,
                IsUnique = card.Unique,
                IsUltimate = card.FromFigure,
                ExpCnt = card.ExpCnt,
                Affection = card.GetAffectionString(),
                UpgradesCnt = card.UpgradesCnt,
                RestartCnt = card.RestartCnt,
                Rarity = card.Rarity,
                Dere = card.Dere,
                Defence = card.GetDefenceWithBonus(),
                Attack = card.GetAttackWithBonus(),
                BaseHealth = card.Health,
                FinalHealth = card.GetHealthWithPenalty(),
                Name = card.Name,
                CharacterId = card.Character,
                Source = card.Source.GetString(),
                AnimeTitle = card.Title ?? "????",
                UltimateQuality = card.Quality,
                ImageUrl = $"https://cdn2.shinden.eu/{card.Id}.png",
                Tags = card.TagList.Select(x => x.Name).ToList()
            };
    }
}