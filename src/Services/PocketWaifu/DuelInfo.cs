#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Services.PocketWaifu
{
    public class DuelInfo
    {
        public enum WinnerSide { Left, Right, Draw }

        public WinnerSide Side { get; set; }
        public Card Winner { get; set; }
        public Card Loser { get; set; }
    }
}