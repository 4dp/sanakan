#pragma warning disable 1591

using Sanakan.Api.Models;

namespace Sanakan.Config.Model
{
    public class RichMessageConfig
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public RichMessageType Type { get; set; }
    }
}
