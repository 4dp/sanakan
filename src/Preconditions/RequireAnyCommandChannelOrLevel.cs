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
    public class RequireAnyCommandChannelOrLevel : PreconditionAttribute
    {
        private readonly long _level;

        public RequireAnyCommandChannelOrLevel(long level) => _level = level;

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user == null) return PreconditionResult.FromSuccess();

            if (user.GuildPermissions.Administrator)
                return PreconditionResult.FromSuccess();

            var config = (IConfig)services.GetService(typeof(IConfig));
            using (var dbu = new Database.UserContext(config))
            {
                var botUser = await dbu.GetBaseUserAndDontTrackAsync(user.Id);
                if (botUser != null)
                {
                    if (botUser.IsBlacklisted)
                        return PreconditionResult.FromError($"{user.Mention} znajdujesz się na czarnej liście bota i nie możesz uzyć tego polecenia.");

                    if (botUser.Level >= _level)
                        return PreconditionResult.FromSuccess();
                }
            }

            using (var db = new Database.GuildConfigContext(config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(context.Guild.Id);
                if (gConfig == null) return PreconditionResult.FromSuccess();

                if (gConfig.CommandChannels != null)
                {
                    if (gConfig.CommandChannels.Any(x => x.Channel == context.Channel.Id))
                        return PreconditionResult.FromSuccess();

                    if (gConfig?.WaifuConfig?.CommandChannels != null)
                    {
                        if (gConfig.WaifuConfig.CommandChannels.Any(x => x.Channel == context.Channel.Id))
                            return PreconditionResult.FromSuccess();
                    }

                    var channel = await context.Guild.GetTextChannelAsync(gConfig.CommandChannels.First().Channel);
                    return PreconditionResult.FromError($"To polecenie działa na kanale {channel?.Mention}, możesz użyć go tutaj po osiągnięciu {_level} poziomu.");
                }
                return PreconditionResult.FromSuccess();
            }
        }
    }
}