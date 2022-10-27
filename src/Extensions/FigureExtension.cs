#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class FigureExtension
    {
        public static string ToName(this Quality q, string broken = "")
        {
            switch (q)
            {
                case Quality.Alpha:   return "α";
                case Quality.Beta:    return "β";
                case Quality.Gamma:   return "γ";
                case Quality.Delta:   return "Δ";
                case Quality.Epsilon: return "ε";
                case Quality.Zeta:    return "ζ";
                case Quality.Theta:   return "Θ";
                case Quality.Jota:    return "ι";
                case Quality.Lambda:  return "λ";
                case Quality.Sigma:   return "Σ";
                case Quality.Omega:   return "Ω";

                default:
                case Quality.Broken:
                    return broken;
            }
        }

        public static double ToValue(this Quality q) => 1 + (int) q * 0.1;

        public static string ToName(this FigurePart p)
        {
            switch (p)
            {
                case FigurePart.Body:     return "Tułów";
                case FigurePart.Clothes:  return "Ciuchy";
                case FigurePart.Head:     return "Głowa";
                case FigurePart.LeftArm:  return "Lewa ręka";
                case FigurePart.LeftLeg:  return "Lewa noga";
                case FigurePart.RightArm: return "Prawa ręka";
                case FigurePart.RightLeg: return "Prawa noga";
                case FigurePart.All:      return "Uniwersalna";

                default:
                case FigurePart.None:
                    return "brak";
            }
        }

        public static ulong GetCharacterId(this PreAssembledFigure pas)
        {
            switch (pas)
            {
                case PreAssembledFigure.Asuna:   return 45276;
                case PreAssembledFigure.Gintoki: return 663;
                case PreAssembledFigure.Megumin: return 72013;

                default:
                    return 0;
            }
        }

        public static string GetCharacterName(this PreAssembledFigure pas)
        {
            switch (pas)
            {
                case PreAssembledFigure.Asuna:   return "Asuna Yuuki";
                case PreAssembledFigure.Gintoki: return "Gintoki Sakata";
                case PreAssembledFigure.Megumin: return "Megumin";

                default:
                    return "";
            }
        }

        public static string GetTitleName(this PreAssembledFigure pas)
        {
            switch (pas)
            {
                case PreAssembledFigure.Asuna:   return "Sword Art Online";
                case PreAssembledFigure.Gintoki: return "Gintama";
                case PreAssembledFigure.Megumin: return "Kono Subarashii Sekai ni Shukufuku wo!";

                default:
                    return "";
            }
        }

        public static bool CanCreateUltimateCard(this Figure figure)
        {
            bool canCreate = true;
            canCreate &= figure.SkeletonQuality != Quality.Broken;
            canCreate &= figure.HeadQuality     != Quality.Broken;
            canCreate &= figure.BodyQuality     != Quality.Broken;
            canCreate &= figure.LeftArmQuality  != Quality.Broken;
            canCreate &= figure.RightArmQuality != Quality.Broken;
            canCreate &= figure.LeftLegQuality  != Quality.Broken;
            canCreate &= figure.RightLegQuality != Quality.Broken;
            canCreate &= figure.ClothesQuality  != Quality.Broken;
            canCreate &= figure.ExpCnt >= CardExtension.ExpToUpgrade(Rarity.SSS, true, figure.SkeletonQuality);
            return canCreate;
        }

        public static Quality GetAvgQuality(this Figure figure)
        {
            double tavg = ((int)figure.SkeletonQuality) * 10;
            double pavg = (int)figure.HeadQuality;
            pavg += (int)figure.BodyQuality;
            pavg += (int)figure.LeftArmQuality;
            pavg += (int)figure.RightArmQuality;
            pavg += (int)figure.LeftLegQuality;
            pavg += (int)figure.RightLegQuality;
            pavg += (int)figure.ClothesQuality;
            tavg += pavg / 7;

            int rAvg = (int)Math.Floor(tavg / 10);
            var eQ = Quality.Broken;

            foreach (int v in Enum.GetValues(typeof(Quality)))
            {
                if (v > rAvg) break;
                eQ = (Quality)v;
            }

            return eQ;
        }

        public static Card ToCard(this Figure figure)
        {
            var quality = figure.GetAvgQuality();
            var card = new Card
            {
                ArenaStats = new CardArenaStats(),
                FirstIdOwner = figure.GameDeckId,
                Expedition = CardExpedition.None,
                LastIdOwner = figure.GameDeckId,
                RestartCnt = figure.RestartCnt,
                ExpeditionDate = DateTime.Now,
                PAS = PreAssembledFigure.None,
                TagList = new List<CardTag>(),
                Character = figure.Character,
                CreationDate = DateTime.Now,
                StarStyle = StarStyle.Full,
                Source = CardSource.Figure,
                RarityOnStart = Rarity.SSS,
                QualityOnStart = quality,
                Defence = figure.Defence,
                Health = figure.Health,
                Curse = CardCurse.None,
                Attack = figure.Attack,
                Title = figure.Title,
                FigureId = figure.Id,
                Rarity = Rarity.SSS,
                CustomBorder = null,
                CustomImage = null,
                Name = figure.Name,
                Dere = figure.Dere,
                IsTradable = false,
                Quality = quality,
                FromFigure = true,
                WhoWantsCount = 0,
                DefenceBonus = 0,
                HealthBonus = 0,
                AttackBonus = 0,
                UpgradesCnt = 0,
                MarketValue = 1,
                EnhanceCnt = 0,
                Unique = false,
                InCage = false,
                Active = false,
                Affection = 0,
                Image = null,
                ExpCnt = 0,
            };

            _ = card.CalculateCardPower();

            return card;
        }

        public static int ConstructionPointsToInstall(this Figure figure, Item part)
        {
            return (80 * (int) part.Quality) + (20 * (int) figure.SkeletonQuality);
        }

        public static Quality GetQualityOfFocusedPart(this Figure figure)
            => figure.GetQualityOfPart(figure.FocusedPart);

        public static Quality GetQualityOfPart(this Figure figure, FigurePart part)
        {
            switch (part)
            {
                case FigurePart.Body:
                    return figure.BodyQuality;
                case FigurePart.Clothes:
                    return figure.ClothesQuality;
                case FigurePart.Head:
                    return figure.HeadQuality;
                case FigurePart.LeftArm:
                    return figure.LeftArmQuality;
                case FigurePart.LeftLeg:
                    return figure.LeftLegQuality;
                case FigurePart.RightArm:
                    return figure.RightArmQuality;
                case FigurePart.RightLeg:
                    return figure.RightLegQuality;

                default:
                    return Quality.Broken;
            }
        }

        public static bool CanAddPart(this Figure fig, Item part)
        {
            return part.Quality >= fig.SkeletonQuality && fig.GetQualityOfFocusedPart() == Quality.Broken;
        }

        public static bool HasEnoughPointsToAddPart(this Figure fig, Item part)
        {
            return fig.PartExp >= fig.ConstructionPointsToInstall(part);
        }

        public static bool AddPart(this Figure figure, Item part)
        {
            if (!figure.CanAddPart(part) || !figure.HasEnoughPointsToAddPart(part))
                return false;

            var partType = part.Type.GetPartType();
            if (partType != figure.FocusedPart && partType != FigurePart.All)
                return false;

            switch (figure.FocusedPart)
            {
                case FigurePart.Body:
                    figure.BodyQuality = part.Quality;
                    break;
                case FigurePart.Clothes:
                    figure.ClothesQuality = part.Quality;
                    break;
                case FigurePart.Head:
                    figure.HeadQuality = part.Quality;
                    break;
                case FigurePart.LeftArm:
                    figure.LeftArmQuality = part.Quality;
                    break;
                case FigurePart.LeftLeg:
                    figure.LeftLegQuality = part.Quality;
                    break;
                case FigurePart.RightArm:
                    figure.RightArmQuality = part.Quality;
                    break;
                case FigurePart.RightLeg:
                    figure.RightLegQuality = part.Quality;
                    break;

                default:
                    return false;
            }

            figure.PartExp = 0;
            return true;
        }

        public static string IsActive(this Figure fig)
        {
            return fig.IsFocus ? "**A**" : "";
        }

        public static string GetFiguresList(this GameDeck deck)
        {
            if (deck.Figures.Count < 1) return "Nie posiadasz figurek.";

            return string.Join("\n", deck.Figures.Select(x => $"**[{x.Id}]** *{x.SkeletonQuality.ToName()}* [{x.Name}]({Shinden.API.Url.GetCharacterURL(x.Character)}) {x.IsActive()}"));
        }

        public static string GetDesc(this Figure fig)
        {
            var name =  $"[{fig.Name}]({Shinden.API.Url.GetCharacterURL(fig.Character)})";

            return $"**[{fig.Id}] Figurka {fig.SkeletonQuality.ToName()}**\n{name}\n*{fig.ExpCnt.ToString("F")} / {CardExtension.ExpToUpgrade(Rarity.SSS, true, fig.SkeletonQuality)} exp*\n\n"
                + $"**Aktywna część:**\n {fig.FocusedPart.ToName()} *{fig.PartExp.ToString("F")} pk*\n\n"
                + $"**Części:**\n*Głowa*: {fig.HeadQuality.ToName("brak")}\n*Tułów*: {fig.BodyQuality.ToName("brak")}\n"
                + $"*Prawa ręka*: {fig.RightArmQuality.ToName("brak")}\n*Lewa ręka*: {fig.LeftArmQuality.ToName("brak")}\n"
                + $"*Prawa noga*: {fig.RightLegQuality.ToName("brak")}\n*Lewa noga*: {fig.LeftLegQuality.ToName("brak")}\n"
                + $"*Ciuchy*: {fig.ClothesQuality.ToName("brak")}";
        }
    }
}
