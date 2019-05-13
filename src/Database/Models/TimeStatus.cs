#pragma warning disable 1591

using System;

namespace Sanakan.Database.Models
{
    public enum StatusType
    {
        Hourly, Daily, Globals, Color
    }

    public class TimeStatus
    {
        public ulong Id { get; set; }
        public StatusType Type { get; set; }
        public DateTime EndsAt { get; set; }

        public ulong UserId { get; set; }
        public virtual User User { get; set; }
    }
}
