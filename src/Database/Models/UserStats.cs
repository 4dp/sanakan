#pragma warning disable 1591

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

        public ulong UserId { get; set; }
        public virtual User User { get; set; }
    }
}
