#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Ogólne"), RequireCommandChannel]
    public class Helper : SanakanModuleBase<SocketCommandContext>
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

        [Command("ktoto", RunMode = RunMode.Async)]
        [Alias("whois")]
        [Summary("wyświetla informacje o użytkowniku")]
        [Remarks("Dzida")]
        public async Task GiveUserInfoAsync([Summary("nazwa użytkownika(opcjonalne)")]SocketUser user = null)
        {
            var usr = (user ?? Context.User) as SocketGuildUser;
            if (usr == null)
            {
                await ReplyAsync("", embed: "Polecenie działa tylko z poziomu serwera.".ToEmbedMessage(EMType.Info).Build());
                return;
            }

            await ReplyAsync("", embed: _helper.GetInfoAboutUser(usr));
        }

        [Command("ping", RunMode = RunMode.Async)]
        [Summary("sprawdza opóźnienie między botem a serwerem")]
        [Remarks("")]
        public async Task GivePingAsync()
        {
            int latency = Context.Client.Latency;

            EMType type = EMType.Error;
            if (latency < 400) type = EMType.Warning;
            if (latency < 200) type = EMType.Success;

            await ReplyAsync("", embed: $"Pong! `{latency}ms`".ToEmbedMessage(type).Build());
        }

        [Command("serwerinfo", RunMode = RunMode.Async)]
        [Alias("serverinfo", "sinfo")]
        [Summary("wyświetla informacje o serwerze")]
        [Remarks("")]
        public async Task GiveServerInfoAsync()
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("", embed: "Polecenie działa tylko z poziomu serwera.".ToEmbedMessage(EMType.Info).Build());
                return;
            }

            await ReplyAsync("", embed: _helper.GetInfoAboutServer(Context.Guild));
        }

        [Command("awatar", RunMode = RunMode.Async)]
        [Alias("avatar", "pfp")]
        [Summary("wyświetla awatar użytkownika")]
        [Remarks("Dzida")]
        public async Task ShowUserAvatarAsync([Summary("nazwa użytkownika(opcjonalne)")]SocketUser user = null)
        {
            var usr = (user ?? Context.User);
            var embed = new EmbedBuilder
            {
                ImageUrl = usr.GetAvatarUrl() ?? "https://i.imgur.com/xVIMQiB.jpg",
                Author = new EmbedAuthorBuilder().WithUser(usr),
                Color = EMType.Info.Color(),
            };

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("info", RunMode = RunMode.Async)]
        [Summary("wyświetla informacje o bocie")]
        [Remarks("")]
        public async Task GiveBotnfoAsync()
        {
            var proc = System.Diagnostics.Process.GetCurrentProcess();
            string info = $"**Sanakan ({typeof(Sanakan).Assembly.GetName().Version})**:\n"
                + $"**Czas działania**: `{(DateTime.Now - proc.StartTime).ToString(@"d'd 'hh\:mm\:ss")}`\n"
                + $"**Framework**: `{RuntimeInformation.FrameworkDescription}`\n"
                + $"**Wątki**: `{proc.Threads.Count}` / **RAM**: `{proc.WorkingSet64 / 1048576} MiB`";

            await ReplyAsync(info);
        }
    }
}