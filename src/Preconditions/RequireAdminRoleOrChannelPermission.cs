#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Preconditions
{
    public class RequireAdminRoleOrChannelPermission : PreconditionAttribute
    {
        private readonly ChannelPermission _permission;

        public RequireAdminRoleOrChannelPermission(ChannelPermission permission) => _permission = permission;

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var user = context.User as SocketGuildUser;
            if (user == null) return PreconditionResult.FromError($"To polecenie działa tylko z poziomu serwera.");

            await Task.CompletedTask;

            var channel = context.Channel as IGuildChannel;
            if (channel == null) return PreconditionResult.FromError($"To polecenie działa tylko z poziomu serwera.");

            var config = (IConfig)services.GetService(typeof(IConfig));
            using (var db = new Database.GuildConfigContext(config))
            {
                var gConfig = await db.GetCachedGuildFullConfigAsync(context.Guild.Id);
                if (gConfig == null) return CheckUser(user, channel);

                var role = context.Guild.GetRole(gConfig.AdminRole);
                if (role == null) return CheckUser(user, channel);

                if (user.Roles.Any(x => x.Id == role.Id)) return PreconditionResult.FromSuccess();
                return CheckUser(user, channel);
            }
        }

        private PreconditionResult CheckUser(SocketGuildUser user, IGuildChannel channel)
        {
            if (user.GuildPermissions.Administrator) return PreconditionResult.FromSuccess();
            if (user.GetPermissions(channel).Has(_permission)) return PreconditionResult.FromSuccess();
            return PreconditionResult.FromError($"|IMAGE|https://i.giphy.com/RX3vhj311HKLe.gif");
        }
    }
}