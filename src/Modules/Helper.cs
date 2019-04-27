#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Extensions;
using System;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Ogólne")]
    public class Helper : ModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;

        public Helper(Services.Helper helper)
        {
            _helper = helper;
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("h", "help")]
        [Summary("wyświetla listę poleceń")]
        [Remarks("odcinki")]
        public async Task GiveHelpAsync([Summary("nazwa polecenia(opcjonalne)")][Remainder]string command = null)
        {
            if (command != null)
            {
                try
                {
                    await ReplyAsync(_helper.GiveHelpAboutPublicCmd(command));
                }
                catch (Exception ex)
                {
                    await ReplyAsync("", embed: ex.Message.ToEmbedMessage(EMType.Error).Build());
                }

                return;
            }

            await ReplyAsync(_helper.GivePublicHelp());
        }
    }
}
