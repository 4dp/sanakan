#pragma warning disable 1591

using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shinden.Logger;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _client;

        public DebugController(DiscordSocketClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <summary>
        /// Zabija bota
        /// </summary>
        [HttpPost("kill"), Authorize(Policy = "Site")]
        public async Task RestartBotAsync()
        {
            await _client.LogoutAsync();
            _logger.Log("Kill app from web.");
            await Task.Delay(1500);
            Environment.Exit(0);
        }

        /// <summary>
        /// Aktualizuje bota
        /// </summary>
        [HttpPost("update"), Authorize(Policy = "Site")]
        public async Task UpdateBotAsync()
        {
            await _client.LogoutAsync();
            System.IO.File.Create("./updateNow");
            _logger.Log("Update app from web.");
            await Task.Delay(1500);
            Environment.Exit(200);
        }
    }
}