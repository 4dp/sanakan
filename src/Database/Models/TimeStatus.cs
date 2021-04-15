#pragma warning disable 1591

using Newtonsoft.Json;
using System;

namespace Sanakan.Database.Models
{
    public enum StatusType
    {
        Hourly = 0, Daily = 1, Globals = 2, Color = 3, Market = 4, Card = 5, Packet = 6, Pvp = 7, Flood = 16,   // normal
        DPacket = 8, DHourly = 9, DMarket = 10, DUsedItems = 11, DExpeditions = 12, DPvp = 13,                  // daily quests
        WDaily = 14, WCardPlus = 15,                                                                            // weekly quests
    }

    public class TimeStatus
    {
        public ulong Id { get; set; }
        public StatusType Type { get; set; }
        public DateTime EndsAt { get; set; }

        public long IValue { get; set; }
        public bool BValue { get; set; }

        public ulong Guild { get; set; }
        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
