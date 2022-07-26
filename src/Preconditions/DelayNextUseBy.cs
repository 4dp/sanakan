#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Preconditions
{
    public class DelayNextUseBy : PreconditionAttribute
    {
        public enum DelayMethod
        {
            PerUser, Global
        }

        public enum ResType
        {
            Nothing, MinSec, HourMin
        }

        private readonly ResType _responseType;
        private readonly DelayMethod _method;
        private readonly TimeSpan _time;

        private static Dictionary<(string, ulong), DateTime> _entries = new Dictionary<(string, ulong), DateTime>();

        public DelayNextUseBy(double time_min, ResType resType = ResType.MinSec, DelayMethod method = DelayMethod.PerUser)
        {
            _time = TimeSpan.FromMinutes(time_min);
            _responseType = resType;
            _method = method;
        }

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

            var userId = _method == DelayMethod.PerUser ? user.Id : 1;
            var cmdKey = (command.Name, context.User.Id);
            if (_entries.ContainsKey(cmdKey))
            {
                var lastUse = _entries[cmdKey];
                if (lastUse + _time > DateTime.Now)
                {
                    switch (_responseType)
                    {
                        case ResType.Nothing:
                            return PreconditionResult.FromError($"{context.User.Mention} to polecenie możesz użyć raz na jakiś czas.");

                        case ResType.HourMin:
                            var min = (int)(lastUse + _time - DateTime.Now).TotalMinutes;
                            return PreconditionResult.FromError($"{context.User.Mention} to polecenie możesz użyć za {min / 60}h {min % 60}m.");

                        default:
                        case ResType.MinSec:
                            var sec = (int)(lastUse + _time - DateTime.Now).TotalSeconds;
                            return PreconditionResult.FromError($"{context.User.Mention} to polecenie możesz użyć za {sec / 60}m {sec % 60}s.");
                    }
                }

                _entries[cmdKey] = DateTime.Now;
            }
            else
            {
                _entries.Add(cmdKey, DateTime.Now);
            }

            return PreconditionResult.FromSuccess();
        }
    }
}