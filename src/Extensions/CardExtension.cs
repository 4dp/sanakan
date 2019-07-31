#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sanakan.Database.Models;
using Sanakan.Services.PocketWaifu;

namespace Sanakan.Extensions
{
    public static class CardExtension
    {
        public static string GetString(this Card card, bool withoutId = false, bool withUpgrades = false, bool nameAsUrl = false, bool allowZero = false)
        {
            string idStr = withoutId ? "" : $"**[{card.Id}]** ";
            string upgCnt = withUpgrades ? $"_(U:{card.UpgradesCnt})_" : "";
            string name = nameAsUrl ? card.GetNameWithUrl() : card.Name;

            return $"{idStr} {name} **{card.Rarity}** ❤{card.GetHealthWithPenalty(allowZero)} 🔥{card.GetAttackWithBonus()} 🛡{card.GetDefenceWithBonus()} {upgCnt}";
        }

        public static string GetNameWithUrl(this Card card) => $"[{card.Name}]({card.GetCharacterUrl()})";

        public static string GetCharacterUrl(this Card card) => Shinden.API.Url.GetCharacterURL(card.Character);

        public static int GetValue(this Card card)
        {
            switch (card.Rarity)
            {
                case Rarity.SSS: return 50;
                case Rarity.SS:  return 25;
                case Rarity.S:   return 15;
                case Rarity.A:   return 10;
                case Rarity.B:   return 7;
                case Rarity.C:   return 5;
                case Rarity.D:   return 3;

                default:
                case Rarity.E: return 1;
            }
        }

        public static double GetCardPower(this Card card)
        {
            return (card.GetHealthWithPenalty() * 0.018)
                   + (card.GetAttackWithBonus() * 0.019)
                   + (card.GetDefenceWithBonus() * 2.76);
        }

        public static string GetStatusIcons(this Card card)
        {
            var icons = new List<string>();
            if (!card.IsTradable) icons.Add("⛔");
            if (card.IsBroken()) icons.Add("💔");
            if (card.InCage) icons.Add("🔒");

            if (card.Tags != null)
            {
                if (card.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase))
                    icons.Add("💗");

                if (card.Tags.Contains("rezerwacja", StringComparison.CurrentCultureIgnoreCase))
                    icons.Add("📝");

                if (card.Tags.Contains("wymiana", StringComparison.CurrentCultureIgnoreCase) && icons.Count == 0)
                    icons.Add("🔄");
            }

            return string.Join(" ", icons);
        }

        public static string GetDesc(this Card card)
        {
            return $"*{card.Title ?? "????"}*\n\n"
                + $"**Relacja:** {card.GetAffectionString()}\n"
                + $"**Doświadczenie:** {card.ExpCnt.ToString("F")}\n"
                + $"**Dostępne ulepszenia:** {card.UpgradesCnt}\n\n"
                + $"**W klatce:** {card.InCage.GetYesNo()}\n"
                + $"**Aktywna:** {card.Active.GetYesNo()}\n"
                + $"**Możliwość wymiany:** {card.IsTradable.GetYesNo()}\n\n"
                + $"**Arena:** **W**: {card?.ArenaStats?.Wins ?? 0} **L**: {card?.ArenaStats?.Loses ?? 0} **D**: {card?.ArenaStats?.Draws ?? 0}\n\n"
                + $"**WID:** {card.Id}\n"
                + $"**Restarty:** {card.RestartCnt}\n"
                + $"**Pochodzenie:** {card.Source.GetString()}\n"
                + $"**Tagi:** {card.Tags ?? "---"}\n\n";
        }

        public static int GetHealthWithPenalty(this Card card, bool allowZero = false)
        {
            var percent = card.Affection * 5d / 100d;
            var newHealth = (int) (card.Health + (card.Health * percent));
            if (newHealth > 999) newHealth = 999;

            if (allowZero)
            {
                if (newHealth < 0)
                    newHealth = 0;
            }
            else
            {
                if (newHealth < 10)
                    newHealth = 10;
            }

            return newHealth;
        }

        public static int GetAttackWithBonus(this Card card)
            => card.Attack + (card.RestartCnt * 2);

        public static int GetDefenceWithBonus(this Card card)
        {
            var newDefence = card.Defence + card.RestartCnt;
            if (newDefence > 99) newDefence = 99;
            return newDefence;
        }

        public static string GetString(this CardSource source)
        {
            switch (source)
            {
                case CardSource.Activity:        return "Aktywność";
                case CardSource.Safari:          return "Safari";
                case CardSource.Shop:            return "Sklepik";
                case CardSource.GodIntervention: return "Czity";
                case CardSource.Api:             return "Nieznane";
                case CardSource.Migration:       return "Stara baza";
                case CardSource.PvE:             return "Walki na boty";

                default:
                case CardSource.Other: return "Inne";
            }
        }

        public static string GetYesNo(this bool b) => b ? "Tak" : "Nie";

        public static bool CanFightOnPvEGMwK(this Card card) => card.Affection > -80;

        public static bool CanGiveRing(this Card card) => card.Affection >= 5;
        public static bool HasNoNegativeEffectAfterBloodUsage(this Card card) => card.Affection >= 4;

        public static bool CanGiveBloodOrUpgradeToSSS(this Card card) => card.Affection >= 50;

        public static bool IsBroken(this Card card) => card.Affection <= -50;

        public static bool IsUnusable(this Card card) => card.Affection <= -5;

