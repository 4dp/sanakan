#pragma warning disable 1591

namespace Sanakan.Services.PocketWaifu
{
    public class DuelInfo
    {
        public enum WinnerSide { Left, Right, Draw }

        public WinnerSide Side { get; set; }
        public CardInfo Winner { get; set; }
        public CardInfo Loser { get; set; }
    }
}