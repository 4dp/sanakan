#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Sanakan.Config;

namespace Sanakan.Services
{
    public class FakeConsciousness
    {
        private struct ReactionMap
        {
            public string Message;
            public IEmote Emote;

            public ReactionMap(string msg, IEmote emote)
            {
                Message = msg;
                Emote = emote;
            }
        }

        private DiscordSocketClient _client;
        private List<ReactionMap> _map;
        private IConfig _config;

        private IEmote _upEmote = new Emoji("👍");
        private IEmote _sadEmote = new Emoji("😫");
        private IEmote _downEmote = new Emoji("👎");
        private IEmote _thinkEmote = new Emoji("🤔");

        public FakeConsciousness(DiscordSocketClient client, IConfig config)
        {
            _client = client;
            _config = config;

            _map = new List<ReactionMap>
            {
                // 👎
                new ReactionMap("potwierdza", _downEmote),
                new ReactionMap("zgadza", _downEmote),
                new ReactionMap("wspiera", _downEmote),
                new ReactionMap("popiera", _downEmote),
                new ReactionMap("daj mi", _downEmote),
                // 😥
                new ReactionMap("jest ze mną", _sadEmote),
                new ReactionMap("jest ze mna", _sadEmote),
                new ReactionMap("mojej stronie", _sadEmote),
                new ReactionMap("twojej stronie", _sadEmote),
                // 🤔
                new ReactionMap("zepsuty", _thinkEmote),
                new ReactionMap("popsuty", _thinkEmote),
                new ReactionMap("zepsuł", _thinkEmote),
                new ReactionMap("zepsul", _thinkEmote),
                new ReactionMap("pakiet", _thinkEmote),
                new ReactionMap("karty", _thinkEmote),
                // 👍
                new ReactionMap("ignoruje", _upEmote),
                new ReactionMap("brutusie", _upEmote),
            };

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

            var thisBot = (await msg.Channel.GetUserAsync(_client.CurrentUser.Id)) as SocketGuildUser;
            bool aboutBot = msg.MentionedUsers.Any(x => x.Id == thisBot.Id);

            var toCheck = msg.Content.ToLower();
            var name = thisBot.Nickname ?? thisBot.Username;
            aboutBot |= toCheck.Contains(name.ToLower());

            if (aboutBot)
            {
                foreach (var r in _map)
                {
                    if (toCheck.Contains(r.Message))
                    {
                        await msg.AddReactionAsync(r.Emote);
                        return;
                    }
                }
            }
        }
    }
}
