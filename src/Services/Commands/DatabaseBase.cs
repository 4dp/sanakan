using Discord.Commands;
using Sanakan.Config;
using Sanakan.Database;

namespace Sanakan.Services.Commands
{
    public abstract class SanakanModuleBase<T> : ModuleBase<T> where T : class, ICommandContext
    {
        public IConfig Config { get; set; }
    }
}
