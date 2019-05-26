#pragma warning disable 1591

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanakan.Config;
using Sanakan.Extensions;

namespace Sanakan.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0"), Authorize]
    [Route("api/[controller]")]
    public class RichMessageController : ControllerBase
    {
        private readonly IConfig _config;
        private readonly DiscordSocketClient _client;

        public RichMessageController(DiscordSocketClient client, IConfig config)
        {
            _client = client;
            _config = config;
        }

        /// <summary>
        /// Kasuje wiadomość typu RichMessage
        /// </summary>
        /// <remarks>
        /// Do usunięcia wystarczy podać id poprzednio wysłanej wiadomości.
        /// </remarks>
        /// <param name="id">id wiadomości</param>
        /// <response code="404">Message not found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("{id}")]
        public async Task DeleteRichMessageAsync(ulong id)
        {
            var config = _config.Get();

            _ = Task.Run(async () =>
            {
                foreach (var rmc in config.RMConfig)
                {
                    var guild = _client.GetGuild(rmc.GuildId);
                    if (guild == null) continue;

                    var channel = guild.GetTextChannel(rmc.ChannelId);
                    if (channel == null) continue;

                    var msg = await channel.GetMessageAsync(id);
                    if (msg == null) continue;

                    await msg.DeleteAsync();
                    break;
                }
            });

            await "Message deleted!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Modyfikuje wiadomość typu RichMessage
        /// </summary>
        /// <remarks>
        /// Do modyfikacji wiadomości należy podać wszystkie dane od nowa.
        /// Jeśli chcemy aby link z pola Url zadziałał to należy również sprecyzować tytuł wiadomości.
        /// </remarks>
        /// <param name="id">id wiadomości</param>
        /// <param name="message">wiadomość</param>
        /// <response code="404">Message not found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut("{id}")]
        public async Task ModifyeRichMessageAsync(ulong id, [FromBody, Required]Models.RichMessage message)
        {
            var config = _config.Get();

            _ = Task.Run(async () =>
            {
                foreach (var rmc in config.RMConfig)
                {
                    var guild = _client.GetGuild(rmc.GuildId);
                    if (guild == null) continue;

                    var channel = guild.GetTextChannel(rmc.ChannelId);
                    if (channel == null) continue;

                    var msg = await channel.GetMessageAsync(id);
                    if (msg == null) continue;

                    await ((IUserMessage)msg).ModifyAsync(x => x.Embed = message.ToEmbed());
                    break;
                }
            });

            await "Message modified!".ToResponse(200).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Wysyła wiadomość typu RichMessage
        /// </summary>
        /// <remarks>
        /// Do utworzenia wiadomości wystarczy ustawić jej typ oraz, podać opis.
        /// Jeśli chcemy aby link z pola Url zadziałał to należy również sprecyzować tytuł wiadomości.
        /// </remarks>
        /// <param name="message">wiadomość</param>
        /// <param name="mention">czy oznanczyć zainteresowanych</param>
        /// <response code="500">Internal Server Error</response>
        [HttpPost]
        public async Task PostRichMessageAsync([FromBody, Required]Models.RichMessage message, [FromQuery]bool? mention)
        {
            var config = _config.Get();
            if (!mention.HasValue) mention = false;

            var msgList = new List<ulong>();
            var rmcs = config.RMConfig.Where(x => x.Type == message.MessageType);
            foreach (var rmc in rmcs)
            {
                var guild = _client.GetGuild(rmc.GuildId);
                if (guild == null) continue;

                var channel = guild.GetTextChannel(rmc.ChannelId);
                if (channel == null) continue;

                string mentionContent = "";
                if (mention.Value)
                {
                    var role = guild.GetRole(rmc.RoleId);
                    if (role != null) mentionContent = role.Mention;
                }

                var msg = await channel.SendMessageAsync(mentionContent, embed: message.ToEmbed());
                if (msg != null) msgList.Add(msg.Id);
            }

            if (msgList.Count() > 1)
            {
                await "Message sended!".ToResponseRich(msgList).ExecuteResultAsync(ControllerContext);
                return;
            }

            await "Message sended!".ToResponseRich(msgList.First()).ExecuteResultAsync(ControllerContext);
        }

        /// <summary>
        /// Zwraca przykładową wiadomość typu RichMessage
        /// </summary>
        /// <returns>wiadomość typu RichMessage</returns>
        [HttpGet]
        public Models.RichMessage GetExampleMsg()
        {
            return new Models.RichMessage().Example();
        }
    }
}