#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public class CardTag
    {
        public ulong Id { get; set; }
        public string Name { get; set; }

        public ulong CardId { get; set; }
        [JsonIgnore]
        public virtual Card Card { get; set; }
    }
}
