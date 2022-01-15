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
            string upgCnt = (withUpgrades && !card.FromFigure) ? $"_(U:{card.UpgradesCnt})_" : "";

            return $"{idStr} {name} **{card.GetCardRealRarity()}** {card.GetCardParams(showBaseHp, allowZero)} {upgCnt}";
        }

        public static string GetShortString(this Card card, bool nameAsUrl = false)
        {
            string name = nameAsUrl ? card.GetNameWithUrl() : card.Name;
            return $"**[{card.Id}]** {name} **{card.GetCardRealRarity()}**";
        }

        public static string GetCardRealRarity(this Card card)
        {
            if (card.FromFigure)
                return card.Quality.ToName();

            return card.Rarity.ToString();
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

        public static double GetMaxExpToChest(this Card card, ExpContainerLevel lvl)
        {
            double exp = 0;

            switch (card.Rarity)
            {
                case Rarity.SSS:
                    exp = 16d;
                    break;

                case Rarity.SS:
                    exp = 8d;
                    break;

                case Rarity.S:
                    exp = 4.8;
                    break;

                case Rarity.A:
                case Rarity.B:
                    exp = 3.5;
                    break;

                case Rarity.C:
                    exp = 2.5;
                    break;

                default:
                case Rarity.D:
                case Rarity.E:
                    exp = 1.5;
                    break;
            }

            switch (lvl)
            {
                case ExpContainerLevel.Level4:
                    exp *= 5d;
                    break;
                case ExpContainerLevel.Level3:
                    exp *= 2d;
                    break;
                case ExpContainerLevel.Level2:
                    exp *= 1.5;
                    break;

                default:
                case ExpContainerLevel.Level1:
                case ExpContainerLevel.Disabled:
                    break;
            }

            return exp;
        }

        public static bool HasImage(this Card card) => card.GetImage() != null;

        public static bool HasCustomBorder(this Card card) => card.CustomBorder != null;

        public static double CalculateCardPower(this Card card)
        {
            var cardPower = card.GetHealthWithPenalty() * 0.018;
            cardPower += card.GetAttackWithBonus() * 0.019;

            var normalizedDef = card.GetDefenceWithBonus();
            if (normalizedDef > 99)
            {
                normalizedDef = 99;
                if (card.FromFigure)
                {
                    cardPower += (card.GetDefenceWithBonus() - normalizedDef) * 0.019;
                }
            }

            cardPower += normalizedDef * 2.76;

            switch (card.Dere)
            {
                case Dere.Yami:
                case Dere.Raito:
                    cardPower += 20;
                    break;

                case Dere.Yato:
                    cardPower += 30;
                    break;

                case Dere.Tsundere:
                    cardPower -= 20;
                    break;

                default:
                    break;
            }

            if (cardPower < 1)
                cardPower = 1;

            card.CardPower = cardPower;

            return cardPower;
        }

        public static bool HasTag(this Card card, string tag)
        {
            return card.TagList.Any(x => x.Name.Equals(tag, StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool HasAnyTag(this Card card, IEnumerable<string> tags)
        {
            return card.TagList.Any(x => tags.Any(t => t.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase)));
        }

        public static MarketValue GetThreeStateMarketValue(this Card card)
        {
            if (card.MarketValue < 0.3) return MarketValue.Low;
            if (card.MarketValue > 2.8) return MarketValue.High;
            return MarketValue.Normal;
        }

        public static string GetStatusIcons(this Card card)
        {
            var icons = new List<string>();
            if (card.Active) icons.Add("☑️");
            if (card.Unique) icons.Add("💠");
            if (card.FromFigure) icons.Add("🎖️");
            if (!card.IsTradable) icons.Add("⛔");
            if (card.IsBroken()) icons.Add("💔");
            if (card.InCage) icons.Add("🔒");
            if (card.Expedition != CardExpedition.None) icons.Add("✈️");
            if (!string.IsNullOrEmpty(card.CustomImage)) icons.Add("🖼️");
            if (!string.IsNullOrEmpty(card.CustomBorder)) icons.Add("✂️");

            var value = card.GetThreeStateMarketValue();
            if (value == MarketValue.Low) icons.Add("♻️");
            if (value == MarketValue.High) icons.Add("💰");

            if (card.TagList.Count > 0)
            {
                if (card.TagList.Any(x => x.Name.Equals("ulubione", StringComparison.CurrentCultureIgnoreCase)))
                    icons.Add("💗");

                if (card.TagList.Any(x => x.Name.Equals("galeria", StringComparison.CurrentCultureIgnoreCase)))
                    icons.Add("📌");

                if (card.TagList.Any(x => x.Name.Equals("rezerwacja", StringComparison.CurrentCultureIgnoreCase)))
                    icons.Add("📝");

                if (card.TagList.Any(x => x.Name.Equals("wymiana", StringComparison.CurrentCultureIgnoreCase)))
                    icons.Add("🔄");
            }
            return string.Join(" ", icons);
        }

        public static string GetDescSmall(this Card card)
        {
            var tags = string.Join(" ", card.TagList.Select(x => x.Name));
            if (card.TagList.Count < 1) tags = "---";

            return $"**[{card.Id}]** *({card.Character})*\n"
                + $"{card.GetString(true, true, true, false, true)}\n"
                + $"_{card.Title}_\n\n"
                + $"{card.Dere}\n"
                + $"{card.GetAffectionString()}\n"
                + $"{card.ExpCnt.ToString("F")}/{card.ExpToUpgrade().ToString("F")} exp\n\n"
                + $"{tags}\n"
                + $"{card.GetStatusIcons()}";
        }

        public static string GetDesc(this Card card)
        {
            var tags = string.Join(" ", card.TagList.Select(x => x.Name));
            if (card.TagList.Count < 1) tags = "---";

            return $"{card.GetNameWithUrl()} **{card.GetCardRealRarity()}**\n"
                + $"*{card.Title ?? "????"}*\n\n"
                + $"*{card.GetCardParams(true, false, true)}*\n\n"
                + $"**Relacja:** {card.GetAffectionString()}\n"
                + $"**Doświadczenie:** {card.ExpCnt.ToString("F")}/{card.ExpToUpgrade().ToString("F")}\n"
                + $"**Dostępne ulepszenia:** {card.UpgradesCnt}\n\n"
                + $"**W klatce:** {card.InCage.GetYesNo()}\n"
                + $"**Aktywna:** {card.Active.GetYesNo()}\n"
                + $"**Możliwość wymiany:** {card.IsTradable.GetYesNo()}\n\n"
                + $"**WID:** {card.Id} *({card.Character})*\n"
                + $"**Restarty:** {card.RestartCnt}\n"
                + $"**Pochodzenie:** {card.Source.GetString()}\n"
                + $"**Tagi:** {tags}\n"
                + $"{card.GetStatusIcons()}\n\n";
        }

        public static int GetHealthWithPenalty(this Card card, bool allowZero = false)
        {
            var maxHealth = 999;
            if (card.FromFigure)
                maxHealth = 99999;

            var percent = card.Affection * 5d / 100d;
            var newHealth = (int)(card.Health + (card.Health * percent));
            if (card.FromFigure)
            {
                newHealth += card.HealthBonus;
            }

            if (newHealth > maxHealth)
                newHealth = maxHealth;

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

        public static int GetCardStarType(this Card card)
        {
            var max = card.MaxStarType();
            var maxRestartsPerType = card.GetMaxStarsPerType() * card.GetRestartCntPerStar();
            var type = (card.RestartCnt - 1) / maxRestartsPerType;
            if (type > 0)
            {
                var ths = card.RestartCnt - (maxRestartsPerType + ((type - 1) * maxRestartsPerType));
                if (ths < card.GetRestartCntPerStar()) --type;
            }

            if (type > max) type = max;
            return type;
        }

        public static int GetMaxCardsRestartsOnStarType(this Card card)
        {
            return  card.GetMaxStarsPerType() * card.GetRestartCntPerStar() * card.GetCardStarType();
        }

        public static int GetCardStarCount(this Card card)
        {
            var max = card.GetMaxStarsPerType();
            var starCnt = (card.RestartCnt - card.GetMaxCardsRestartsOnStarType()) / card.GetRestartCntPerStar();
            if (starCnt > max) starCnt = max;
            return starCnt;
        }

        public static int GetTotalCardStarCount(this Card card)
        {
            var max = card.GetMaxStarsPerType() * card.MaxStarType();
            var stars = card.RestartCnt / card.GetRestartCntPerStar();
            if (stars > max) stars = max;
            return stars;
        }

        public static int MaxStarType(this Card _) => 9;

        public static int GetRestartCntPerStar(this Card _) => 2;

        public static int GetMaxStarsPerType(this Card _) => 5;

        public static int GetAttackWithBonus(this Card card)
        {
            var maxAttack = 999;
            if (card.FromFigure)
                maxAttack = 9999;

            var newAttack = card.Attack + (card.RestartCnt * 2) + (card.GetTotalCardStarCount() * 8);
            if (card.FromFigure)
            {
                newAttack += card.AttackBonus;
            }

            if (card.Curse == CardCurse.LoweredStats)
            {
                newAttack -= newAttack * 5 / 10;
            }

            if (newAttack > maxAttack)
                newAttack = maxAttack;

            return newAttack;
        }

        public static int GetDefenceWithBonus(this Card card)
        {
            var maxDefence = 99;
            if (card.FromFigure)
                maxDefence = 9999;

            var newDefence = card.Defence + card.RestartCnt;
            if (card.FromFigure)
            {
                newDefence += card.DefenceBonus;
            }

            if (card.Curse == CardCurse.LoweredStats)
            {
                newDefence -= newDefence * 5 / 10;
            }

            if (newDefence > maxDefence)
                newDefence = maxDefence;

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
                case CardSource.Api: return "Strona";
                case CardSource.Migration: return "Stara baza";
                case CardSource.PvE: return "Walki na boty";
                case CardSource.Daily: return "Karta+";
                case CardSource.Crafting: return "Tworzenie";
                case CardSource.PvpShop: return "Koszary";
                case CardSource.Figure: return "Figurka";
                case CardSource.Expedition: return "Wyprawa";
                case CardSource.ActivityShop: return "Kiosk";

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

        public static bool ValidExpedition(this Card card, CardExpedition expedition, double karma)
        {
            if (card.Expedition != CardExpedition.None)
                return false;

            if (card.Curse == CardCurse.ExpeditionBlockade)
                return false;

            if (card.InCage || !card.CanFightOnPvEGMwK())
                return false;

            if (card.CalculateMaxTimeOnExpeditionInMinutes(karma, expedition) < 1)
                return false;

            switch (expedition)
            {
                case CardExpedition.ExtremeItemWithExp:
                    return !card.FromFigure && !card.HasTag("ulubione");

                case CardExpedition.NormalItemWithExp:
                    return !card.FromFigure;

                case CardExpedition.UltimateEasy:
                case CardExpedition.UltimateHard:
                case CardExpedition.UltimateMedium:
                    return card.Rarity == Rarity.SSS;

                case CardExpedition.UltimateHardcore:
                    return card.Rarity == Rarity.SSS && !card.HasTag("ulubione");

                case CardExpedition.LightExp:
                case CardExpedition.LightItems:
                    return (karma > 1000) && !card.FromFigure;
                case CardExpedition.LightItemWithExp:
                    return (karma > 400) && !card.FromFigure;

                case CardExpedition.DarkExp:
                case CardExpedition.DarkItems:
                    return (karma < -1000) && !card.FromFigure;
                case CardExpedition.DarkItemWithExp:
                    return (karma < -400) && !card.FromFigure;

                default:
                case CardExpedition.None:
                    return false;
            }
        }

        public static string GetAffectionString(this Card card)
        {
            if (card.Affection <= -400) return "Pogarda (γ)";
            if (card.Affection <= -200) return "Pogarda (β)";
            if (card.Affection <= -100) return "Pogarda (α)";
            if (card.Affection <= -50) return "Pogarda";
            if (card.Affection <= -5) return "Nienawiść";
            if (card.Affection <= -4) return "Zawiść";
            if (card.Affection <= -3) return "Wrogość";
            if (card.Affection <= -2) return "Złośliwość";
            if (card.Affection <= -1) return "Chłodność";
            if (card.Affection >= 400) return "Obsesyjna miłość (γ)";
            if (card.Affection >= 200) return "Obsesyjna miłość (β)";
            if (card.Affection >= 100) return "Obsesyjna miłość (α)";
            if (card.Affection >= 50) return "Obsesyjna miłość";
            if (card.Affection >= 5) return "Miłość";
            if (card.Affection >= 4) return "Zauroczenie";
            if (card.Affection >= 3) return "Przyjaźń";
            if (card.Affection >= 2) return "Fascynacja";
            if (card.Affection >= 1) return "Zaciekawienie";
            return "Obojętność";
        }

        public static string GetName(this CardExpedition expedition, string end = "a")
        {
            switch (expedition)
            {
                case CardExpedition.NormalItemWithExp:
                    return $"normaln{end}";

                case CardExpedition.ExtremeItemWithExp:
                    return $"niemożliw{end}";

                case CardExpedition.DarkExp:
                case CardExpedition.DarkItems:
                case CardExpedition.DarkItemWithExp:
                    return $"nikczemn{end}";

                case CardExpedition.LightExp:
                case CardExpedition.LightItems:
                case CardExpedition.LightItemWithExp:
                    return $"heroiczn{end}";

                case CardExpedition.UltimateEasy:
                    return $"niezwykł{end} (E)";
                case CardExpedition.UltimateMedium:
                    return $"niezwykł{end} (M)";
                case CardExpedition.UltimateHard:
                    return $"niezwykł{end} (H)";
                case CardExpedition.UltimateHardcore:
                    return $"niezwykł{end} (HH)";

                default:
                case CardExpedition.None:
                    return "-";
            }
        }

        public static double ExpToUpgrade(this Card card)
        {
            switch (card.Rarity)
            {
                case Rarity.SSS:
                    if (card.FromFigure)
                    {
                        return 120 * (int) card.Quality;
                    }
                    return 1000;
                case Rarity.SS:
                    return 100;

                default:
                    return 30 + (4 * (7 - (int)card.Rarity));
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

        public static void DecAffectionOnExpeditionBy(this Card card, double value)
        {
            card.Affection -= value;

            switch (card.Expedition)
            {
                case CardExpedition.UltimateEasy:
                {
                    if (card.Affection < 0)
                        card.Affection = 0;
                }
                break;

                case CardExpedition.UltimateMedium:
                {
                    if (card.Affection < -100)
                        card.Affection = -100;
                }
                break;

                default:
                break;
            }
        }

        public static void IncAttackBy(this Card card, int value)
        {
            if (card.FromFigure)
            {
                card.AttackBonus += value;
            }
            else
            {
                var max = card.Rarity.GetAttackMax();
                card.Attack += value;

                if (card.Attack > max)
                    card.Attack = max;
            }
        }

        public static void DecAttackBy(this Card card, int value)
        {
            if (card.FromFigure)
            {
                card.AttackBonus -= value;
            }
            else
            {
                var min = card.Rarity.GetAttackMin();
                card.Attack -= value;

                if (card.Attack < min)
                    card.Attack = min;
            }
        }

        public static void IncDefenceBy(this Card card, int value)
        {
            if (card.FromFigure)
            {
                card.DefenceBonus += value;
            }
            else
            {
                var max = card.Rarity.GetDefenceMax();
                card.Defence += value;

                if (card.Defence > max)
                    card.Defence = max;
            }
        }

        public static void DecDefenceBy(this Card card, int value)
        {
            if (card.FromFigure)
            {
                card.DefenceBonus -= value;
            }
            else
            {
                var min = card.Rarity.GetDefenceMin();
                card.Defence -= value;

                if (card.Defence < min)
                    card.Defence = min;
            }
        }

        public static string GetImage(this Card card) => card.CustomImage ?? card.Image;

        public static async Task Update(this Card card, IUser user, Shinden.ShindenClient client)
        {
            var response = await client.GetCharacterInfoAsync(card.Character);
            if (!response.IsSuccessStatusCode())
            {
                card.Unique = true;
                throw new Exception($"Couldn't get card info!");
            }

            if (user != null)
            {
                if (card.FirstIdOwner == 0)
                    card.FirstIdOwner = user.Id;
            }

            card.Unique = false;
            card.Name = response.Body.ToString();
            card.Image = response.Body.HasImage ? response.Body.PictureUrl : null;
            card.Title = response.Body?.Relations?.OrderBy(x => x.Id).FirstOrDefault()?.Title ?? "????";
        }

        public static StarStyle Parse(this StarStyle star, string s)
        {
            switch (s.ToLower())
            {
                case "waz":
                case "waż":
                case "wąz":
                case "wąż":
                case "snek":
                case "snake":
                    return StarStyle.Snek;

                case "pig":
                case "świnia":
                case "swinia":
                case "świnka":
                case "swinka":
                    return StarStyle.Pig;

                case "biała":
                case "biala":
                case "white":
                    return StarStyle.White;

                case "full":
                case "pełna":
                case "pelna":
                    return StarStyle.Full;

                case "empty":
                case "pusta":
                    return StarStyle.Empty;

                case "black":
                case "czarna":
                    return StarStyle.Black;

                default:
                    throw new Exception("Could't parse input!");
            }
        }

        public static double GetCostOfExpeditionPerMinute(this Card card, CardExpedition expedition = CardExpedition.None)
        {
            return GetCostOfExpeditionPerMinuteRaw(card, expedition) * card.Rarity.ValueModifierReverse() * card.Dere.ValueModifierReverse();
        }

        public static double GetCostOfExpeditionPerMinuteRaw(this Card card, CardExpedition expedition = CardExpedition.None)
        {
            expedition = (expedition == CardExpedition.None) ? card.Expedition : expedition;

            switch (expedition)
            {
                case CardExpedition.NormalItemWithExp:
                    return 0.015;

                case CardExpedition.ExtremeItemWithExp:
                    return 0.17;

                case CardExpedition.DarkExp:
                case CardExpedition.LightExp:
                case CardExpedition.LightItems:
                case CardExpedition.DarkItems:
                    return 0.12;

                case CardExpedition.DarkItemWithExp:
                case CardExpedition.LightItemWithExp:
                    return 0.07;

                case CardExpedition.UltimateEasy:
                case CardExpedition.UltimateMedium:
                    return 1;

                case CardExpedition.UltimateHard:
                    return 2;

                case CardExpedition.UltimateHardcore:
                    return 0.5;

                default:
                    return 0;
            }
        }

        public static double GetKarmaCostInExpeditionPerMinute(this Card card)
        {
            switch (card.Expedition)
            {
                case CardExpedition.NormalItemWithExp:
                    return 0.0009;

                case CardExpedition.ExtremeItemWithExp:
                    return 0.028;

                case CardExpedition.DarkItemWithExp:
                case CardExpedition.DarkItems:
                case CardExpedition.DarkExp:
                    return 0.0018;

                case CardExpedition.LightItemWithExp:
                case CardExpedition.LightExp:
                case CardExpedition.LightItems:
                    return 0.0042;

                default:
                case CardExpedition.UltimateEasy:
                case CardExpedition.UltimateMedium:
                case CardExpedition.UltimateHard:
                case CardExpedition.UltimateHardcore:
                    return 0;
            }
        }

        public static double CalculateMaxTimeOnExpeditionInMinutes(this Card card, double karma, CardExpedition expedition = CardExpedition.None)
        {
            expedition = (expedition == CardExpedition.None) ? card.Expedition : expedition;
            double perMinute = card.GetCostOfExpeditionPerMinute(expedition);
            double param = card.Affection;
            double addOFK = karma / 200;
            double affOffset = 6d;

            if (karma.IsKarmaNeutral())
            {
                affOffset += 4d;
            }

            switch (expedition)
            {
                case CardExpedition.NormalItemWithExp:
                case CardExpedition.ExtremeItemWithExp:
                    addOFK = 0;
                    break;

                case CardExpedition.LightExp:
                case CardExpedition.LightItems:
                case CardExpedition.LightItemWithExp:
                    if (addOFK > 5) addOFK = 5;
                    break;

                case CardExpedition.DarkItems:
                case CardExpedition.DarkExp:
                case CardExpedition.DarkItemWithExp:
                    addOFK = -addOFK;
                    if (addOFK > 10) addOFK = 10;
                    break;

                case CardExpedition.UltimateEasy:
                case CardExpedition.UltimateMedium:
                case CardExpedition.UltimateHard:
                case CardExpedition.UltimateHardcore:
                    param *= (int) card.Quality + 2;
                    affOffset = 0;
                    addOFK = 0;
                    break;

                default:
                    return 0;
            }

            if (!card.HasImage()) perMinute *= 2;
            param += affOffset + addOFK;
            var t = param / perMinute;
            if (t > 10080) t = 10080;

            return (t < 0.1) ? 0.1 : t;
        }

        public static double ValueModifier(this Dere dere)
        {
            switch (dere)
            {
                case Dere.Tsundere: return 0.6;

                default: return 1;
            }
        }

        public static double ValueModifier(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.SS: return 1.15;
                case Rarity.S: return 1.05;
                case Rarity.A: return 0.95;
                case Rarity.B: return 0.90;
                case Rarity.C: return 0.85;
                case Rarity.D: return 0.80;
                case Rarity.E: return 0.70;

                case Rarity.SSS:
                default: return 1.3;
            }
        }

        public static double ValueModifierReverse(this Dere dere)
        {
            return 2d - dere.ValueModifier();
        }

        public static double ValueModifierReverse(this Rarity rarity)
        {
            return 2d - rarity.ValueModifier();
        }

        public static Api.Models.CardFinalView ToView(this Card c)
            => Api.Models.CardFinalView.ConvertFromRaw(c);

        public static Api.Models.ExpeditionCard ToExpeditionView(this Card c, double karma)
            => Api.Models.ExpeditionCard.ConvertFromRaw(c, karma);

        public static List<Api.Models.ExpeditionCard> ToExpeditionView(this IEnumerable<Card> clist, double karma)
        {
            var list = new List<Api.Models.ExpeditionCard>();
            foreach (var c in clist) list.Add(c.ToExpeditionView(karma));
            return list;
        }

        public static List<Api.Models.CardFinalView> ToView(this IEnumerable<Card> clist)
        {
            var list = new List<Api.Models.CardFinalView>();
            foreach (var c in clist) list.Add(c.ToView());
            return list;
        }

        public static double GetAvgValue(this List<Card> cards)
        {
            if (cards.Count < 1) return 0.01;
            return cards.Average(x => x.MarketValue);
        }

        public static double GetAvgRarity(this List<Card> cards)
        {
            if (cards.Count < 1) return (int) Rarity.E;
            return cards.Average(x => (int) x.Rarity);
        }
    }
}