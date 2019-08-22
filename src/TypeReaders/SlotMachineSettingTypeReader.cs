#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services;
using System;
using System.Threading.Tasks;

namespace Sanakan.TypeReaders
{
    public class SlotMachineSettingTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "info":
                case "informacje":
                    return Task.FromResult(TypeReaderResult.FromSuccess(SlotMachineSetting.Info));

                case "beat":
                case "stawka":
                    return Task.FromResult(TypeReaderResult.FromSuccess(SlotMachineSetting.Beat));

                case "mnożnik":
                case "mnoznik":
                case "multiplier":
                    return Task.FromResult(TypeReaderResult.FromSuccess(SlotMachineSetting.Multiplier));

                case "rows":
                case "rzedy":
                case "rzędy":
                    return Task.FromResult(TypeReaderResult.FromSuccess(SlotMachineSetting.Rows));

                default:
                    return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Nie rozpoznano typu nastawy!"));
            }
        }
    }
}