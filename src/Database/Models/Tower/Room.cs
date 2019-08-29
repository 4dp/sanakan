#pragma warning disable 1591

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public enum RoomType
    {
        Start,
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

        [InverseProperty("MainRoom")]
        public virtual ICollection<RoomConnection> ConnectedRooms { get; set; }
        [InverseProperty("ConnectedRoom")]
        public virtual ICollection<RoomConnection> RetConnectedRooms { get; set; }

        public ulong FloorId { get; set; }
        public ulong ItemToOpenId { get; set; }

        [JsonIgnore]
        public virtual Floor Floor { get; set; }
        [JsonIgnore]
        public virtual TowerItem ItemToOpen { get; set; }
    }
}
