#pragma warning disable 1591

using Sanakan.Services;

namespace Sanakan.Extensions
{
    public static class TopTypeExtension
    {
        public static string Name(this TopType type)
        {
            switch (type)
            {
                default:
                case TopType.Level:
                    return "doświadczenia";

                case TopType.ScCnt:
                    return "SC";

                case TopType.TcCnt:
                    return "TC";

                case TopType.Posts:
                    return "liczby wiadomości";

                case TopType.PostsMonthly:
                    return "liczby wiadomości w miesiącu";

                case TopType.PostsMonthlyCharacter:
                    return "liczby znaków w wiadomości";

                case TopType.Commands:
                    return "liczby użytych poleceń";

                case TopType.Cards:
                    return "liczby kart";

                case TopType.CardsPower:
                    return "mocy kart";
            }
        }
    }
}
