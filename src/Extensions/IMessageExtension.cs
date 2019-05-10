using Discord;

namespace Sanakan.Extensions
{
    public static class IMessageExtension
    {
        public static string GetLocalCreatedAtShortDateTime(this IMessage message)
            => message.CreatedAt.DateTime.ToLocalTime().ToShortDateTime();
    }
}
