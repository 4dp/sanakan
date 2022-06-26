#pragma warning disable 1591

using System;
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
        /// <summary>
        /// Id karty
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// Czy karta jest w talii
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Czy karta jest w klatce
        /// </summary>
        public bool IsInCage { get; set; }
        /// <summary>
        /// Czy karta jest na wyprawie
        /// </summary>
        public bool IsOnExpedition { get; set; }
        /// <summary>
        /// Czy karta można wymienić
        /// </summary>
        public bool IsTradable { get; set; }
        /// <summary>
        /// Czy karta jest unikalna
        /// </summary>
        public bool IsUnique { get; set; }
        /// <summary>
        /// Czy karta jest kartą ultimate
        /// </summary>
        public bool IsUltimate { get; set; }
        /// <summary>
        /// Czy karta posiada niestandardowy obrazek
        /// </summary>
        public bool HasCustomImage { get; set; }
        /// <summary>
        /// Czy karta posiada niestandardową ramkę
        /// </summary>
        public bool HasCustomBorder { get; set; }
        /// <summary>
        /// Ilość punktów doświadczenia na karcie
        /// </summary>
        public double ExpCnt { get; set; }
        /// <summary>
        /// Ilość punktów doświadczenia potrzebnych na następny poziom karty
        /// </summary>
        public double ExpCntForNextLevel { get; set; }
        /// <summary>
        /// Teoretyczna moc karty
        /// </summary>
        public double CardPower { get; set; }
        /// <summary>
        /// Poziom relacji na karcie
        /// </summary>
        public string Affection { get; set; }
        /// <summary>
        /// Liczba dostępnych ulepszeń
        /// </summary>
        public int UpgradesCnt { get; set; }
        /// <summary>
        /// Ile razy została karta zrestartowana
        /// </summary>
        public int RestartCnt { get; set; }
        /// <summary>
        /// Jakość karty
        /// </summary>
        public Rarity Rarity { get; set; }
        /// <summary>
        /// Charakter karty
        /// </summary>
        public Dere Dere { get; set; }
        /// <summary>
        /// Punkty obrony karty
        /// </summary>
        public int Defence { get; set; }
        /// <summary>
        /// Punkty ataku karty
        /// </summary>
        public int Attack { get; set; }
        /// <summary>
        /// Bazowe punkty życia karty
        /// </summary>
        public int BaseHealth { get; set; }
        /// <summary>
        /// Punkty życia karty zmienione o relacje
        /// </summary>
        public int FinalHealth { get; set; }
        /// <summary>
        /// Imię i nazwisko postaci
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Link do postaci
        /// </summary>
        public string CharacterUrl { get; set; }
        /// <summary>
        /// Źródło karty
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Z jakiego tytułu jest postać
        /// </summary>
        public string AnimeTitle { get; set; }
        /// <summary>
        /// Link do obrazka karty
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Link do obrazka karty w pomniejszonym rozmiarze
        /// </summary>
        public string SmallImageUrl { get; set; }
        /// <summary>
        /// Link do obrazka karty który będzie się wyświetlał gdy ustawi się go na waifu
        /// </summary>
        public string ProfileImageUrl { get; set; }
        /// <summary>
        /// Wartość rynkowa wyrażona w trzech stanach
        /// </summary>
        public MarketValue Value { get; set; }
        /// <summary>
        /// Jakość karty poziomu ultimate
        /// </summary>
        public Quality UltimateQuality { get; set; }
        /// <summary>
        /// Data utworzenia karty
        /// </summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>
        /// Tagi znajdujące się na karcie
        /// </summary>
        public List<string> Tags { get; set; }
        /// <summary>
        /// Liczba kc karty
        /// </summary>
        public int WhoWantsCount { get; set; }

        public static CardFinalView ConvertFromRaw(Card card)
        {
            if (card == null) return null;

            return new CardFinalView
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
                CharacterUrl = card.GetCharacterUrl(),
                Source = card.Source.GetString(),
                AnimeTitle = card.Title ?? "????",
                UltimateQuality = card.Quality,
                CreatedAt = card.CreationDate,
                CardPower = card.CardPower,
                WhoWantsCount = card.WhoWantsCount,
                Value = card.GetThreeStateMarketValue(),
                ExpCntForNextLevel = card.ExpToUpgrade(),
                HasCustomImage = card.CustomImage != null,
                HasCustomBorder = card.CustomBorder != null,
                ImageUrl = $"https://cdn2.shinden.eu/{card.Id}.webp",
                IsOnExpedition = card.Expedition != CardExpedition.None,
                SmallImageUrl = $"https://cdn2.shinden.eu/small/{card.Id}.webp",
                ProfileImageUrl = $"https://cdn2.shinden.eu/profile/{card.Id}.webp",
                Tags = card.TagList.Select(x => x.Name).ToList()
            };
        }
    }
}