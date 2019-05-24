#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Config;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Preconditions
{
    public class RequireDev : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var config = (IConfig)services.GetService(typeof(IConfig));
            if (config.Get().Dev.Any(x => x == context.User.Id))
                return PreconditionResult.FromSuccess();

            await Task.CompletedTask;

            return PreconditionResult.FromError($"|IMAGE|https://i.giphy.com/RX3vhj311HKLe.gif");
        }
    }
}