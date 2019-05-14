#pragma warning disable 1591

using Shinden.Models;

namespace Sanakan.Extensions
{
    public class MoreSeriesStatus
    {
        public IDedicatedTime Time { get; set; }
        public IMeanScore Score { get; set; }
        public ulong? Count { get; set; }
    }
}
