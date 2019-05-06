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
    public class RequireUserRole : PreconditionAttribute
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

                var role = context.Guild.GetRole(gConfig.UserRole);
                if (role == null) return PreconditionResult.FromSuccess();

                if (user.Roles.Any(x => x.Id == role.Id)) return PreconditionResult.FromSuccess();
                if (user.GuildPermissions.Administrator) return PreconditionResult.FromSuccess();
                
                return PreconditionResult.FromError($"Do użycia tego polecenia wymagana jest rola {role.Mention}");
            }
        }
    }
}