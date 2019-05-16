#pragma warning disable 1591

using Sanakan.Services;

namespace Sanakan.Extensions
{
    public static class FColorExtension
    {
        public static int Price(this FColor color, SCurrency currency)
        {
            if (color == FColor.CleanColor)
                return 0;

            if (currency == SCurrency.Sc)
                return 30000;

            switch (color)
            {
                case FColor.DefinitelyNotWhite:
                    return 400;

                default:
                    return 800;
            }
        }

        public static bool IsOption(this FColor color)
        {
            switch (color)
            {
                case FColor.None:
                case FColor.CleanColor:
                    return true;

                default:
                    return false;
            }
        }
    }
}
