using Discord.Commands;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Ogólne")]
    public class Helper : ModuleBase<SocketCommandContext>
    {
        [Command("test", RunMode = RunMode.Async)]
        public async Task MakeTestAsync()
        {
            var msg = await ReplyAsync("test in async");
            await Task.Delay(2000);
            await msg.ModifyAsync(x => x.Content = "test in async - done");
        }

        [Command("test2")]
        public async Task MakeTest2Async()
        {
            var msg = await ReplyAsync("test2 in async");
            await Task.Delay(2000);
            await msg.ModifyAsync(x => x.Content = "test2 in async - done");
        }
    }
}
