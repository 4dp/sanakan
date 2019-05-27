#pragma warning disable 1591

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sanakan.Database.Models.Configuration
{
    public class Waifu
    {
        public ulong Id { get; set; }
        public ulong MarketChannel { get; set; }
        public ulong SpawnChannel { get; set; }
        public ulong TrashFightChannel { get; set; }
        public ulong TrashSpawnChannel { get; set; }
        public ulong TrashCommandsChannel { get; set; }

        public ulong GuildOptionsId { get; set; }
        [JsonIgnore]
        public virtual GuildOptions GuildOptions { get; set; }

        public virtual ICollection<WaifuCommandChannel> CommandChannels { get; set; }
        public virtual ICollection<WaifuFightChannel> FightChannels { get; set; }
    }
}
