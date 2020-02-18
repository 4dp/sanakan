#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Database.Models;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class WishlistObjectTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "c":
                case "card":
                case "karta":
                    return Task.FromResult(TypeReaderResult.FromSuccess(WishlistObjectType.Card));

                case "p":
                case "postac":
                case "postać":
                case "character":
                    return Task.FromResult(TypeReaderResult.FromSuccess(WishlistObjectType.Character));

                case "t":
                case "title":
                case "tytuł":
                case "tytul":
                    return Task.FromResult(TypeReaderResult.FromSuccess(WishlistObjectType.Title));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano typu id!"));
            }
        }
    }
}
