#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
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
        private Services.Moderator _moderation;
        private Database.GuildConfigContext _dbConfigContext;

        public Moderation(Services.Helper helper, Services.Moderator moderation, Database.GuildConfigContext dbConfigContext)
        {
            _helper = helper;
            _moderation = moderation;
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
        [Remarks("karna")]
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

        [Command("config")]
        [Summary("wyświetla konfiguracje serwera")]
        [Remarks("")]
        public async Task ShowConfigAsync()
        {
            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
            if (config == null)
            {
                config = new Database.Models.Configuration.GuildOptions
                {
                    Id = Context.Guild.Id
                };
                await _dbConfigContext.Guilds.AddAsync(config);

                config.WaifuConfig = new Database.Models.Configuration.Waifu();

                await _dbConfigContext.SaveChangesAsync();
            }

            await ReplyAsync("", embed: _moderation.GetConfiguration(config, Context).WithTitle($"Konfiguracja {Context.Guild.Name}:").Build());
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

            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
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

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role administratora.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("logch")]
        [Summary("ustawia kanał logowania usuniętych wiadomości")]
        [Remarks("")]
        public async Task SetLogChannelAsync()
        {
            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
            if (config == null)
            {
                config = new Database.Models.Configuration.GuildOptions
                {
                    Id = Context.Guild.Id
                };
                await _dbConfigContext.Guilds.AddAsync(config);
            }

            if (config.LogChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiona jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.LogChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tfight")]
        [Summary("ustawia śmieciowy kanał walk waifu")]
        [Remarks("")]
        public async Task SetTrashFightWaifuChannelAsync()
        {
            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
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

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tcmd")]
        [Summary("ustawia śmieciowy kanał poleceń waifu")]
        [Remarks("")]
        public async Task SetTrashCmdWaifuChannelAsync()
        {
            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
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

            if (config.WaifuConfig.TrashCommandsChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiona jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashCommandsChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tsafari")]
        [Summary("ustawia śmieciowy kanał polowań waifu")]
        [Remarks("")]
        public async Task SetTrashSpawnWaifuChannelAsync()
        {
            var config = await _dbConfigContext.Guilds.FirstOrDefaultAsync(x => x.Id == Context.Guild.Id);
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

            if (config.WaifuConfig.TrashSpawnChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiona jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashSpawnChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Success).Build());
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
