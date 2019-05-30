#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services.Executor;
using System;
using System.Threading.Tasks;

namespace Sanakan.Services.Commands
{
    public class Command : IExecutable
    {
        public Command(CommandMatch match, ParseResult result, ICommandContext context)
        {
            Match = match;
            Result = result;
            Context = context;
        }

        public string GetName() => $"cmd-{Match.Command.Name}";

        public CommandMatch Match { get; private set; }
        public ParseResult Result { get; private set; }
        public ICommandContext Context { get; private set; }

        public async Task<bool> ExecuteAsync(IServiceProvider provider)
        {
            var result = await Match.ExecuteAsync(Context, Result, provider).ConfigureAwait(false);
            if (result.IsSuccess) return true;

            throw new Exception(result.ErrorReason);
        }
    }
}
