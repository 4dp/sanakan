#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Moderacja"), Group("mod"), DontAutoLoad, RequireUserPermission(GuildPermission.Administrator)]
    public class Moderation : ModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;

        public Moderation(Services.Helper helper)
        {
            _helper = helper;
        }

        [Command("kasuj", RunMode = RunMode.Async)]
        [Alias("purne")]
        [Summary("usuwa x ostatnich wiadomośći")]
        [Remarks("12")]
        public async Task DeleteMesegesAsync([Summary("liczba wiadomości")]int count)
        {
            if (count < 1)
                return;

            await Context.Message.DeleteAsync();
            if (Context.Channel is ITextChannel channel)
            {
                var enumerable = await channel.GetMessagesAsync(count).FlattenAsync();
                await channel.DeleteMessagesAsync(enumerable).ConfigureAwait(false);

                await ReplyAsync("", embed: $"Usunięto {count} ostatnich wiadomości!".ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("kasuju", RunMode = RunMode.Async)]
        [Alias("purneu")]
        [Summary("usuwa wiadomośći danego użytkownika")]
        [Remarks("karna 12")]
        public async Task DeleteUserMesegesAsync([Summary("użytkownik")]SocketGuildUser user)
        {
            await Context.Message.DeleteAsync();
            if (Context.Channel is ITextChannel channel)
            {
                var enumerable = await channel.GetMessagesAsync().FlattenAsync();
                var userMessages = enumerable.Where(x => x.Author == user);
                await channel.DeleteMessagesAsync(userMessages).ConfigureAwait(false);

                await ReplyAsync("", embed: $"Usunięto wiadomości {user.Mention}!".ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("help", "h")]
        [Summary("wypisuje polecenia")]
        [Remarks("kasuj")]
        public async Task SendHelpAsync([Summary("nazwa polecenia(opcjonalne)")][Remainder]string command = null)
        {
            if (command != null)
            {
                try
                {
                    await ReplyAsync(_helper.GiveHelpAboutPrivateCmd("Moderacja", command));
                }
                catch (Exception ex)
                {
                    await ReplyAsync("", embed: ex.Message.ToEmbedMessage(EMType.Error).Build());
                }

                return;
            }

            await ReplyAsync(_helper.GivePrivateHelp("Moderacja"));
        }
    }
}
