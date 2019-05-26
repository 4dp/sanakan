#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models.Configuration;
using Sanakan.Database.Models.Management;
using Sanakan.Extensions;
using Shinden.Logger;
using Z.EntityFramework.Plus;

namespace Sanakan.Services
{
    public enum ConfigType
    {
        Global,
        SelfRoles,
        Lands,
        LevelRoles,
        CmdChannels,
        NonExpChannels,
        NonSupChannels,
        WaifuCmdChannels,
        WaifuFightChannels,
        RichMessages,
        ModeratorRoles
    }

    public class Moderator
    {
        private DiscordSocketClient _client;
        private ILogger _logger;
        private IConfig _config;
        private Timer _timer;

        public Moderator(ILogger logger, IConfig config, DiscordSocketClient client)
        {
            _logger = logger;
            _config = config;
            _client = client;

            _timer = new Timer(async _ =>
            {
                try
                {
                    using (var db = new Database.ManagmentContext(_config))
                    {
                        await CyclicCheckPenalties(db);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"in penalty: {ex}");
                }
            },
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
        }

        private async Task CyclicCheckPenalties(Database.ManagmentContext db)
        {
            foreach (var penalty in await db.GetCachedFullPenalties())
            {
                var guild = _client.GetGuild(penalty.Guild);
                if (guild == null) continue;

                var user = guild.GetUser(penalty.User);
                if (user != null)
                {
                    using (var conf = new Database.GuildConfigContext(_config))
                    {
                        var gconfig = await conf.GetCachedGuildFullConfigAsync(guild.Id);
                        var muteModRole = guild.GetRole(gconfig.ModMuteRole);
                        var muteRole = guild.GetRole(gconfig.MuteRole);

                        if ((DateTime.Now - penalty.StartDate).TotalHours < penalty.DurationInHours)
                        {
                            _ = Task.Run(async () => { await MuteUserGuildAsync(user, muteRole, penalty.Roles); });
                            continue;
                        }

                        if (penalty.Type == PenaltyType.Mute)
                        {
                            await UnmuteUserGuildAsync(user, muteRole, muteModRole, penalty.Roles);
                            await RemovePenaltyFromDb(db, penalty);
                        }
                    }
                }
                else
                {
                    if ((DateTime.Now - penalty.StartDate).TotalHours > penalty.DurationInHours)
                    {
                        if (penalty.Type == PenaltyType.Ban)
                        {
                            var ban = await guild.GetBanAsync(penalty.User);
                            if (ban != null) await guild.RemoveBanAsync(penalty.User);
                        }
                        await RemovePenaltyFromDb(db, penalty);
                    }
                }
            }
        }

        private EmbedBuilder GetFullConfiguration(GuildOptions config, SocketCommandContext context)
        {
            var modsRolesCnt = config.ModeratorRoles?.Count;
            string mods = (modsRolesCnt > 0) ? $"({modsRolesCnt}) `config mods`" : "--";

            var wExpCnt = config.ChannelsWithoutExp?.Count;
            string wExp = (wExpCnt > 0) ? $"({wExpCnt}) `config wexp`" : "--";

            var wSupCnt = config.ChannelsWithoutSupervision?.Count;
            string wSup = (wSupCnt > 0) ? $"({wSupCnt}) `config wsup`" : "--";

            var cmdChCnt = config.CommandChannels?.Count;
            string cmdCh = (cmdChCnt > 0) ? $"({cmdChCnt}) `config cmd`" : "--";

            var rolPerLvlCnt = config.RolesPerLevel?.Count;
            string roles = (rolPerLvlCnt > 0) ? $"({rolPerLvlCnt}) `config role`" : "--";

            var selfRolesCnt = config.SelfRoles?.Count;
            string selfRoles = (selfRolesCnt > 0) ? $"({selfRolesCnt}) `config selfrole`" : "--";

            var landsCnt = config.Lands?.Count;
            string lands = (landsCnt > 0) ? $"({landsCnt}) `config lands`" : "--";

            var wCmdCnt = config.WaifuConfig?.CommandChannels?.Count;
            string wcmd = (wCmdCnt > 0) ? $"({wCmdCnt}) `config wcmd`" : "--";

            var wFightCnt = config.WaifuConfig?.FightChannels?.Count;
            string wfCh = (wFightCnt > 0) ? $"({wFightCnt}) `config wfight`" : "--";

            return new EmbedBuilder
            {
                Color = EMType.Bot.Color(),
                Description = $"**Admin:** {context.Guild.GetRole(config.AdminRole)?.Mention ?? "--"}\n"
                            + $"**User:** {context.Guild.GetRole(config.UserRole)?.Mention ?? "--"}\n"
                            + $"**Mute:** {context.Guild.GetRole(config.MuteRole)?.Mention ?? "--"}\n"
                            + $"**ModMute:** {context.Guild.GetRole(config.ModMuteRole)?.Mention ?? "--"}\n"
                            + $"**Emote:** {context.Guild.GetRole(config.GlobalEmotesRole)?.Mention ?? "--"}\n\n"

                            + $"**W Market:** {context.Guild.GetTextChannel(config.WaifuConfig?.MarketChannel ?? 0)?.Mention ?? "--"}\n"
                            + $"**W Spawn:** {context.Guild.GetTextChannel(config.WaifuConfig?.SpawnChannel ?? 0)?.Mention ?? "--"}\n"
                            + $"**W Trash Fight:** {context.Guild.GetTextChannel(config.WaifuConfig?.TrashFightChannel ?? 0)?.Mention ?? "--"}\n"
                            + $"**W Trash Spawn:** {context.Guild.GetTextChannel(config.WaifuConfig?.TrashSpawnChannel ?? 0)?.Mention ?? "--"}\n"
                            + $"**W Trash Cmd:** {context.Guild.GetTextChannel(config.WaifuConfig?.TrashCommandsChannel ?? 0)?.Mention ?? "--"}\n"
                            + $"**Powiadomienia:** {context.Guild.GetTextChannel(config.NotificationChannel)?.Mention ?? "--"}\n"
                            + $"**Przywitalnia:** {context.Guild.GetTextChannel(config.GreetingChannel)?.Mention ?? "--"}\n"
                            + $"**Raport:** {context.Guild.GetTextChannel(config.RaportChannel)?.Mention ?? "--"}\n"
                            + $"**Todos:** {context.Guild.GetTextChannel(config.ToDoChannel)?.Mention ?? "--"}\n"
                            + $"**Quiz:** {context.Guild.GetTextChannel(config.QuizChannel)?.Mention ?? "--"}\n"
                            + $"**Nsfw:** {context.Guild.GetTextChannel(config.NsfwChannel)?.Mention ?? "--"}\n"
                            + $"**Log:** {context.Guild.GetTextChannel(config.LogChannel)?.Mention ?? "--"}\n\n"

                            + $"**W Cmd**: {wcmd}\n"
                            + $"**W Fight**: {wfCh}\n"
                            + $"**Role modów**: {mods}\n"
                            + $"**Bez exp**: {wExp}\n"
                            + $"**Bez nadzoru**: {wSup}\n"
                            + $"**Polecenia**: {cmdCh}\n"
                            + $"**Role na lvl**: {roles}\n"
                            + $"**AutoRole**: {selfRoles}\n"
                            + $"**Krainy**: {lands}".TrimToLength(1950)
            };
        }

        private EmbedBuilder GetSelfRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**AutoRole:**\n\n";
            if (config.SelfRoles?.Count > 0)
            {
                foreach (var role in config.SelfRoles)
                    value += $"*{role.Name}* - {context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetModRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Role moderatorów:**\n\n";
            if (config.ModeratorRoles?.Count > 0)
            {
                foreach (var role in config.ModeratorRoles)
                    value += $"{context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetLandsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Krainy:**\n\n";
            if (config.Lands?.Count > 0)
            {
                foreach (var land in config.Lands)
                    value += $"*{land.Name}*: M:{context.Guild.GetRole(land.Manager)?.Mention ?? "usunięta"} U:{context.Guild.GetRole(land.Underling)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetLevelRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Role na poziom:**\n\n";
            if (config.RolesPerLevel?.Count > 0)
            {
                foreach (var role in config.RolesPerLevel)
                    value += $"*{role.Level}*: {context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetCmdChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Kanały poleceń:**\n\n";
            if (config.CommandChannels?.Count > 0)
            {
                foreach (var channel in config.CommandChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetWaifuCmdChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Kanały poleceń waifu:**\n\n";
            if (config.WaifuConfig?.CommandChannels?.Count > 0)
            {
                foreach (var channel in config.WaifuConfig.CommandChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetWaifuFightChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Kanały walk waifu:**\n\n";
            if (config.WaifuConfig?.FightChannels?.Count > 0)
            {
                foreach (var channel in config.WaifuConfig.FightChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetNonExpChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Kanały bez exp:**\n\n";
            if (config.ChannelsWithoutExp?.Count > 0)
            {
                foreach (var channel in config.ChannelsWithoutExp)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetNonSupChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Kanały bez nadzoru:**\n\n";
            if (config.ChannelsWithoutSupervision?.Count > 0)
            {
                foreach (var channel in config.ChannelsWithoutSupervision)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithColor(EMType.Bot.Color()).WithDescription(value.TrimToLength(1950));
        }

        public EmbedBuilder GetConfiguration(GuildOptions config, SocketCommandContext context, ConfigType type)
        {
            switch (type)
            {
                case ConfigType.NonExpChannels:
                    return GetNonExpChannelsConfig(config, context);

                case ConfigType.NonSupChannels:
                    return GetNonSupChannelsConfig(config, context);

                case ConfigType.WaifuCmdChannels:
                    return GetWaifuCmdChannelsConfig(config, context);

                case ConfigType.WaifuFightChannels:
                    return GetWaifuFightChannelsConfig(config, context);

                case ConfigType.CmdChannels:
                    return GetCmdChannelsConfig(config, context);

                case ConfigType.LevelRoles:
                    return GetLevelRolesConfig(config, context);

                case ConfigType.Lands:
                    return GetLandsConfig(config, context);

                case ConfigType.ModeratorRoles:
                    return GetModRolesConfig(config, context);

                case ConfigType.SelfRoles:
                    return GetSelfRolesConfig(config, context);

                default:
                case ConfigType.Global:
                    return GetFullConfiguration(config, context);
            }
        }

        public async Task NotifyAboutPenaltyAsync(SocketGuildUser user, ITextChannel channel,
            PenaltyInfo info, string byWho = "automat")
        {
            var embed = new EmbedBuilder
            {
                Color = (info.Type == PenaltyType.Mute) ? EMType.Warning.Color() : EMType.Error.Color(),
                Footer = new EmbedFooterBuilder().WithText($"Przez: {byWho}"),
                Description = $"Powód: {info.Reason}".TrimToLength(1800),
                Author = new EmbedAuthorBuilder().WithUser(user),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "UserId:",
                        Value = $"{user.Id}",
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Typ:",
                        Value = info.Type,
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Kiedy:",
                        Value = $"{info.StartDate.ToShortDateString()} {info.StartDate.ToShortTimeString()}"
                    },
                    new EmbedFieldBuilder
                    {
                        IsInline = true,
                        Name = "Na ile:",
                        Value = $"{info.DurationInHours/24} dni {info.DurationInHours%24} godzin",
                    }
                }
            };

            if (channel != null)
                await channel.SendMessageAsync("", embed: embed.Build());

            try
            {
                var dm = await user.GetOrCreateDMChannelAsync();
                if (dm != null)
                {
                    await dm.SendMessageAsync($"Elo! Zostałeś ukarany mutem na {info.DurationInHours/24} dni {info.DurationInHours%24} godzin. Pozdrawiam serdecznie!");
                    await dm.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"in mute: {ex}");
            }
        }

        public async Task<Embed> GetMutedListAsync(Database.ManagmentContext db, SocketCommandContext context)
        {
            string mutedList = "Brak";

            var list = (await db.Penalties.Include(x => x.Roles).FromCacheAsync(new string[] { $"mute" })).Where(x => x.Guild == context.Guild.Id && x.Type == PenaltyType.Mute);
            if (list.Count() > 0)
            {
                mutedList = "";
                foreach (var penalty in list)
                {
                    var endDate = penalty.StartDate.AddHours(penalty.DurationInHours);
                    mutedList += $"{context.Guild.GetUser(penalty.User)?.Mention} [DO: {endDate.ToShortDateString()} {endDate.ToShortTimeString()}] - {penalty.Reason}\n";
                }
            }

            return new EmbedBuilder
            {
                Description = $"**Wyciszeni**:\n\n{mutedList.TrimToLength(1900)}",
                Color = EMType.Bot.Color(),
            }.Build();
        }

        public Embed BuildTodo(IMessage message, SocketGuildUser who)
        {
            string image = "";
            if (message.Attachments.Count > 0)
            {
                var first = message.Attachments.First();

                if (first.Url.IsURLToImage())
                    image = first.Url;
            }

            return new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder().WithUser(message.Author),
                Description = message.Content.TrimToLength(1800),
                Color = EMType.Bot.Color(),
                ImageUrl = image,
                Footer = new EmbedFooterBuilder
                {
                    IconUrl = who.GetAvatarUrl() ?? "https://i.imgur.com/xVIMQiB.jpg",
                    Text = $"Przez: {who.Nickname ?? who.Username}",
                },
            }.Build();
        }

        public async Task UnmuteUserAsync(SocketGuildUser user, SocketRole muteRole, SocketRole muteModRole, Database.ManagmentContext db)
        {
            var penalty = await db.Penalties.Include(x => x.Roles).FirstOrDefaultAsync(x => x.User == user.Id 
                && x.Type == PenaltyType.Mute && x.Guild == user.Guild.Id);

            await UnmuteUserGuildAsync(user, muteRole, muteModRole, penalty?.Roles);
            await RemovePenaltyFromDb(db, penalty);
        }

        private async Task RemovePenaltyFromDb(Database.ManagmentContext db, PenaltyInfo penalty)
        {
            if (penalty == null) return;

            db.OwnedRoles.RemoveRange(penalty.Roles);
            db.Penalties.Remove(penalty);

            await db.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"mute" });
        }

        private async Task MuteUserGuildAsync(SocketGuildUser user, SocketRole muteRole, IEnumerable<OwnedRole> roles)
        {
            if (muteRole != null)
            {
                if (!user.Roles.Contains(muteRole))
                    await user.AddRoleAsync(muteRole);
            }

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    var r = user.Guild.GetRole(role.Role);

                    if (r != null)
                        if (user.Roles.Contains(r))
                            await user.RemoveRoleAsync(r);
                }
            }
        }

        private async Task UnmuteUserGuildAsync(SocketGuildUser user, SocketRole muteRole, SocketRole muteModRole, IEnumerable<OwnedRole> roles)
        {
            if (muteRole != null)
            {
                if (user.Roles.Contains(muteRole))
                    await user.RemoveRoleAsync(muteRole);
            }

            if (muteModRole != null)
            {
                if (user.Roles.Contains(muteModRole))
                    await user.RemoveRoleAsync(muteModRole);
            }

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    var r = user.Guild.GetRole(role.Role);

                    if (r != null)
                        if (!user.Roles.Contains(r))
                            await user.AddRoleAsync(r);
                }
            }
        }

        public async Task<PenaltyInfo> MuteUserAysnc(SocketGuildUser user, SocketRole muteRole, SocketRole muteModRole, SocketRole userRole, 
            Database.ManagmentContext db, long duration, string reason = "nie podano", IEnumerable<ModeratorRoles> modRoles = null)
        {
            var info = new PenaltyInfo
            {
                User = user.Id,
                Reason = reason,
                Guild = user.Guild.Id,
                Type = PenaltyType.Mute,
                StartDate = DateTime.Now,
                DurationInHours = duration,
                Roles = new List<OwnedRole>(),
            };

            await db.Penalties.AddAsync(info);

            if (userRole != null)
            {
                if (user.Roles.Contains(userRole))
                {
                    await user.RemoveRoleAsync(userRole);
                    info.Roles.Add(new OwnedRole
                    {
                        Role = userRole.Id
                    });
                }
            }

            if (modRoles != null)
            {
                foreach (var r in modRoles)
                {
                    var role = user.Roles.FirstOrDefault(x => x.Id == r.Role);
                    if (role == null) continue;

                    await user.RemoveRoleAsync(role);
                    info.Roles.Add(new OwnedRole
                    {
                        Role = role.Id
                    });
                }
            }

            if (!user.Roles.Contains(muteRole))
                await user.AddRoleAsync(muteRole);

            if (muteModRole != null)
            {
                if (!user.Roles.Contains(muteModRole))
                    await user.AddRoleAsync(muteModRole);
            }

            await db.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"mute" });

            return info;
        }
    }
}
