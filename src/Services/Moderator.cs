using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
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
        private ILogger _logger;

        public Moderator(ILogger logger)
        {
            _logger = logger;
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
                            + $"**Notification:** {context.Guild.GetTextChannel(config.NotificationChannel)?.Mention ?? "--"}\n"
                            + $"**Raport:** {context.Guild.GetTextChannel(config.RaportChannel)?.Mention ?? "--"}\n"
                            + $"**Todos:** {context.Guild.GetTextChannel(config.ToDoChannel)?.Mention ?? "--"}\n"
                            + $"**Quiz:** {context.Guild.GetTextChannel(config.QuizChannel)?.Mention ?? "--"}\n"
                            + $"**Nsfw:** {context.Guild.GetTextChannel(config.NsfwChannel)?.Mention ?? "--"}\n"
                            + $"**Log:** {context.Guild.GetTextChannel(config.LogChannel)?.Mention ?? "--"}\n\n"

                            + $"**W Cmd**: {wcmd}\n"
                            + $"**W Fight**: {wfCh}\n"
                            + $"**Mods**: {mods}\n"
                            + $"**NonExp**: {wExp}\n"
                            + $"**NonSup**: {wSup}\n"
                            + $"**CmdCh**: {cmdCh}\n"
                            + $"**Roles**: {roles}\n"
                            + $"**SelfRoles**: {selfRoles}\n"
                            + $"**MyLands**: {lands}".TrimToLength(1950)
            };
        }

        private EmbedBuilder GetSelfRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**SelfRoles:**\n\n";
            if (config.SelfRoles?.Count > 0)
            {
                foreach (var role in config.SelfRoles)
                    value += $"*{role.Name}* - {context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetModRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Moderator roles:**\n\n";
            if (config.ModeratorRoles?.Count > 0)
            {
                foreach (var role in config.ModeratorRoles)
                    value += $"{context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetLandsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Lands:**\n\n";
            if (config.Lands?.Count > 0)
            {
                foreach (var land in config.Lands)
                    value += $"*{land.Name}*: M:{context.Guild.GetRole(land.Manager)?.Mention ?? "usunięta"} U:{context.Guild.GetRole(land.Underling)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetLevelRolesConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Role:**\n\n";
            if (config.RolesPerLevel?.Count > 0)
            {
                foreach (var role in config.RolesPerLevel)
                    value += $"*{role.Level}*: {context.Guild.GetRole(role.Role)?.Mention ?? "usunięta"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetCmdChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Cmd Channels:**\n\n";
            if (config.CommandChannels?.Count > 0)
            {
                foreach (var channel in config.CommandChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetWaifuCmdChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Waifu Cmd Channels:**\n\n";
            if (config.WaifuConfig?.CommandChannels?.Count > 0)
            {
                foreach (var channel in config.WaifuConfig.CommandChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetWaifuFightChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**Waifu Fight Channels:**\n\n";
            if (config.WaifuConfig?.FightChannels?.Count > 0)
            {
                foreach (var channel in config.WaifuConfig.FightChannels)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetNonExpChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**NonExp Channels:**\n\n";
            if (config.ChannelsWithoutExp?.Count > 0)
            {
                foreach (var channel in config.ChannelsWithoutExp)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        private EmbedBuilder GetNonSupChannelsConfig(GuildOptions config, SocketCommandContext context)
        {
            string value = "**NonSup Channels:**\n\n";
            if (config.ChannelsWithoutSupervision?.Count > 0)
            {
                foreach (var channel in config.ChannelsWithoutSupervision)
                    value += $"{context.Guild.GetTextChannel(channel.Channel)?.Mention ?? "usunięty"}\n";
            }
            else value += "*brak*";

            return new EmbedBuilder().WithDescription(value.TrimToLength(1950));
        }

        public EmbedBuilder GetConfiguration(GuildOptions config, SocketCommandContext context, ConfigType type)
        {
            switch (type)
            {
                //TODO: case ConfigType.RichMessages:
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

        public async Task<bool> UnmuteUserAsync(SocketGuildUser user, SocketRole muteRole, Database.ManagmentContext db)
        {
            var penalty = await db.Penalties.Include(x => x.Roles).FirstOrDefaultAsync(x => x.User == user.Id 
                && x.Type == PenaltyType.Mute && x.Guild == user.Guild.Id);

            if (user.Roles.Contains(muteRole))
                await user.RemoveRoleAsync(muteRole);

            if (penalty == null) return false;

            foreach (var role in penalty.Roles)
            {
                var r = user.Guild.GetRole(role.Role);
                if (r != null) await user.AddRoleAsync(r);
            }

            db.OwnedRoles.RemoveRange(penalty.Roles);
            db.Penalties.Remove(penalty);

            await db.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"mute" });

            return true;
        }

        public async Task<PenaltyInfo> MuteUserAysnc(SocketGuildUser user, SocketRole muteRole, SocketRole userRole, 
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

            await db.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"mute" });

            return info;
        }
    }
}
