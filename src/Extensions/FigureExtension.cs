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
                case Quality.Alpha:  return "α";
                case Quality.Beta:   return "β";
                case Quality.Gamma:  return "γ";
                case Quality.Delta:  return "Δ";
                case Quality.Zeta:   return "ζ";
                case Quality.Lambda: return "λ";
                case Quality.Sigma:  return "Σ";
                case Quality.Omega:  return "Ω";


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
    }
}
