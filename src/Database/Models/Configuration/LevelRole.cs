#pragma warning disable 1591

using System.ComponentModel.DataAnnotations;

namespace Sanakan.Database.Models.Configuration
{
    public class LevelRole
    {
        public ulong Id { get; set; }
        public ulong Role { get; set; }
        public ulong Level { get; set; }

        public ulong GuildOptionsId { get; set; }
        public virtual GuildOptions GuildOptions { get; set; }
    }
}
