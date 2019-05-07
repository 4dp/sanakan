using System;
using System.Collections.Generic;

namespace Sanakan.Database.Models.Management
{
    public enum PenaltyType
    {
        Mute, Ban
    }

    public class PenaltyInfo
    {
        public ulong Id { get; set; }
        public ulong User { get; set; }
        public ulong Guild { get; set; }
        public string Reason { get; set; }
        public PenaltyType Type { get; set; }
        public DateTime StartDate { get; set; }
        public long DurationInHours { get; set; }

        public virtual ICollection<OwnedRole> Roles { get; set; }
    }
}
