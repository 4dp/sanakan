#pragma warning disable 1591

using System;

namespace Sanakan.Database.Models.Analytics
{
    public enum UserAnalyticsEventType
    {
        Card, Pack, Level, Characters, Cmd
    }

    public class UserAnalytics
    {
        public ulong Id { get; set; }
        public long Value { get; set; }
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public DateTime MeasureDate { get; set; }
        public UserAnalyticsEventType Type { get; set; }
    }
}