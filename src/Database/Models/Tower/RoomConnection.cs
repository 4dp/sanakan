#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public class RoomConnection
    {
        public ulong Id { get; set; }
        public ulong MainRoomId { get; set; }
        public ulong ConnectedRoomId { get; set; }

        [JsonIgnore]
        public virtual Room MainRoom { get; set; }
        [JsonIgnore]
        public virtual Room ConnectedRoom { get; set; }
    }
}
