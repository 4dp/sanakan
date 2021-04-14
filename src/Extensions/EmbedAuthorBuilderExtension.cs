#pragma warning disable 1591

using Discord;
using Discord.WebSocket;

namespace Sanakan.Extensions
{
    public static class EmbedAuthorBuilderExtension
    {
        public static EmbedBuilder WithUser(this EmbedBuilder builder, IUser user, bool includeId = false)
        {
            return builder.WithAuthor(new EmbedAuthorBuilder().WithUser(user, includeId));
        }

        public static EmbedAuthorBuilder WithUser(this EmbedAuthorBuilder builder, IUser user, bool includeId = false)
        {
            if (user == null)
                return builder.WithName("????");

            string id = includeId ? $" ({user.Id})" : "";

            if (user is SocketGuildUser sUser)
                builder.WithName($"{sUser.Nickname ?? sUser.Username}{id}");
            else
                builder.WithName($"{user.Username}{id}");

            return builder.WithIconUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());
        }
    }
}
