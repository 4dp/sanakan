﻿#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Sanakan.Services.Session
{
    public class SessionContext
    {
        public SessionContext(SocketCommandContext context)
        {
            ReactionRemoved = null;
            ReactionAdded = null;

            Channel = context.Channel;
            Message = context.Message;
            Client = context.Client;
            User = context.User;
        }

        public SessionContext(IMessageChannel channel, SocketUser user, IUserMessage message,
            DiscordSocketClient client, SocketReaction reaction, bool reactionAdded)
        {
            ReactionAdded = reactionAdded ? reaction : null;
            ReactionRemoved = reactionAdded ? null : reaction;

            User = user;
            Client = client;
            Channel = channel;
            Message = message;
        }

        public SocketReaction ReactionRemoved { get; private set; }
        public SocketReaction ReactionAdded { get; private set; }

        public IMessageChannel Channel { get; private set; }
        public DiscordSocketClient Client { get; private set; }
        public IUserMessage Message { get; private set; }
        public SocketUser User { get; private set; }
    }
}
