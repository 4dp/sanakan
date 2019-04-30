#pragma warning disable 1591

using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;

namespace Sanakan.Services
{
    public class DeletedLog
    {
        private DiscordSocketClient _client;
        private IConfig _config;

        public DeletedLog(DiscordSocketClient client, IConfig config)
        {
            _client = client;
            _config = config;

            _client.MessageDeleted += HandleDeletedMsgAsync;
        }

        private async Task HandleDeletedMsgAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (!message.HasValue) return;
            
            if (message.Value.Author.IsBot || message.Value.Author.IsWebhook) return;

            if (message.Value.Content.Length < 4 && message.Value.Attachments.Count < 1) return;

            if (message.Value.Channel is SocketGuildChannel gChannel)
            {
                _ = Task.Run(async () =>
                {
                    await LogMessageAsync(gChannel, message.Value);
                });
            }

            await Task.CompletedTask;
        }

        private async Task LogMessageAsync(SocketGuildChannel channel, IMessage message)
        {
            if (message.Content.IsEmotikunEmote()) return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(channel.Guild.Id);
                if (config == null) return;

                var ch = channel.Guild.GetTextChannel(config.LogChannel);
                if (ch == null) return;

                await ch.SendMessageAsync("", embed: BuildMessage(message));
            }
        }

        private Embed BuildMessage(IMessage message)
        {
            return new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder().WithUser(message.Author, true),
                Description = message.Content.TrimToLength(1800),
                Color = EMType.Warning.Color(),
                Fields = GetFields(message)
            }.Build();
        }

        private List<EmbedFieldBuilder> GetFields(IMessage message)
        {
            return new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Napisano:",
                    Value = message.GetLocalCreatedAtShortDateTime()
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Kanał:",
                    Value = message.Channel.Name
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Załączniki:",
                    Value = message.Attachments?.Count
                }
            };
        }
    }
}
