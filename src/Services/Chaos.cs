#pragma warning disable 1591

using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;

namespace Sanakan.Services
{
    public class Chaos
    {
        private DiscordSocketClient _client;
        private IConfig _config;

        public Chaos(DiscordSocketClient client, IConfig config)
        {
            _client = client;
            _config = config;

#if !DEBUG
            _client.MessageReceived += HandleMessageAsync;
#endif
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            var user = msg.Author as SocketGuildUser;
            if (user == null) return;

            if (_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
                return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (gConfig == null) return;

                if (!gConfig.ChaosMode) return;
            }

            if (Fun.TakeATry(3))
            {
                var user2 = Fun.GetOneRandomFrom(user.Guild.Users.Where(x => !x.IsBot && x.Id != user.Id));

                var user1Nickname = user.Nickname ?? user.Username;
                var user2Nickname = user2.Nickname ?? user2.Username;

                await user.ModifyAsync(x => x.Nickname = user2Nickname);
                await user.ModifyAsync(x => x.Nickname = user1Nickname);
            }
        }
    }
}
