#pragma warning disable 1591

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class Floor
    {
        public ulong Id { get; set; }
        public DateTime BeatDate { get; set; }
        public DateTime CreateDate { get; set; }
        public ulong UserIdFirstToBeat { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }

        public ulong BossId { get; set; }

        [JsonIgnore]
        public virtual Enemy Boss { get; set; }
    }
}
