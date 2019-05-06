using System;

namespace Sanakan.Database.Models.Management
{
    public class LeaverInfo
    {
        public ulong Id { get; set; }
        public ulong User { get; set; }
        public ulong Guild { get; set; }
        public long LeaverCnt { get; set; }
        public long MutedInHours { get; set; }
        public DateTime MuteStart { get; set; }
    }
}
