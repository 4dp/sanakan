#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class ConfigTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "wfight":
                case "waifufight":
                case "waifu fight":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.WaifuFightChannels));

                case "wcmd":
                case "waifucmd":
                case "waifu cmd":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.WaifuCmdChannels));

                case "mods":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.ModeratorRoles));

                case "wexp":
                case "nonexp":
                case "nonexpchannel":
                case "nonexpchannels":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.NonExpChannels));

                case "ignch":
                case "nomsgcnt":
                case "ignoredchannel":
                case "ignoredchannels":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.IgnoredChannels));

                case "wsup":
                case "nosup":
                case "nonsupchannel":
                case "nonsupchannels":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.NonSupChannels));

                case "cmd":
                case "cmdchannel":
                case "cmdchannels":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.CmdChannels));

                case "role":
                case "roles":
                case "levelrole":
                case "levelroles":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.LevelRoles));

                case "land":
                case "lands":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.Lands));

                case "selfrole":
                case "selfroles":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.SelfRoles));

                case "rmessage":
                case "rmessages":
                case "richmessage":
                case "richmessages":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.RichMessages));

                case "all":
                case "full":
                case "global":
                    return Task.FromResult(TypeReaderResult.FromSuccess(ConfigType.Global));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano typu konfiguracji!"));
            }
        }
    }
}
