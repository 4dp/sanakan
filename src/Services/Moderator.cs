using Discord;
using Discord.Commands;
using Sanakan.Database.Models.Configuration;
using Sanakan.Extensions;

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
    }
}
