#pragma warning disable 1591

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public enum RoomType
    {
        Empty,
        Campfire,
        Fight,
        Event,
        BossBattle
    }

    public class Room
    {
        public ulong Id { get; set; }
        public int Count { get; set; }
        public bool IsHidden { get; set; }

        public virtual ICollection<Room> ConnectedRooms { get; set; }

        public ulong FloorId { get; set; }
        public ulong ItemToOpenId { get; set; }
        public ulong ConnectedRoomId { get; set; }

        [JsonIgnore]
        public virtual Floor Floor { get; set; }
        [JsonIgnore]
        public virtual Item ItemToOpen { get; set; }
        [JsonIgnore]
        public virtual Room ConnectedRoom { get; set; }
    }
}
