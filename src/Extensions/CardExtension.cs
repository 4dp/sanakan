#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class CardExtension
    {
        public static string GetString(this Card card, bool withoutId = false, bool withUpgrades = false, bool nameAsUrl = false, bool allowZero = false, bool showBaseHp = false)
        {
            string idStr = withoutId ? "" : $"**[{card.Id}]** ";
            string name = nameAsUrl ? card.GetNameWithUrl() : card.Name;
            string upgCnt = withUpgrades ? $"_(U:{card.UpgradesCnt})_" : "";

            return $"{idStr} {name} **{card.Rarity}** {card.GetCardParams(showBaseHp, allowZero)} {upgCnt}";
        }

        public static string GetCardParams(this Card card, bool showBaseHp = false, bool allowZero = false, bool inNewLine = false)
        {
            string hp = showBaseHp ? $"**({card.Health})**{card.GetHealthWithPenalty(allowZero)}" : $"{card.GetHealthWithPenalty(allowZero)}";
            var param = new string[] { $"❤{hp}", $"🔥{card.GetAttackWithBonus()}", $"🛡{card.GetDefenceWithBonus()}" };

            return string.Join(inNewLine ? "\n" : " ", param);
        }

        public static string GetNameWithUrl(this Card card) => $"[{card.Name}]({card.GetCharacterUrl()})";

        public static string GetCharacterUrl(this Card card) => Shinden.API.Url.GetCharacterURL(card.Character);

        public static int GetValue(this Card card)
        {
            switch (card.Rarity)
            {
                case Rarity.SSS: return 50;
                case Rarity.SS: return 25;
                case Rarity.S: return 15;
                case Rarity.A: return 10;
                case Rarity.B: return 7;
                case Rarity.C: return 5;
                case Rarity.D: return 3;

                default:
                case Rarity.E: return 1;
            }
        }

        public static bool HasImage(this Card card) => card.GetImage() != null;

        public static double GetCardPower(this Card card)
        {
            var cardPower = card.GetHealthWithPenalty() * 0.018;
            cardPower += card.GetAttackWithBonus() * 0.019;
            cardPower += card.GetDefenceWithBonus() * 2.76;

            switch (card.Dere)
            {
                case Dere.Yami:
                case Dere.Raito:
                    cardPower += 5;
                    break;

                case Dere.Yato:
                    cardPower += 10;
                    break;

                case Dere.Tsundere:
                    cardPower -= 5;
                    break;

                default:
                    break;
            }

            if (cardPower < 1)
                cardPower = 1;

            return cardPower;
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

        public static string GetDescSmall(this Card card)
        {
            return $"**[{card.Id}]** *({card.Character})*\n"
                + $"{card.GetString(true, true, true, false, true)}\n"
                + $"_{card.Title}_\n\n"
                + $"{card.GetAffectionString()}\n"
                + $"{card.ExpCnt.ToString("F")} exp\n\n"
                + $"{card.Tags ?? "---"}\n"
                + $"{card.GetStatusIcons()}";
        }

        public static string GetDesc(this Card card)
        {
            return $"{card.GetNameWithUrl()} **{card.Rarity}**\n"
                + $"*{card.Title ?? "????"}*\n\n"
                + $"*{card.GetCardParams(true, false, true)}*\n\n"
                + $"**Relacja:** {card.GetAffectionString()}\n"
                + $"**Doświadczenie:** {card.ExpCnt.ToString("F")}\n"
                + $"**Dostępne ulepszenia:** {card.UpgradesCnt}\n\n"
                + $"**W klatce:** {card.InCage.GetYesNo()}\n"
                + $"**Aktywna:** {card.Active.GetYesNo()}\n"
                + $"**Możliwość wymiany:** {card.IsTradable.GetYesNo()}\n\n"
                + $"**Arena:** **W**: {card?.ArenaStats?.Wins ?? 0} **L**: {card?.ArenaStats?.Loses ?? 0} **D**: {card?.ArenaStats?.Draws ?? 0}\n\n"
                + $"**WID:** {card.Id} *({card.Character})*\n"
                + $"**Restarty:** {card.RestartCnt}\n"
                + $"**Pochodzenie:** {card.Source.GetString()}\n"
                + $"**Tagi:** {card.Tags ?? "---"}\n\n";
        }

        public static int GetHealthWithPenalty(this Card card, bool allowZero = false)
        {
            var percent = card.Affection * 5d / 100d;
            var newHealth = (int)(card.Health + (card.Health * percent));
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
        {
            var newAttack = card.Attack + (card.RestartCnt * 2);
            if (newAttack > 990) newAttack = 999;
            return newAttack;
        }

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
                case CardSource.Activity: return "Aktywność";
                case CardSource.Safari: return "Safari";
                case CardSource.Shop: return "Sklepik";
                case CardSource.GodIntervention: return "Czity";
                case CardSource.Api: return "Nieznane";
                case CardSource.Migration: return "Stara baza";
                case CardSource.PvE: return "Walki na boty";
                case CardSource.Daily: return "Karta+";
                case CardSource.Crafting: return "Tworzenie";

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
            if (card.Affection <= -5) return "Nienawiść";
            if (card.Affection <= -4) return "Zawiść";
            if (card.Affection <= -3) return "Wrogość";
            if (card.Affection <= -2) return "Złośliwość";
            if (card.Affection <= -1) return "Chłodność";
            if (card.Affection >= 50) return "Obsesyjna miłość";
            if (card.Affection >= 5) return "Miłość";
            if (card.Affection >= 4) return "Zauroczenie";
            if (card.Affection >= 3) return "Przyjaźń";
            if (card.Affection >= 2) return "Fascynacja";
            if (card.Affection >= 1) return "Zaciekawienie";
            return "Obojętność";
        }

        public static bool IsWeakTo(this Dere dere1, Dere dere2)
        {
            if (dere2 == Dere.Yato && dere1 != Dere.Yato)
                return true;

            if (dere2 == Dere.Yami && (dere1 != Dere.Yato && dere1 != Dere.Raito && dere1 != Dere.Yami))
                return true;

            switch (dere1)
            {
                case Dere.Tsundere:
                    if (dere2 != Dere.Tsundere)
                        return true;
                    return false;

                case Dere.Kamidere:
                    if (dere2 == Dere.Deredere)
                        return true;
                    return false;

                case Dere.Deredere:
                    if (dere2 == Dere.Yandere)
                        return true;
                    return false;

                case Dere.Yandere:
                    if (dere2 == Dere.Dandere)
                        return true;
                    return false;

                case Dere.Dandere:
                    if (dere2 == Dere.Kuudere)
                        return true;
                    return false;

                case Dere.Kuudere:
                    if (dere2 == Dere.Mayadere)
                        return true;
                    return false;

                case Dere.Mayadere:
                    if (dere2 == Dere.Bodere)
                        return true;
                    return false;

                case Dere.Bodere:
                    if (dere2 == Dere.Kamidere)
                        return true;
                    return false;

                case Dere.Yami:
                    if (dere2 == Dere.Raito)
                        return true;
                    return false;

                case Dere.Raito:
                    if (dere2 == Dere.Yami)
                        return true;
                    return false;

                case Dere.Yato:
                    return false;

                default:
                    return false;
            }
        }

        public static bool IsResistTo(this Dere dere1, Dere dere2)
        {
            if (dere1 == dere2 && dere2 != Dere.Yato)
                return true;

            switch (dere1)
            {
                case Dere.Tsundere:
                    return false;

                case Dere.Kamidere:
                    if (dere2 == Dere.Yandere)
                        return true;
                    return false;

                case Dere.Deredere:
                    if (dere2 == Dere.Dandere)
                        return true;
                    return false;

                case Dere.Yandere:
                    if (dere2 == Dere.Kuudere)
                        return true;
                    return false;

                case Dere.Dandere:
                    if (dere2 == Dere.Mayadere)
                        return true;
                    return false;

                case Dere.Kuudere:
                    if (dere2 == Dere.Bodere)
                        return true;
                    return false;

                case Dere.Mayadere:
                    if (dere2 == Dere.Kamidere)
                        return true;
                    return false;

                case Dere.Bodere:
                    if (dere2 == Dere.Deredere)
                        return true;
                    return false;

                case Dere.Yami:
                    return false;

                case Dere.Raito:
                    if (dere2 != Dere.Yami)
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
                case Rarity.SS: return 90;
                case Rarity.S: return 80;
                case Rarity.A: return 65;
                case Rarity.B: return 50;
                case Rarity.C: return 32;
                case Rarity.D: return 20;

                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetDefenceMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 88;
                case Rarity.SS: return 77;
                case Rarity.S: return 68;
                case Rarity.A: return 60;
                case Rarity.B: return 50;
                case Rarity.C: return 32;
                case Rarity.D: return 15;

                case Rarity.E:
                default: return 1;
            }
        }

        public static int GetHealthMin(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 100;
                case Rarity.SS: return 90;
                case Rarity.S: return 80;
                case Rarity.A: return 70;
                case Rarity.B: return 60;
                case Rarity.C: return 50;
                case Rarity.D: return 40;

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
                case Rarity.SS: return 100;
                case Rarity.S: return 96;
                case Rarity.A: return 87;
                case Rarity.B: return 84;
                case Rarity.C: return 68;
                case Rarity.D: return 50;

                case Rarity.E:
                default: return 35;
            }
        }

        public static int GetDefenceMax(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SSS: return 96;
                case Rarity.SS: return 91;
                case Rarity.S: return 79;
                case Rarity.A: return 75;
                case Rarity.B: return 70;
                case Rarity.C: return 65;
                case Rarity.D: return 53;

                case Rarity.E:
                default: return 38;
            }
        }

        public static string GetImage(this Card card) => card.CustomImage ?? card.Image;

        public static async Task Update(this Card card, IUser user, Shinden.ShindenClient client)
        {
            var response = await client.GetCharacterInfoAsync(card.Character);
            if (!response.IsSuccessStatusCode())
                throw new Exception($"Couldn't get card info!");

            if (user != null)
            {
                if (card.FirstIdOwner == 0)
                    card.FirstIdOwner = user.Id;
            }

            card.Name = response.Body.ToString();
            card.Image = response.Body.HasImage ? response.Body.PictureUrl : null;
            card.Title = response.Body?.Relations?.OrderBy(x => x.Id).FirstOrDefault()?.Title ?? "????";
        }
    }
}