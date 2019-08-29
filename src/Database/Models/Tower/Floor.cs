#pragma warning disable 1591

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class Floor
    {
        public ulong Id { get; set; }
        public ulong UserIdFirstToBeat { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }

        public ulong StartRoomId { get; set;}
        public ulong BossId { get; set; }

        [JsonIgnore]
        public virtual Boss Boss { get; set; }
        [JsonIgnore]
        public virtual Room StartRoom { get; set; }
    }
}