        public static string GetAffectionString(this Card card)
        {
            if (card.Affection <= -50) return "Pogarda";
            if (card.Affection <= -5)  return "Nienawiść";
            if (card.Affection <= -4)  return "Zawiść";
            if (card.Affection <= -3)  return "Wrogość";
            if (card.Affection <= -2)  return "Złośliwość";
            if (card.Affection <= -1)  return "Chłodność";
            if (card.Affection >= 50)  return "Obsesyjna miłość";
            if (card.Affection >= 5)   return "Miłość";
            if (card.Affection >= 4)   return "Zauroczenie";
            if (card.Affection >= 3)   return "Przyjaźń";
            if (card.Affection >= 2)   return "Fascynacja";
            if (card.Affection >= 1)   return "Zaciekawienie";
            return "Obojętność";
        }

        public static bool IsWeakTo(this Card card, Dere dere)
        {
            if (dere == Dere.Yato && card.Dere != Dere.Yato)
                return true;

            switch (card.Dere)
            {
                case Dere.Tsundere:
                    if (dere != Dere.Tsundere)
                        return true;
                    return false;

                case Dere.Kamidere:
                    if (dere == Dere.Deredere)
                        return true;
                    return false;

                case Dere.Deredere:
                    if (dere == Dere.Yandere)
                        return true;
                    return false;

                case Dere.Yandere:
                    if (dere == Dere.Dandere)
                        return true;
                    return false;

                case Dere.Dandere:
                    if (dere == Dere.Kuudere)
                        return true;
                    return false;

                case Dere.Kuudere:
                    if (dere == Dere.Mayadere)
                        return true;
                    return false;

                case Dere.Mayadere:
                    if (dere == Dere.Bodere)
                        return true;
                    return false;

                case Dere.Bodere:
                    if (dere == Dere.Kamidere)
                        return true;
                    return false;

                case Dere.Yami:
                    if (dere == Dere.Raito)
                        return true;
                    return false;

                case Dere.Raito:
                    if (dere == Dere.Yami)
                        return true;
                    return false;

                case Dere.Yato:
                    return false;

                default:
                    return false;
            }
        }

        public static bool IsResistTo(this Card card, Dere dere)
        {
            if (card.Dere == dere && dere != Dere.Yato)
                return true;

            switch (card.Dere)
            {
                case Dere.Tsundere:
                    return false;

                case Dere.Kamidere:
                    if (dere == Dere.Yandere)
                        return true;
                    return false;

                case Dere.Deredere:
                    if (dere == Dere.Dandere)
                        return true;
                    return false;

                case Dere.Yandere:
                    if (dere == Dere.Kuudere)
                        return true;
                    return false;

                case Dere.Dandere:
                    if (dere == Dere.Mayadere)
                        return true;
                    return false;

                case Dere.Kuudere:
                    if (dere == Dere.Bodere)
                        return true;
                    return false;

                case Dere.Mayadere:
                    if (dere == Dere.Kamidere)
                        return true;
                    return false;

                case Dere.Bodere:
                    if (dere == Dere.Deredere)
                        return true;
                    return false;

                case Dere.Yami:
                    if (dere != Dere.Raito)
                        return true;
                    return false;

                case Dere.Raito:
                    if (dere != Dere.Yami)
                        return true;
                    return false;

                case Dere.Yato:
                    return true;

                default:
                    return false;
            }
        }

        public static double ExpToUpgrade(this Card card)
        {
            switch (card.Rarity)
            {
                case Rarity.SS:
                    return 100;

                default: return 30;
            }
        }

        public static int GetAttackMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 100;
                case Rarity.SS:  return 90;
                case Rarity.S:   return 80;
                case Rarity.A:   return 65;
                case Rarity.B:   return 50;
                case Rarity.C:   return 32;
                case Rarity.D:   return 20;

                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetDefenceMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 88;
                case Rarity.SS:  return 77;
                case Rarity.S:   return 68;
                case Rarity.A:   return 60;
                case Rarity.B:   return 50;
                case Rarity.C:   return 32;
                case Rarity.D:   return 15;

                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetHealthMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 100;
                case Rarity.SS:  return 90;
                case Rarity.S:   return 80;
                case Rarity.A:   return 70;
                case Rarity.B:   return 60;
                case Rarity.C:   return 50;
                case Rarity.D:   return 40;

                case Rarity.E:
                default: return 30;
            }
        }

        public static int GetHealthMax(this Card card)
        {
            return 300 - (card.Attack + card.Defence);
        }

        public static int GetAttackMax(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 130;
                case Rarity.SS:  return 100;
                case Rarity.S:   return 96;
                case Rarity.A:   return 87;
                case Rarity.B:   return 84;
                case Rarity.C:   return 68;
                case Rarity.D:   return 50;

                case Rarity.E:
                default: return 35;
            }
        }

        public static int GetDefenceMax(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 96;
                case Rarity.SS:  return 91;
                case Rarity.S:   return 79;
                case Rarity.A:   return 75;
                case Rarity.B:   return 70;
                case Rarity.C:   return 65;
                case Rarity.D:   return 53;

                case Rarity.E:
                default: return 38;
            }
        }

        public static async Task<CardInfo> GetCardInfoAsync(this Card card, Shinden.ShindenClient client)
        {
            var response = await client.GetCharacterInfoAsync(card.Character);
            if (!response.IsSuccessStatusCode())
                throw new Exception($"Couldn't get card info!");

            return new CardInfo
            {
                Info = response.Body,
                Card = card
            };
        }
    }
}