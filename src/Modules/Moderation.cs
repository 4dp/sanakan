#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Moderacja"), Group("mod"), DontAutoLoad, RequireAdminRole]
    public class Moderation : SanakanModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;
        private Database.GuildConfigContext _dbConfigContext;

        public Moderation(Services.Helper helper, Database.GuildConfigContext dbConfigContext)
        {
            _helper = helper;
            _dbConfigContext = dbConfigContext;
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

                await ReplyAsync("", embed: $"Usunięto {count} ostatnich wiadomości.".ToEmbedMessage(EMType.Bot).Build());
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

                await ReplyAsync("", embed: $"Usunięto wiadomości {user.Mention}.".ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("admin")]
        [Summary("ustawia role administratora")]
        [Remarks("34125343243432")]
        public async Task SetAdminRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = _dbConfigContext.Guilds.FirstOrDefault(x => x.Id == Context.Guild.Id);
            if (config == null)
            {
                config = new Database.Models.Configuration.GuildOptions
                {
                    Id = Context.Guild.Id
                };
                await _dbConfigContext.Guilds.AddAsync(config);
            }

            if (config.AdminRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola administratora.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.AdminRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role administratora.".ToEmbedMessage(EMType.Error).Build());
        }

        [Command("tfight")]
        [Summary("ustawia śmieciowy kanał walk waifu")]
        [Remarks("")]
        public async Task SetTrashFightWaifuChannelAsync()
        {
            var config = _dbConfigContext.Guilds.FirstOrDefault(x => x.Id == Context.Guild.Id);
            if (config == null)
            {
                config = new Database.Models.Configuration.GuildOptions
                {
                    Id = Context.Guild.Id
                };
                await _dbConfigContext.Guilds.AddAsync(config);
            }

            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.TrashFightChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiona jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashFightChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Error).Build());
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
