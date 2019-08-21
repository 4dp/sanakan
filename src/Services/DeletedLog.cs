#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
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
            _client.MessageUpdated += HandleUpdatedMsgAsync;
        }

        private async Task HandleUpdatedMsgAsync(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            if (!oldMessage.HasValue) return;

            if (newMessage.Author.IsBot || newMessage.Author.IsWebhook) return;

            if (oldMessage.Value.Content.Equals(newMessage.Content, StringComparison.CurrentCultureIgnoreCase)) return;

            if (newMessage.Channel is SocketGuildChannel gChannel)
            {
                if (_config.Get().BlacklistedGuilds.Any(x => x == gChannel.Guild.Id))
                    return;

                _ = Task.Run(async () =>
                {
                    await LogMessageAsync(gChannel, oldMessage.Value, newMessage);
                });
            }

            await Task.CompletedTask;
        }

        private async Task HandleDeletedMsgAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (!message.HasValue) return;

            if (message.Value.Author.IsBot || message.Value.Author.IsWebhook) return;

            if (message.Value.Content.Length < 4 && message.Value.Attachments.Count < 1) return;

            if (message.Value.Channel is SocketGuildChannel gChannel)
            {
                if (_config.Get().BlacklistedGuilds.Any(x => x == gChannel.Guild.Id))
                    return;

                _ = Task.Run(async () =>
                {
                    await LogMessageAsync(gChannel, message.Value);
                });
            }

            await Task.CompletedTask;
        }

        private async Task LogMessageAsync(SocketGuildChannel channel, IMessage oldMessage, IMessage newMessage = null)
        {
            if (oldMessage.Content.IsEmotikunEmote() && newMessage == null) return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(channel.Guild.Id);
                if (config == null) return;

                var ch = channel.Guild.GetTextChannel(config.LogChannel);
                if (ch == null) return;

                var jump = (newMessage == null) ? "" : $"{newMessage.GetJumpUrl()}";
                await ch.SendMessageAsync(jump, embed: BuildMessage(oldMessage, newMessage));
            }
        }

        private Embed BuildMessage(IMessage oldMessage, IMessage newMessage)
        {
            string content = (newMessage == null) ? oldMessage.Content
                : $"**Stara:**\n{oldMessage.Content}\n\n**Nowa:**\n{newMessage.Content}";

            return new EmbedBuilder
            {
                Color = (newMessage == null) ? EMType.Warning.Color() : EMType.Info.Color(),
                Author = new EmbedAuthorBuilder().WithUser(oldMessage.Author, true),
                Fields = GetFields(oldMessage, newMessage == null),
                Description = content.TrimToLength(1800),
            }.Build();
        }

        private List<EmbedFieldBuilder> GetFields(IMessage message, bool deleted)
        {
            var fields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = deleted ? "Napisano:" : "Edytowano:",
                    Value = message.GetLocalCreatedAtShortDateTime()
                },
                new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Kanał:",
                    Value = message.Channel.Name
                }
            };

            if (deleted)
            {
                fields.Add(new EmbedFieldBuilder
                {
                    IsInline = true,
                    Name = "Załączniki:",
                    Value = message.Attachments?.Count
                });
            }

            return fields;
        }
    }
}
