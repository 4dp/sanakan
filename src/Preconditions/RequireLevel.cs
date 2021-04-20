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
    public class RequireLevel : PreconditionAttribute
    {
        private readonly long _level;

        public RequireLevel(long level) => _level = level;

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user == null) return PreconditionResult.FromError($"To polecenie działa tylko z poziomu serwera.");

            if (user.GuildPermissions.Administrator)
                return PreconditionResult.FromSuccess();

            var config = (IConfig)services.GetService(typeof(IConfig));
            using (var db = new Database.GuildConfigContext(config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(context.Guild.Id);
                if (gConfig != null)
                {
                    var role = context.Guild.GetRole(gConfig.AdminRole);
                    if (role != null)
                    {
                        if (user.Roles.Any(x => x.Id == role.Id))
                            return PreconditionResult.FromSuccess();
                    }
                }
            }

            using (var db = new Database.UserContext(config))
            {
                var botUser = await db.GetBaseUserAndDontTrackAsync(user.Id);
                if (botUser != null)
                {
                    if (botUser.Level >= _level)
                        return PreconditionResult.FromSuccess();
                }
            }

            return PreconditionResult.FromError($"|IMAGE|https://i.imgur.com/YEuawi2.gif");
        }
    }
}