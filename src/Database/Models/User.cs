#pragma warning disable 1591

using System;
using System.Collections.Generic;

namespace Sanakan.Database.Models
{
    public enum ProfileType
    {
        Stats, Img, StatsWithImg, Cards
    }

    public class User
    {
        public ulong Id { get; set; }
        public ulong Shinden { get; set; }
        public bool IsBlacklisted { get; set; }
        public long AcCnt { get; set; }
        public long TcCnt { get; set; }
        public long ScCnt { get; set; }
        public long Level { get; set; }
        public long ExpCnt { get; set; }
        public ProfileType ProfileType { get; set; }
        public string BackgroundProfileUri { get; set; }
        public string StatsReplacementProfileUri { get; set; }
        public ulong MessagesCnt { get; set; }
        public ulong CommandsCnt { get; set; }
        public DateTime MeasureDate { get; set; }
        public ulong MessagesCntAtDate { get; set; }
        public ulong CharacterCntFromDate { get; set; }
        public bool ShowWaifuInProfile { get; set; }
        public long Warnings { get; set; }

        public virtual UserStats Stats { get; set; }
        public virtual GameDeck GameDeck { get; set; }
        public virtual SlotMachineConfig SMConfig { get; set; }

        public virtual ICollection<TimeStatus> TimeStatuses { get; set; }
    }
}
