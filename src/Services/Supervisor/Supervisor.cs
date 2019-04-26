#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;

namespace Sanakan.Services.Supervisor
{
    public class Supervisor
    {
        private enum Action { None, Ban, Mute, Warn }

        private const int MAX_TOTAL = 13;
        private const int MAX_SPECIFIED = 8;

        private const int COMMAND_MOD = 2;
        private const int UNCONNECTED_MOD = -2;

        private Dictionary<ulong, SupervisorEntity> _mem;

        private DiscordSocketClient _client { get; set; }
        private IConfig _config { get; set; }

        public Supervisor(DiscordSocketClient client, IConfig config)
        {
            _client = client;
            _config = config;

            _mem = new Dictionary<ulong, SupervisorEntity>();

            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (!_config.Get().Supervision) return;

            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            var user = msg.Author as SocketGuildUser;
            if (user == null) return;

            var messageContent = GetMessageContent(msg);
            if (!_mem.Any(x => x.Key == user.Id))
            {
                _mem.Add(user.Id, new SupervisorEntity(messageContent));
                return;
            }

            var susspect = _mem[user.Id];
            if (!susspect.IsValid())
            {
                susspect = new SupervisorEntity(messageContent);
                return;
            }

            var thisMessage = susspect.Get(messageContent);
            if (thisMessage == null)
            {
                thisMessage = new SupervisorMessage(messageContent, 0);
                susspect.Add(thisMessage);
            }

            //TODO: check if has "user role"
            switch (MakeDecision(messageContent, susspect.Inc(), thisMessage.Inc(), true))
            {
                case Action.Warn:
                    await msg.Channel.SendMessageAsync("", 
                        embed: $"{user.Mention} zaraz przekroczysz granicę!".ToEmbedMessage(EMType.Bot).Build());
                    break;

                case Action.Mute:
                    //TODO: mute / database
                    break;

                case Action.Ban:
                    await user.Guild.AddBanAsync(user, 1, "Supervisor(ban)");
                    break;

                default:
                case Action.None:
                    break;
            }
        }

        private Action MakeDecision(string content, int total, int specified, bool hasRole)
        {
            int mSpecified = MAX_SPECIFIED;
            int mTotal = MAX_TOTAL;

            if (content.IsCommand(_config.Get().Prefix))
            {
                mTotal += COMMAND_MOD;
                mSpecified += COMMAND_MOD;
            }

            if (!hasRole)
            {
                mTotal += UNCONNECTED_MOD;
                mSpecified += UNCONNECTED_MOD;
            }
            
            int mWSpec = mSpecified - 1;
            int mWTot = mTotal - 1;

            if ((total == mWTot || specified == mWSpec) && hasRole) 
                return Action.Warn;

            if (total > mTotal || specified > mSpecified)
            {
                if (!hasRole) return Action.Ban;
                return Action.Mute;
            }

            return Action.None;
        }

        private string GetMessageContent(SocketUserMessage message)
        {
            string content = message.Content;
            if (string.IsNullOrEmpty(message.Content))
                content = message?.Attachments?.FirstOrDefault()?.Filename ?? "embed";

            return content;
        }
    }
}
