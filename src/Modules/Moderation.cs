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
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Moderacja"), Group("mod"), DontAutoLoad, RequireAdminRole]
    public class Moderation : SanakanModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;
        private Services.Moderator _moderation;
        private Database.GuildConfigContext _dbConfigContext;
        private Database.ManagmentContext _dbManagmentContext;

        public Moderation(Services.Helper helper, Services.Moderator moderation, 
            Database.GuildConfigContext dbConfigContext, Database.ManagmentContext dbManagmentContext)
        {
            _helper = helper;
            _moderation = moderation;
            _dbConfigContext = dbConfigContext;
            _dbManagmentContext = dbManagmentContext;
        }

        [Command("kasuj", RunMode = RunMode.Async)]
        [Alias("prune")]
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
        [Alias("pruneu")]
        [Summary("usuwa wiadomości danego użytkownika")]
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

        [Command("mute")]
        [Summary("wycisza użytkownika")]
        [Remarks("karna")]
        public async Task MuteUserAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("czas trwania w godzinach")]long duration, [Summary("powód(opcjonalne)")]string reason = "nie podano")
        {
            SocketRole muteRole = null;
            SocketRole userRole = null;
            ITextChannel notifChannel = null;

            var config = await _dbConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
            if (config == null)
            {
                await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (config.NotificationChannel != 0)
                notifChannel = Context.Guild.GetTextChannel(config.NotificationChannel);

            if (config.UserRole != 0)
                userRole = Context.Guild.GetRole(config.UserRole);

            if (config.MuteRole != 0)
                muteRole = Context.Guild.GetRole(config.MuteRole);

            if (muteRole == null)
            {
                await ReplyAsync("", embed: "Rola wyciszająca nie jest ustawiona.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (user.Roles.Contains(muteRole))
            {
                await ReplyAsync("", embed: $"{user.Mention} już jest wyciszony.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var info = await _moderation.MuteUserAysnc(user, muteRole, userRole, _dbManagmentContext, duration, reason);
            await _moderation.NotifyAboutPenaltyAsync(user, notifChannel, info, $"");

            await ReplyAsync("", embed: $"{user.Mention} został wyciszony.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("config")]
        [Summary("wyświetla konfiguracje serwera")]
        [Remarks("mods")]
        public async Task ShowConfigAsync([Summary("typ(opcjonalne)")][Remainder]Services.ConfigType type = Services.ConfigType.Global)
        {
            var config = await _dbConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
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

            await ReplyAsync("", embed: _moderation.GetConfiguration(config, Context, type).WithTitle($"Konfiguracja {Context.Guild.Name}:").Build());
        }

        [Command("adminr")]
        [Summary("ustawia role administratora")]
        [Remarks("34125343243432")]
        public async Task SetAdminRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.AdminRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola administratora.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.AdminRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role administratora.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("userr")]
        [Summary("ustawia role użytkownika")]
        [Remarks("34125343243432")]
        public async Task SetUserRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.UserRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola użytkownika.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.UserRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role użytkownika.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("muter")]
        [Summary("ustawia role wyciszająca użytkownika")]
        [Remarks("34125343243432")]
        public async Task SetMuteRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.MuteRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola wyciszająca użytkownika.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.MuteRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role wyciszającą użytkownika.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("mutemodr")]
        [Summary("ustawia role wyciszająca moderatora")]
        [Remarks("34125343243432")]
        public async Task SetMuteModRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.ModMuteRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola wyciszająca moderatora.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.ModMuteRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role wyciszającą moderatora.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("globalr")]
        [Summary("ustawia role globalnych emotek")]
        [Remarks("34125343243432")]
        public async Task SetGlobalRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.GlobalEmotesRole == role.Id)
            {
                await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola globalnych emotek.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.GlobalEmotesRole = role.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role globalnych emotek.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("modr")]
        [Summary("ustawia role moderatora")]
        [Remarks("34125343243432")]
        public async Task SetModRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var rol = config.ModeratorRoles.FirstOrDefault(x => x.Role == role.Id);
            if (rol != null)
            {
                config.ModeratorRoles.Remove(rol);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli moderatorów.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            rol = new Database.Models.Configuration.ModeratorRoles { Role = role.Id };
            config.ModeratorRoles.Add(rol);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role moderatora.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("addur")]
        [Summary("dodaje nową role na poziom")]
        [Remarks("34125343243432 130")]
        public async Task SetUselessRoleAsync([Summary("id roli")]SocketRole role, [Summary("poziom")]uint level)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var rol = config.RolesPerLevel.FirstOrDefault(x => x.Role == role.Id);
            if (rol != null)
            {
                config.RolesPerLevel.Remove(rol);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli na poziom.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            rol = new Database.Models.Configuration.LevelRole { Role = role.Id, Level = level };
            config.RolesPerLevel.Add(rol);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role na poziom `{level}`.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("selfrole")]
        [Summary("dodaje nową role do automatycznego zarządzania")]
        [Remarks("34125343243432 newsy")]
        public async Task SetSelfRoleAsync([Summary("id roli")]SocketRole role, [Summary("nazwa")][Remainder]string name)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var rol = config.SelfRoles.FirstOrDefault(x => x.Role == role.Id);
            if (rol != null)
            {
                config.SelfRoles.Remove(rol);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli automatycznego zarządzania.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            rol = new Database.Models.Configuration.SelfRole { Role = role.Id, Name = name };
            config.SelfRoles.Add(rol);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role automatycznego zarządzania: `{name}`.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("myland")]
        [Summary("dodaje nowy myland")]
        [Remarks("34125343243432 64325343243432 Kopacze")]
        public async Task AddMyLandRoleAsync([Summary("id roli")]SocketRole manager, [Summary("id roli")]SocketRole underling = null, [Summary("nazwa landu")][Remainder]string name = null)
        {
            if (manager == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var land = config.Lands.FirstOrDefault(x => x.Manager == manager.Id);
            if (land != null)
            {
                await ReplyAsync("", embed: $"Usunięto {land.Name}.".ToEmbedMessage(EMType.Success).Build());

                config.Lands.Remove(land);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
                return;
            }

            if (underling == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                await ReplyAsync("", embed: "Nazwa nie może być pusta.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (manager.Id == underling.Id)
            {
                await ReplyAsync("", embed: "Rola właściciela nie może być taka sama jak podwładnego.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            land = new Database.Models.Configuration.MyLand
            {
                Manager = manager.Id,
                Underling = underling.Id,
                Name = name
            };

            config.Lands.Add(land);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Dodano {land.Name} z właścicielem {manager.Mention} i podwładnym {underling.Mention}.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("logch")]
        [Summary("ustawia kanał logowania usuniętych wiadomości")]
        [Remarks("")]
        public async Task SetLogChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.LogChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.LogChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("notifch")]
        [Summary("ustawia kanał powiadomień o karach")]
        [Remarks("")]
        public async Task SetNotifChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.NotificationChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał powiadomień o karach.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.NotificationChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał powiadomień o karach.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("raportch")]
        [Summary("ustawia kanał raportów")]
        [Remarks("")]
        public async Task SetRaportChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.RaportChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał raportów.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.RaportChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał raportów.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("quizch")]
        [Summary("ustawia kanał quizów")]
        [Remarks("")]
        public async Task SetQuizChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.QuizChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał quizów.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.QuizChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał quizów.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("todoch")]
        [Summary("ustawia kanał todo")]
        [Remarks("")]
        public async Task SetTodoChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.ToDoChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał todo.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.ToDoChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał todo.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("nsfwch")]
        [Summary("ustawia kanał nsfw")]
        [Remarks("")]
        public async Task SetNsfwChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.NsfwChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał nsfw.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.NsfwChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał nsfw.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tfightch")]
        [Summary("ustawia śmieciowy kanał walk waifu")]
        [Remarks("")]
        public async Task SetTrashFightWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.TrashFightChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashFightChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tcmdch")]
        [Summary("ustawia śmieciowy kanał poleceń waifu")]
        [Remarks("")]
        public async Task SetTrashCmdWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.TrashCommandsChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashCommandsChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tsafarich")]
        [Summary("ustawia śmieciowy kanał polowań waifu")]
        [Remarks("")]
        public async Task SetTrashSpawnWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.TrashSpawnChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.TrashSpawnChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("marketch")]
        [Summary("ustawia kanał rynku waifu")]
        [Remarks("")]
        public async Task SetMarketWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.MarketChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał rynku waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.MarketChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał rynku waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("spawnch")]
        [Summary("ustawia kanał safari waifu")]
        [Remarks("")]
        public async Task SetSafariWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            if (config.WaifuConfig.SpawnChannel == Context.Channel.Id)
            {
                await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał safari waifu.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            config.WaifuConfig.SpawnChannel = Context.Channel.Id;
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał safari waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("fightch")]
        [Summary("ustawia kanał walk waifu")]
        [Remarks("")]
        public async Task SetFightWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            var chan = config.WaifuConfig.FightChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (chan != null)
            {
                config.WaifuConfig.FightChannels.Remove(chan);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów walk waifu.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            chan = new Database.Models.Configuration.WaifuFightChannel { Channel = Context.Channel.Id };
            config.WaifuConfig.FightChannels.Add(chan);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał walk waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("wcmdch")]
        [Summary("ustawia kanał poleneń waifu")]
        [Remarks("")]
        public async Task SetCmdWaifuChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);
            if (config.WaifuConfig == null)
            {
                config.WaifuConfig = new Database.Models.Configuration.Waifu();
            }

            var chan = config.WaifuConfig.CommandChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (chan != null)
            {
                config.WaifuConfig.CommandChannels.Remove(chan);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            chan = new Database.Models.Configuration.WaifuCommandChannel { Channel = Context.Channel.Id };
            config.WaifuConfig.CommandChannels.Add(chan);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("cmdch")]
        [Summary("ustawia kanał poleneń")]
        [Remarks("")]
        public async Task SetCmdChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var chan = config.CommandChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (chan != null)
            {
                config.CommandChannels.Remove(chan);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów poleceń.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            chan = new Database.Models.Configuration.CommandChannel { Channel = Context.Channel.Id };
            config.CommandChannels.Add(chan);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał poleceń.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("noexpch")]
        [Summary("ustawia kanał bez punktów doświadczenia")]
        [Remarks("")]
        public async Task SetNonExpChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var chan = config.ChannelsWithoutExp.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (chan != null)
            {
                config.ChannelsWithoutExp.Remove(chan);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów bez doświadczenia.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            chan = new Database.Models.Configuration.WithoutExpChannel { Channel = Context.Channel.Id };
            config.ChannelsWithoutExp.Add(chan);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał bez doświadczenia.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("nosupch")]
        [Summary("ustawia kanał bez nadzoru")]
        [Remarks("")]
        public async Task SetNonSupChannelAsync()
        {
            var config = await _dbConfigContext.GetGuildConfigOrCreateAsync(Context.Guild.Id);

            var chan = config.ChannelsWithoutSupervision.FirstOrDefault(x => x.Channel == Context.Channel.Id);
            if (chan != null)
            {
                config.ChannelsWithoutSupervision.Remove(chan);
                await _dbConfigContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów bez nadzoru.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            chan = new Database.Models.Configuration.WithoutSupervisionChannel { Channel = Context.Channel.Id };
            config.ChannelsWithoutSupervision.Add(chan);
            await _dbConfigContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał bez nadzoru.".ToEmbedMessage(EMType.Success).Build());
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
