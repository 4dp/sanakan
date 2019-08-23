#pragma warning disable 1591

using System;

namespace Sanakan.Database.Models.Analytics
{
    public enum TransferSource
    {
        ByShindenId, ByDiscordId
    }

    public class TransferAnalytics
    {
        public ulong Id { get; set; }
        public long Value { get; set; }
        public DateTime Date { get; set; }
        public ulong DiscordId { get; set; }
        public ulong ShindenId { get; set; }
        public TransferSource Source { get; set; }
    }
}