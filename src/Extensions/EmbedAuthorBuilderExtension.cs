using Discord;
using Discord.WebSocket;

namespace Sanakan.Extensions
{
    public static class EmbedAuthorBuilderExtension
    {
        public static EmbedAuthorBuilder WithUser(this EmbedAuthorBuilder builder, IUser user, bool includeId = false)
        {
            string id = includeId ? $" ({user.Id})" : "";

            if (user is SocketGuildUser sUser)
                builder.WithName($"{sUser.Nickname ?? sUser.Username}{id}");
            else
                builder.WithName($"{user.Username}{id}");

            return builder.WithIconUrl(user.GetAvatarUrl() ?? "https://i.imgur.com/xVIMQiB.jpg");
        }
    }
}
