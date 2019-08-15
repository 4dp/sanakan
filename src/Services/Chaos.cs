#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using Shinden.Logger;

namespace Sanakan.Services
{
    public class Chaos
    {
        private DiscordSocketClient _client;
        private List<ulong> _changed;
        private IConfig _config;
        private ILogger _logger;
        private Timer _timer;

        public Chaos(DiscordSocketClient client, IConfig config, ILogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
            _changed = new List<ulong>();
            _timer = new Timer(_ =>
            {
                try
                {
                    _changed = new List<ulong>();
                }
                catch (Exception ex)
                {
                    _logger.Log($"in chaos: {ex}");
                    _changed.Clear();
                }
            },
            null,
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(1));

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
                var notChangedUsers = user.Guild.Users.Where(x => !x.IsBot && x.Id != user.Id && !_changed.Any(c => c == x.Id)).ToList();
                if (notChangedUsers.Count < 2) return;

                if (_changed.Any(x => x == user.Id))
                {
                    user = Fun.GetOneRandomFrom(notChangedUsers);
                    notChangedUsers.Remove(user);
                }

                var user2 = Fun.GetOneRandomFrom(notChangedUsers);

                var user1Nickname = user.Nickname ?? user.Username;
                var user2Nickname = user2.Nickname ?? user2.Username;

                await user.ModifyAsync(x => x.Nickname = user2Nickname);
                _changed.Add(user.Id);

                await user2.ModifyAsync(x => x.Nickname = user1Nickname);
                _changed.Add(user2.Id);
            }
        }
    }
}
