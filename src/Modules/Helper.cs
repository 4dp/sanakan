#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services.Session;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Ogólne")]
    public class Helper : ModuleBase<SocketCommandContext>
    {
        private SessionManager _sessions;

        public Helper(SessionManager sessions)
        {
            _sessions = sessions;
        }

        [Command("test", RunMode = RunMode.Async)]
        public async Task MakeTestAsync()
        {
            var msg = await ReplyAsync("test in async");
            await Task.Delay(2000);
            await msg.ModifyAsync(x => x.Content = "test in async - done");

            var session = new Session(Context.User)
            {
                Id = "Inna sesja",
                Event = ExecuteOn.Message,
                OnExecute = async (context, curr) =>
                {
                    if (context.Message.Content == "dziala?")
                    {
                        await msg.ModifyAsync(x => x.Content = "lol, nie");
                        return true;
                    }
                    return false;
                },
                OnDispose = async () =>
                {
                    await Task.Delay(5000);
                    await msg.DeleteAsync();
                }
            };

            if (_sessions.SessionExist(session))
            {
                await msg.ModifyAsync(x => x.Content = "juz jestem");
                return;
            }
            await _sessions.TryAddSession(session);
        }

        [Command("test2")]
        public async Task MakeTest2Async()
        {
            var msg = await ReplyAsync("test2 in async");
            await Task.Delay(2000);
            await msg.ModifyAsync(x => x.Content = "test2 in async - done");

            var session = new Session(Context.User)
            {
                Id = "Inna sesja2",
                Event = ExecuteOn.Message,
                OnExecute = async (context, curr) =>
                {
                    if (context.Message.Content == "dziala?")
                    {
                        await msg.ModifyAsync(x => x.Content = "lol, nie");
                        return true;
                    }
                    return false;
                },
                OnDispose = async () =>
                {
                    await Task.Delay(5000);
                    await msg.DeleteAsync();
                }
            };

            if (_sessions.SessionExist(session))
            {
                await msg.ModifyAsync(x => x.Content = "juz jestem");
                return;
            }
            await _sessions.TryAddSession(session);
        }
    }
}
