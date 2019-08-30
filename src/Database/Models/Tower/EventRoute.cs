#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public enum RouteType
    {
        Nothing,
        ConnectedEvent,
        RandomConnectedRoute,
        TowerItem,
        WaifuItem,
        Character
    }

    public class EventRoute
    {
        public ulong Id { get; set; }
        public string Text { get; set; }
        public string Result { get; set; }
        public RouteType Type { get; set; }

        public ulong EventId { get; set; }
        [JsonIgnore]
        public virtual Event Event { get; set; }
    }
}
