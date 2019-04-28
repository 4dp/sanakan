#pragma warning disable 1591

namespace Sanakan.Database.Models.Configuration
{
    public class ModeratorRoles
    {
        public ulong Id { get; set; }
        public ulong Role { get; set; }

        public ulong GuildOptionsId { get; set; }
        public virtual GuildOptions GuildOptions { get; set; }
    }
}
