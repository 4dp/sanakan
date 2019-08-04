#pragma warning disable 1591

using Newtonsoft.Json;
using Sanakan.Database.Models.Configuration;
using System;

namespace Sanakan.Database.Models
{
    public enum StatusType
    {
        Hourly, Daily, Globals, Color, Market, Card
    }

    public class TimeStatus
    {
        public ulong Id { get; set; }
        public StatusType Type { get; set; }
        public DateTime EndsAt { get; set; }

        public ulong Guild { get; set; }
        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
