#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class TopTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "lvl":
                case "level":
                case "poziom":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Level));

                case "sc":
                case "funds":
                case "wallet":
                case "wallet sc":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.ScCnt));

                case "tc":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.TcCnt));

                case "posty":
                case "msg":
                case "wiadomosci":
                case "wiadomości":
                case "messages":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Posts));

                case "postym":
                case "msgm":
                case "wiadomoscim":
                case "wiadomościm":
                case "messagesm":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.PostsMonthly));

                case "postyms":
                case "msgmavg":
                case "wiadomoscims":
                case "wiadomościms":
                case "messagesmabg":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.PostsMonthlyCharacter));

                case "command":
                case "commands":
                case "polecenia":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Commands));

                case "karta":
                case "card":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Card));

                case "karty":
                case "cards":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Cards));

                case "kartym":
                case "cardsp":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.CardsPower));

                case "karma":
                case "karma+":
                case "+karma":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.Karma));

                case "karma-":
                case "-karma":
                    return Task.FromResult(TypeReaderResult.FromSuccess(TopType.KarmaNegative));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano typu topki!"));
            }
        }
    }
}
