#pragma warning disable 1591

using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Configuration
{
    public class MyLand
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public ulong Manager { get; set; }
        public ulong Underling { get; set; }

        public ulong GuildOptionsId { get; set; }
        [JsonIgnore]
        public virtual GuildOptions GuildOptions { get; set; }
    }
}
