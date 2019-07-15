#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public class UserStats
    {
        public ulong Id { get; set; }
        public long ScLost { get; set; }
        public long IncomeInSc { get; set; }
        public long SlotMachineGames { get; set; }
        public long Tail { get; set; }
        public long Head { get; set; }
        public long Hit { get; set; }
        public long Misd { get; set; }
        public long RightAnswers { get; set; }
        public long TotalAnswers { get; set; }
        public long TurnamentsWon { get; set; }

        public long UpgaredCards { get; set; }
        public long SacraficeCards { get; set; }
        public long DestroyedCards { get; set; }
        public long UnleashedCards { get; set; }
        public long ReleasedCards { get; set; }
        public long WastedTcOnCookies { get; set; }
        public long WastedTcOnCards { get; set; }

        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
