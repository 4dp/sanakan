#pragma warning disable 1591

using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using Shinden.Logger;

namespace Sanakan.Services
{
    public class Greeting
    {
        private DiscordSocketClient _client { get; set; }
        private ILogger _logger { get; set; }
        private IConfig _config { get; set; }

        public Greeting(DiscordSocketClient client, ILogger logger, IConfig config)
        {
            _client = client;
            _logger = logger;
            _config = config;
#if !DEBUG
            _client.UserJoined += UserJoinedAsync;
            _client.UserLeft += UserLeftAsync;
#endif
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config?.WelcomeMessage == null) return;
                if (config.WelcomeMessage == "off") return;

                await SendMessageAsync(ReplaceTags(user, config.WelcomeMessage), user.Guild.GetTextChannel(config.GreetingChannel));

                if (config?.WelcomeMessagePW == null) return;
                if (config.WelcomeMessagePW == "off") return;

                try
                {
                    var pw = await user.GetOrCreateDMChannelAsync();
                    await pw.SendMessageAsync(ReplaceTags(user, config.WelcomeMessagePW));
                    await pw.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Greeting: {ex}");
                }
            }
        }

        private async Task UserLeftAsync(SocketGuildUser user)
        {
            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config?.GoodbyeMessage == null) return;
                if (config.GoodbyeMessage == "off") return;

                await SendMessageAsync(ReplaceTags(user, config.GoodbyeMessage), user.Guild.GetTextChannel(config.GreetingChannel));
            }
        }

        private async Task SendMessageAsync(string message, ITextChannel channel)
        {
            if (channel != null) await channel.SendMessageAsync(message);
        }

        private string ReplaceTags(SocketGuildUser user, string message)
            => message.Replace("^nick", user.Nickname ?? user.Username).Replace("^mention", user.Mention);
    }
}