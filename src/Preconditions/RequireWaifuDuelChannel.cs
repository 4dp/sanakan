#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using System;
using System.Threading.Tasks;

namespace Sanakan.Preconditions
{
    public class RequireWaifuDuelChannel : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user == null) return PreconditionResult.FromError($"To polecenie działa tylko z poziomu serwera.");

            await Task.CompletedTask;

            var config = (IConfig)services.GetService(typeof(IConfig));
            using (var db = new Database.GuildConfigContext(config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(context.Guild.Id);
                if (gConfig == null) return PreconditionResult.FromSuccess();

                if (gConfig?.WaifuConfig?.DuelChannel != null)
                {
                    if (gConfig.WaifuConfig.DuelChannel == context.Channel.Id)
                        return PreconditionResult.FromSuccess();

                    if (user.GuildPermissions.Administrator)
                        return PreconditionResult.FromSuccess();

                    var channel = await context.Guild.GetTextChannelAsync(gConfig.WaifuConfig.DuelChannel);
                    return PreconditionResult.FromError($"To polecenie działa na kanale {channel?.Mention}");
                }
                return PreconditionResult.FromSuccess();
            }
        }
    }
}