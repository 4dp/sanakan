#pragma warning disable 1591

using System.Collections.Generic;

namespace Sanakan.Database.Models.Tower
{
    public enum EventType
    {
        None,
        Person,
        EvilPerson
    }

    public class Event
    {
        public ulong Id { get; set; }
        public bool Start { get; set; }
        public string Text { get; set; }
        public EventType Type { get; set; }

        public virtual ICollection<EventRoute> Routes { get; set; }
    }
}
