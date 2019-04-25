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

        public CommandMatch Match { get; private set; }
        public ParseResult Result { get; private set; }
        public ICommandContext Context { get; private set; }

        public async Task<IResult> ExecuteAsync(IServiceProvider provider)
            => await Match.ExecuteAsync(Context, Result, provider).ConfigureAwait(false);
    }
}
