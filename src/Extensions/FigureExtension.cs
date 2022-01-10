#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class FigureExtension
    {
        public static string ToName(this Quality q)
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
                case Quality.Lambda:  return "λ";
                case Quality.Sigma:   return "Σ";
                case Quality.Omega:   return "Ω";

                default:
                case Quality.Broken:
                    return "";
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

        public static int ConstructionPointsToInstall(this Figure figure, Item part)
        {
            return (80 * (int) part.Quality) + (20 * (int) figure.SkeletonQuality);
        }

        public static Quality GetQualityOfFocusedPart(this Figure figure)
        {
            switch (figure.FocusedPart)
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
    }
}
