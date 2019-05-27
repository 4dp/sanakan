#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Configuration
{
    public class Raport
    {
        public ulong Id { get; set; }
        public ulong User { get; set; }
        public ulong Message { get; set; }

        public ulong GuildOptionsId { get; set; }
        [JsonIgnore]
        public virtual GuildOptions GuildOptions { get; set; }
    }
}
