#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Database.Models;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class FigurePartTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "głowa":
                case "glowa":
                case "head":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.Head));

                case "ciało":
                case "cialo":
                case "tułów":
                case "tulów":
                case "tulow":
                case "tułow":
                case "body":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.Body));

                case "ubrania":
                case "ubranie":
                case "ciuchy":
                case "clothes":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.Clothes));

                case "lewa reka":
                case "lewa ręka":
                case "left arm":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.LeftArm));

                case "prawa reka":
                case "prawa ręka":
                case "right arm":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.RightArm));

                case "lewa noga":
                case "left leg":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.LeftLeg));

                case "prawa noga":
                case "right leg":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.RightLeg));

                case "!all":
                    return Task.FromResult(TypeReaderResult.FromSuccess(FigurePart.All));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano podanej części figurki!"));
            }
        }
    }
}
