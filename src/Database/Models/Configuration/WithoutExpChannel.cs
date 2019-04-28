#pragma warning disable 1591

namespace Sanakan.Database.Models.Configuration
{
    public class WithoutExpChannel
    {
        public ulong Id { get; set; }
        public ulong Channel { get; set; }

        public ulong GuildOptionsId { get; set; }
        public virtual GuildOptions GuildOptions { get; set; }
    }
}
