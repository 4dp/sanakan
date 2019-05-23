#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Preconditions
{
    public class RequireWaifuFightChannel : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user == null) return PreconditionResult.FromSuccess();

            await Task.CompletedTask;

            var config = (IConfig)services.GetService(typeof(IConfig));
            using (var db = new Database.GuildConfigContext(config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(context.Guild.Id);
                if (gConfig == null) return PreconditionResult.FromSuccess();

                if (gConfig?.WaifuConfig?.FightChannels != null)
                {
                    if (gConfig.WaifuConfig.FightChannels.Any(x => x.Channel == context.Channel.Id))
                        return PreconditionResult.FromSuccess();

                    if (user.GuildPermissions.Administrator)
                        return PreconditionResult.FromSuccess();

                    var channel = await context.Guild.GetTextChannelAsync(gConfig.WaifuConfig.FightChannels.First().Channel);
                    return PreconditionResult.FromError($"To polecenie działa na kanale {channel?.Mention}");
                }
                return PreconditionResult.FromSuccess();
            }
        }
    }
}