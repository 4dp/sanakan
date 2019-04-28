#pragma warning disable 1591

namespace Sanakan.Database.Models.Configuration
{
    public class WaifuFightChannel
    {
        public ulong Id { get; set; }
        public ulong Channel { get; set; }

        public ulong WaifuId { get; set; }
        public virtual Waifu Waifu { get; set; }
    }
}
