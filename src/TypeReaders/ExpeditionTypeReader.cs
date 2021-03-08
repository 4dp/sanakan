#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Database.Models;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class ExpeditionTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "-":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.None));

                case "normalna":
                case "normal":
                case "n":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.NormalItemWithExp));

                case "trudna":
                case "hard":
                case "h":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.ExtremeItemWithExp));

                case "mrok":
                case "dark":
                case "d":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.DarkItemWithExp));

                case "mrok 1":
                case "dark 1":
                case "d1":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.DarkExp));

                case "mrok 2":
                case "dark 2":
                case "d2":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.DarkItems));

                case "światło":
                case "światlo":
                case "swiatło":
                case "swiatlo":
                case "light":
                case "l":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.LightItemWithExp));

                case "światło 1":
                case "światlo 1":
                case "swiatło 1":
                case "swiatlo 1":
                case "light 1":
                case "l1":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.LightExp));

                case "światło 2":
                case "światlo 2":
                case "swiatło 2":
                case "swiatlo 2":
                case "light 2":
                case "l2":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.LightItems));

                case "ue":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.UltimateEasy));
                case "um":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.UltimateMedium));
                case "uh":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.UltimateHard));
                case "uhh":
                    return Task.FromResult(TypeReaderResult.FromSuccess(CardExpedition.UltimateHardcore));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano typu wyprawy!"));
            }
        }
    }
}
