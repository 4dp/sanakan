#pragma warning disable 1591

using Discord.WebSocket;
using Sanakan.Database.Models;
using System.Collections.Generic;

namespace Sanakan.Services.PocketWaifu
{
    public class PlayerInfo
    {
        public SocketGuildUser User { get; set; }
        public List<Card> ActiveCards { get; set; }
    }
}