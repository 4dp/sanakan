#pragma warning disable 1591

using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class BoolTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "1":
                case "tak":
                case "true":
                case "prawda":
                    return Task.FromResult(TypeReaderResult.FromSuccess(true));

                case "0":
                case "nie":
                case "false":
                case "fałsz":
                case "falsz":
                    return Task.FromResult(TypeReaderResult.FromSuccess(false));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano wartości!"));
            }
        }
    }
}