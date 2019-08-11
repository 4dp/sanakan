#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Shinden;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Moderacja"), Group("mod"), DontAutoLoad]
    public class Moderation : SanakanModuleBase<SocketCommandContext>
    {
        private Services.Helper _helper;
        private ShindenClient _shClient;
        private Services.Profile _profile;
        private Services.Moderator _moderation;

        public Moderation(Services.Helper helper, Services.Moderator moderation, Services.Profile prof, ShindenClient sh)
        {
            _shClient = sh;
            _profile = prof;
            _helper = helper;
            _moderation = moderation;
        }

        [Command("kasuj", RunMode = RunMode.Async)]
        [Alias("prune")]
        [Summary("usuwa x ostatnich wiadomości")]
        [Remarks("12"), RequireAdminRole]
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
        [Remarks("karna"), RequireAdminRole]
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

        [Command("ban")]
        [Summary("banuje użytkownika")]
        [Remarks("karna"), RequireAdminRole]
        public async Task BanUserAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("czas trwania w godzinach")]long duration, [Summary("powód(opcjonalne)")][Remainder]string reason = "nie podano")
        {
            if (duration < 1) return;

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var notifChannel = Context.Guild.GetTextChannel(config.NotificationChannel);

                using (var mdb = new Database.ManagmentContext(Config))
                {
                    var usr = Context.User as SocketGuildUser;
                    var info = await _moderation.BanUserAysnc(user, mdb, duration, reason);
                    await _moderation.NotifyAboutPenaltyAsync(user, notifChannel, info, $"{usr.Nickname ?? usr.Username}");
                }
            }

            await ReplyAsync("", embed: $"{user.Mention} został wyciszony.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("mute")]
        [Summary("wycisza użytkownika")]
        [Remarks("karna"), RequireAdminRole]
        public async Task MuteUserAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("czas trwania w godzinach")]long duration, [Summary("powód(opcjonalne)")][Remainder]string reason = "nie podano")
        {
            if (duration < 1) return;

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var notifChannel = Context.Guild.GetTextChannel(config.NotificationChannel);
                var userRole = Context.Guild.GetRole(config.UserRole);
                var muteRole = Context.Guild.GetRole(config.MuteRole);

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

                using (var mdb = new Database.ManagmentContext(Config))
                {
                    var usr = Context.User as SocketGuildUser;
                    var info = await _moderation.MuteUserAysnc(user, muteRole, null, userRole, mdb, duration, reason);
                    await _moderation.NotifyAboutPenaltyAsync(user, notifChannel, info, $"{usr.Nickname ?? usr.Username}");
                }
            }

            await ReplyAsync("", embed: $"{user.Mention} został wyciszony.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("mute mod")]
        [Summary("wycisza moderatora")]
        [Remarks("karna"), RequireAdminRole]
        public async Task MuteModUserAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("czas trwania w godzinach")]long duration, [Summary("powód(opcjonalne)")][Remainder]string reason = "nie podano")
        {
            if (duration < 1) return;

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var notifChannel = Context.Guild.GetTextChannel(config.NotificationChannel);
                var muteModRole = Context.Guild.GetRole(config.ModMuteRole);
                var userRole = Context.Guild.GetRole(config.UserRole);
                var muteRole = Context.Guild.GetRole(config.MuteRole);

                if (muteRole == null)
                {
                    await ReplyAsync("", embed: "Rola wyciszająca nie jest ustawiona.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (muteModRole == null)
                {
                    await ReplyAsync("", embed: "Rola wyciszająca moderatora nie jest ustawiona.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (user.Roles.Contains(muteRole))
                {
                    await ReplyAsync("", embed: $"{user.Mention} już jest wyciszony.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var mdb = new Database.ManagmentContext(Config))
                {
                    var usr = Context.User as SocketGuildUser;
                    var info = await _moderation.MuteUserAysnc(user, muteRole, muteModRole, userRole, mdb, duration, reason, config.ModeratorRoles);
                    await _moderation.NotifyAboutPenaltyAsync(user, notifChannel, info, $"{usr.Nickname ?? usr.Username}");
                }
            }

            await ReplyAsync("", embed: $"{user.Mention} został wyciszony.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("unmute")]
        [Summary("zdejmuje wyciszenie z użytkownika")]
        [Remarks("karna"), RequireAdminRole]
        public async Task UnmuteUserAsync([Summary("użytkownik")]SocketGuildUser user)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var muteRole = Context.Guild.GetRole(config.MuteRole);
                var muteModRole = Context.Guild.GetRole(config.ModMuteRole);
                if (muteRole == null)
                {
                    await ReplyAsync("", embed: "Rola wyciszająca nie jest ustawiona.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (!user.Roles.Contains(muteRole))
                {
                    await ReplyAsync("", embed: $"{user.Mention} nie jest wyciszony.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var mdb = new Database.ManagmentContext(Config))
                {
                    await _moderation.UnmuteUserAsync(user, muteRole, muteModRole, mdb);
                }
            }

            await ReplyAsync("", embed: $"{user.Mention} już nie jest wyciszony.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("wyciszeni", RunMode = RunMode.Async)]
        [Alias("show muted")]
        [Summary("wyświetla wyciszonych użytkowników")]
        [Remarks(""), RequireAdminRole]
        public async Task ShowMutedUsersAsync()
        {
            using (var mdb = new Database.ManagmentContext(Config))
            {
                await ReplyAsync("", embed: await _moderation.GetMutedListAsync(mdb, Context));
            }
        }

        [Command("przywitanie")]
        [Alias("welcome")]
        [Summary("ustawia/wyświetla wiadomośc przywitania")]
        [Remarks("No elo ^mention!"), RequireAdminRole]
        public async Task SetOrShowWelcomeMessageAsync([Summary("wiadomość(opcjonalne, off - wyłączenie)")][Remainder]string messsage = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (messsage == null)
                {
                    await ReplyAsync("", embed: $"**Wiadomość przywitalna:**\n\n{config?.WelcomeMessage ?? "off"}".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (messsage.Length > 2000)
                {
                    await ReplyAsync("", embed: $"**Wiadomość jest za długa!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                config.WelcomeMessage = messsage;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{messsage}` jako wiadomość przywitalną.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("przywitaniepw")]
        [Alias("welcomepw")]
        [Summary("ustawia/wyświetla wiadomośc przywitania wysyłanego na pw")]
        [Remarks("No elo ^mention!"), RequireAdminRole]
        public async Task SetOrShowWelcomeMessagePWAsync([Summary("wiadomość(opcjonalne, off - wyłączenie)")][Remainder]string messsage = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (messsage == null)
                {
                    await ReplyAsync("", embed: $"**Wiadomość przywitalna pw:**\n\n{config?.WelcomeMessagePW ?? "off"}".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (messsage.Length > 2000)
                {
                    await ReplyAsync("", embed: $"**Wiadomość jest za długa!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                config.WelcomeMessagePW = messsage;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{messsage}` jako wiadomość przywitalną wysyłaną na pw.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("pożegnanie")]
        [Alias("pozegnanie", "goodbye")]
        [Summary("ustawia/wyświetla wiadomośc pożegnalną")]
        [Remarks("Nara ^nick?"), RequireAdminRole]
        public async Task SetOrShowGoodbyeMessageAsync([Summary("wiadomość(opcjonalne, off - wyłączenie)")][Remainder]string messsage = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (messsage == null)
                {
                    await ReplyAsync("", embed: $"**Wiadomość pożegnalna:**\n\n{config?.GoodbyeMessage ?? "off"}".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (messsage.Length > 2000)
                {
                    await ReplyAsync("", embed: $"**Wiadomość jest za długa!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                config.GoodbyeMessage = messsage;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{messsage}` jako wiadomość pożegnalną.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("role", RunMode = RunMode.Async)]
        [Summary("wyświetla role serwera")]
        [Remarks(""), RequireAdminRole]
        public async Task ShowRolesAsync()
        {
            string tmg = "";
            var msg = new List<String>();
            foreach(var item in Context.Guild.Roles)
            {
                string mg = tmg + $"{item.Mention} `{item.Mention}`\n";
                if ((mg.Length) > 2000)
                {
                    msg.Add(tmg);
                    tmg = "";
                }
                tmg += $"{item.Mention} `{item.Mention}`\n";
            }
            msg.Add(tmg);

            foreach (var content in msg)
                await ReplyAsync("", embed: content.ToEmbedMessage(EMType.Bot).Build());
        }

        [Command("config")]
        [Summary("wyświetla konfiguracje serwera")]
        [Remarks("mods"), RequireAdminRole]
        public async Task ShowConfigAsync([Summary("typ(opcjonalne)")][Remainder]Services.ConfigType type = Services.ConfigType.Global)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    config = new Database.Models.Configuration.GuildOptions
                    {
                        SafariLimit = 50,
                        Id = Context.Guild.Id
                    };
                    await db.Guilds.AddAsync(config);

                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                    await db.SaveChangesAsync();
                }

                await ReplyAsync("", embed: _moderation.GetConfiguration(config, Context, type).WithTitle($"Konfiguracja {Context.Guild.Name}:").Build());
            }
        }

        [Command("adminr")]
        [Summary("ustawia role administratora")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetAdminRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.AdminRole == role.Id)
                {
                    await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola administratora.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.AdminRole = role.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role administratora.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("userr")]
        [Summary("ustawia role użytkownika")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetUserRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.UserRole == role.Id)
                {
                    await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola użytkownika.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.UserRole = role.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role użytkownika.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("muter")]
        [Summary("ustawia role wyciszająca użytkownika")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetMuteRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.MuteRole == role.Id)
                {
                    await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola wyciszająca użytkownika.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.MuteRole = role.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role wyciszającą użytkownika.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("mutemodr")]
        [Summary("ustawia role wyciszająca moderatora")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetMuteModRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.ModMuteRole == role.Id)
                {
                    await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola wyciszająca moderatora.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.ModMuteRole = role.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role wyciszającą moderatora.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("globalr")]
        [Summary("ustawia role globalnych emotek")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetGlobalRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.GlobalEmotesRole == role.Id)
                {
                    await ReplyAsync("", embed: $"Rola {role.Mention} już jest ustawiona jako rola globalnych emotek.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.GlobalEmotesRole = role.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role globalnych emotek.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("modr")]
        [Summary("ustawia role moderatora")]
        [Remarks("34125343243432"), RequireAdminRole]
        public async Task SetModRoleAsync([Summary("id roli")]SocketRole role)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var rol = config.ModeratorRoles.FirstOrDefault(x => x.Role == role.Id);
                if (rol != null)
                {
                    config.ModeratorRoles.Remove(rol);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli moderatorów.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                rol = new Database.Models.Configuration.ModeratorRoles { Role = role.Id };
                config.ModeratorRoles.Add(rol);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role moderatora.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("addur")]
        [Summary("dodaje nową role na poziom")]
        [Remarks("34125343243432 130"), RequireAdminRole]
        public async Task SetUselessRoleAsync([Summary("id roli")]SocketRole role, [Summary("poziom")]uint level)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var rol = config.RolesPerLevel.FirstOrDefault(x => x.Role == role.Id);
                if (rol != null)
                {
                    config.RolesPerLevel.Remove(rol);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli na poziom.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                rol = new Database.Models.Configuration.LevelRole { Role = role.Id, Level = level };
                config.RolesPerLevel.Add(rol);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role na poziom `{level}`.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("selfrole")]
        [Summary("dodaje/usuwa role do automatycznego zarządzania")]
        [Remarks("34125343243432 newsy"), RequireAdminRole]
        public async Task SetSelfRoleAsync([Summary("id roli")]SocketRole role, [Summary("nazwa")][Remainder]string name = null)
        {
            if (role == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var rol = config.SelfRoles.FirstOrDefault(x => x.Role == role.Id);
                if (rol != null)
                {
                    config.SelfRoles.Remove(rol);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto {role.Mention} z listy roli automatycznego zarządzania.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                if (name == null)
                {
                    await ReplyAsync("", embed: "Nie podano nazwy roli.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                rol = new Database.Models.Configuration.SelfRole { Role = role.Id, Name = name };
                config.SelfRoles.Add(rol);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Ustawiono {role.Mention} jako role automatycznego zarządzania: `{name}`.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("myland"), RequireAdminRole]
        [Summary("dodaje nowy myland")]
        [Remarks("34125343243432 64325343243432 Kopacze")]
        public async Task AddMyLandRoleAsync([Summary("id roli")]SocketRole manager, [Summary("id roli")]SocketRole underling = null, [Summary("nazwa landu")][Remainder]string name = null)
        {
            if (manager == null)
            {
                await ReplyAsync("", embed: "Nie odnaleziono roli na serwerze.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var land = config.Lands.FirstOrDefault(x => x.Manager == manager.Id);
                if (land != null)
                {
                    await ReplyAsync("", embed: $"Usunięto {land.Name}.".ToEmbedMessage(EMType.Success).Build());

                    config.Lands.Remove(land);
                    await db.SaveChangesAsync();

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
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                await ReplyAsync("", embed: $"Dodano {land.Name} z właścicielem {manager.Mention} i podwładnym {underling.Mention}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("logch")]
        [Summary("ustawia kanał logowania usuniętych wiadomości")]
        [Remarks(""), RequireAdminRole]
        public async Task SetLogChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.LogChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.LogChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał logowania usuniętych wiadomości.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("helloch")]
        [Summary("ustawia kanał witania nowych użytkowników")]
        [Remarks(""), RequireAdminRole]
        public async Task SetGreetingChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.GreetingChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał witania nowych użytkowników.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.GreetingChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał witania nowych użytkowników.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("notifch")]
        [Summary("ustawia kanał powiadomień o karach")]
        [Remarks(""), RequireAdminRole]
        public async Task SetNotifChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.NotificationChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał powiadomień o karach.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.NotificationChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał powiadomień o karach.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("raportch")]
        [Summary("ustawia kanał raportów")]
        [Remarks(""), RequireAdminRole]
        public async Task SetRaportChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.RaportChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał raportów.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.RaportChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał raportów.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("quizch")]
        [Summary("ustawia kanał quizów")]
        [Remarks(""), RequireAdminRole]
        public async Task SetQuizChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.QuizChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał quizów.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.QuizChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał quizów.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("todoch")]
        [Summary("ustawia kanał todo")]
        [Remarks(""), RequireAdminRole]
        public async Task SetTodoChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.ToDoChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał todo.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.ToDoChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał todo.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("nsfwch")]
        [Summary("ustawia kanał nsfw")]
        [Remarks(""), RequireAdminRole]
        public async Task SetNsfwChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.NsfwChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał nsfw.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.NsfwChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał nsfw.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tfightch")]
        [Summary("ustawia śmieciowy kanał walk waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetTrashFightWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                if (config.WaifuConfig.TrashFightChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.WaifuConfig.TrashFightChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy walk waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tcmdch")]
        [Summary("ustawia śmieciowy kanał poleceń waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetTrashCmdWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                if (config.WaifuConfig.TrashCommandsChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.WaifuConfig.TrashCommandsChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tsafarich")]
        [Summary("ustawia śmieciowy kanał polowań waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetTrashSpawnWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                if (config.WaifuConfig.TrashSpawnChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.WaifuConfig.TrashSpawnChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał śmieciowy polowań waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("marketch")]
        [Summary("ustawia kanał rynku waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetMarketWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                if (config.WaifuConfig.MarketChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał rynku waifu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.WaifuConfig.MarketChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał rynku waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("spawnch")]
        [Summary("ustawia kanał safari waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetSafariWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                if (config.WaifuConfig.SpawnChannel == Context.Channel.Id)
                {
                    await ReplyAsync("", embed: $"Kanał `{Context.Channel.Name}` już jest ustawiony jako kanał safari waifu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                config.WaifuConfig.SpawnChannel = Context.Channel.Id;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał safari waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("fightch")]
        [Summary("ustawia kanał walk waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetFightWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                var chan = config.WaifuConfig.FightChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
                if (chan != null)
                {
                    config.WaifuConfig.FightChannels.Remove(chan);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów walk waifu.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                chan = new Database.Models.Configuration.WaifuFightChannel { Channel = Context.Channel.Id };
                config.WaifuConfig.FightChannels.Add(chan);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał walk waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("wcmdch")]
        [Summary("ustawia kanał poleneń waifu")]
        [Remarks(""), RequireAdminRole]
        public async Task SetCmdWaifuChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);
                if (config.WaifuConfig == null)
                    config.WaifuConfig = new Database.Models.Configuration.Waifu();

                var chan = config.WaifuConfig.CommandChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
                if (chan != null)
                {
                    config.WaifuConfig.CommandChannels.Remove(chan);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                chan = new Database.Models.Configuration.WaifuCommandChannel { Channel = Context.Channel.Id };
                config.WaifuConfig.CommandChannels.Add(chan);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał poleceń waifu.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("cmdch")]
        [Summary("ustawia kanał poleneń")]
        [Remarks(""), RequireAdminRole]
        public async Task SetCmdChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var chan = config.CommandChannels.FirstOrDefault(x => x.Channel == Context.Channel.Id);
                if (chan != null)
                {
                    config.CommandChannels.Remove(chan);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów poleceń.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                chan = new Database.Models.Configuration.CommandChannel { Channel = Context.Channel.Id };
                config.CommandChannels.Add(chan);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał poleceń.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("noexpch")]
        [Summary("ustawia kanał bez punktów doświadczenia")]
        [Remarks(""), RequireAdminRole]
        public async Task SetNonExpChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var chan = config.ChannelsWithoutExp.FirstOrDefault(x => x.Channel == Context.Channel.Id);
                if (chan != null)
                {
                    config.ChannelsWithoutExp.Remove(chan);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów bez doświadczenia.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                chan = new Database.Models.Configuration.WithoutExpChannel { Channel = Context.Channel.Id };
                config.ChannelsWithoutExp.Add(chan);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał bez doświadczenia.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("nosupch")]
        [Summary("ustawia kanał bez nadzoru")]
        [Remarks(""), RequireAdminRole]
        public async Task SetNonSupChannelAsync()
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var chan = config.ChannelsWithoutSupervision.FirstOrDefault(x => x.Channel == Context.Channel.Id);
                if (chan != null)
                {
                    config.ChannelsWithoutSupervision.Remove(chan);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });

                    await ReplyAsync("", embed: $"Usunięto `{Context.Channel.Name}` z listy kanałów bez nadzoru.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }

                chan = new Database.Models.Configuration.WithoutSupervisionChannel { Channel = Context.Channel.Id };
                config.ChannelsWithoutSupervision.Add(chan);
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"config-{Context.Guild.Id}" });
            }

            await ReplyAsync("", embed: $"Ustawiono `{Context.Channel.Name}` jako kanał bez nadzoru.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("todo")]
        [Summary("dodaje wiadomość do todo")]
        [Remarks("2342123444212"), RequireAdminOrModRole]
        public async Task MarkAsTodoAsync([Summary("id wiadomości")]ulong messageId)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                if (config == null)
                {
                    await ReplyAsync("", embed: "Serwer nie jest poprawnie skonfigurowany.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var todoChannel = Context.Guild.GetTextChannel(config.ToDoChannel);
                if (todoChannel == null)
                {
                    await ReplyAsync("", embed: "Kanał todo nie jest ustawiony.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var message = await Context.Channel.GetMessageAsync(messageId);
                if (message == null)
                {
                    await ReplyAsync("", embed: "Wiadomość nie isnieje!\nPamiętaj że polecenie musi zostać użyte w tym samym kanale gdzie znajduje się wiadomość!".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                await todoChannel.SendMessageAsync(message.GetJumpUrl(), embed: _moderation.BuildTodo(message, Context.User as SocketGuildUser));
            }
        }

        [Command("check")]
        [Summary("sprawdza użytkownika")]
        [Remarks("Karna"), RequireAdminRole]
        public async Task CheckUserAsync([Summary("użytkownik")]SocketGuildUser user)
        {
            string report = "**Globalki:** ✅\n\n";
            using (var dbg = new Database.GuildConfigContext(Config))
            {
                var guildConfig = await dbg.GetCachedGuildFullConfigAsync(user.Guild.Id);
                using (var db = new Database.UserContext(Config))
                {
                    var duser = await db.GetUserOrCreateAsync(user.Id);
                    var globalRole = user.Guild.GetRole(guildConfig.GlobalEmotesRole);
                    if (globalRole != null)
                    {
                        if (user.Roles.Contains(globalRole))
                        {
                            var sub = duser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Globals && x.Guild == user.Guild.Id);
                            if (sub == null)
                            {
                                report = $"**Globalki:** ❗\n\n";
                                await user.RemoveRoleAsync(globalRole);
                            }
                            else if (!sub.IsActive())
                            {
                                report = $"**Globalki:** ⚠\n\n";
                                await user.RemoveRoleAsync(globalRole);
                            }
                        }
                    }

                    string kolorRep = $"**Kolor:** ✅\n\n";
                    var colorRoles = (IEnumerable<uint>) Enum.GetValues(typeof(FColor));
                    if (user.Roles.Any(x => colorRoles.Any(c => c.ToString() == x.Name)))
                    {
                        var sub = duser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Color && x.Guild == user.Guild.Id);
                        if (sub == null)
                        {
                            kolorRep = $"**Kolor:** ❗\n\n";
                            await _profile.RomoveUserColorAsync(user);
                        }
                        else if (!sub.IsActive())
                        {
                            kolorRep = $"**Kolor:** ⚠\n\n";
                            await _profile.RomoveUserColorAsync(user);
                        }
                    }
                    report += kolorRep;

                    string nickRep = $"**Nick:** ✅";
                    if (guildConfig.UserRole != 0)
                    {
                        var userRole = user.Guild.GetRole(guildConfig.UserRole);
                        if (userRole != null)
                        {
                            if (user.Roles.Contains(userRole))
                            {
                                var realNick = user.Nickname ?? user.Username;
                                if (duser.Shinden != 0)
                                {
                                    var res = await _shClient.User.GetAsync(duser.Shinden);
                                    if (res.Body.Name != realNick)
                                        nickRep = $"**Nick:** ❗ {res.Body.Name}";
                                }
                                else
                                {
                                    var res = await _shClient.Search.UserAsync(realNick);
                                    if (res.IsSuccessStatusCode())
                                    {
                                        if (!res.Body.Any(x => x.Name.Equals(realNick, StringComparison.Ordinal)))
                                            nickRep = $"**Nick:** ⚠";
                                    }
                                    else nickRep = $"**Nick:** ⚠";
                                }
                            }
                        }
                    }
                    report += nickRep;
                }
            }

            await ReplyAsync("", embed: report.ToEmbedMessage(EMType.Bot).WithAuthor(new EmbedAuthorBuilder().WithUser(user)).Build());
        }

        [Command("loteria", RunMode = RunMode.Async)]
        [Summary("bot losuje osobę spośród tych co dodali reakcję")]
        [Remarks("5"), RequireAdminRole]
        public async Task GetRandomUserAsync([Summary("długość w minutach")]uint duration)
        {
            var emote = new Emoji("🎰");
            var time = DateTime.Now.AddMinutes(duration);
            var msg = await ReplyAsync("", embed: $"Loteria! zareaguj {emote} aby wziąć udział.\n\n Koniec `{time.ToShortTimeString()}:{time.Second.ToString("00")}`".ToEmbedMessage(EMType.Bot).Build());

            await msg.AddReactionAsync(emote);
            await Task.Delay(TimeSpan.FromMinutes(duration));
            await msg.RemoveReactionAsync(emote, Context.Client.CurrentUser);

            var reactions = await msg.GetReactionUsersAsync(emote, 300).FlattenAsync();
            var winner = Services.Fun.GetOneRandomFrom(reactions);
            await msg.DeleteAsync();

            await ReplyAsync("", embed: $"Zwycięzca loterii: {winner.Mention}".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("raport")]
        [Summary("rozwiązuje raport")]
        [Remarks("2342123444212 4 kara dla Ciebie"), RequireAdminRole]
        public async Task ResolveReportAsync([Summary("id raportu")]ulong rId, [Summary("długość wyciszenia w h")]long duration = -1, [Summary("powód")][Remainder]string reason = "z raportu")
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetGuildConfigOrCreateAsync(Context.Guild.Id);

                var raport = config.Raports.FirstOrDefault(x => x.Message == rId);
                if (raport == null)
                {
                    await ReplyAsync("", embed: $"Taki raport nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var notifChannel = Context.Guild.GetTextChannel(config.NotificationChannel);
                var reportChannel = Context.Guild.GetTextChannel(config.RaportChannel);
                var userRole = Context.Guild.GetRole(config.UserRole);
                var muteRole = Context.Guild.GetRole(config.MuteRole);

                if (muteRole == null)
                {
                    await ReplyAsync("", embed: "Rola wyciszająca nie jest ustawiona.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (reportChannel == null)
                {
                    await ReplyAsync("", embed: "Kanał raportów nie jest ustawiony.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                var reportMsg = await reportChannel.GetMessageAsync(raport.Message);
                if (duration == -1)
                {
                    if (reportMsg != null)
                        await reportMsg.DeleteAsync();

                    config.Raports.Remove(raport);
                    await db.SaveChangesAsync();

                    await ReplyAsync("", embed: $"Odrzucono zgłoszenie.".ToEmbedMessage(EMType.Success).Build());
                    return;
                }
                else if (duration < 1) return;

                if (reportMsg != null)
                {
                    if (reason == "z raportu")
                    {
                        try
                        {
                            reason = reportMsg?.Embeds?.FirstOrDefault()?.Fields.FirstOrDefault(x => x.Name == "Powód:").Value ?? reason;
                        }
                        catch (Exception) { }
                    }

                    await reportMsg.DeleteAsync();
                }

                config.Raports.Remove(raport);
                await db.SaveChangesAsync();

                var user = Context.Guild.GetUser(raport.User);
                if (user == null)
                {
                    await ReplyAsync("", embed: $"Użytkownika nie ma serwerze.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (user.Roles.Contains(muteRole))
                {
                    await ReplyAsync("", embed: $"{user.Mention} już jest wyciszony.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var mdb = new Database.ManagmentContext(Config))
                {
                    var usr = Context.User as SocketGuildUser;
                    var info = await _moderation.MuteUserAysnc(user, muteRole, null, userRole, mdb, duration, reason);
                    await _moderation.NotifyAboutPenaltyAsync(user, notifChannel, info, $"{usr.Nickname ?? usr.Username}");
                }

                await ReplyAsync("", embed: $"{user.Mention} został wyciszony.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("help", "h")]
        [Summary("wypisuje polecenia")]
        [Remarks("kasuj"), RequireAdminOrModRole]
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
