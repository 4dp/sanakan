#pragma warning disable 1591

using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Shinden;
using Shinden.Logger;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Services
{
    public enum TopType
    {
        Level, ScCnt, TcCnt, Posts, PostsMonthly, PostsMonthlyCharacter, Commands, Cards, CardsPower, Card, Karma, KarmaNegative, Pvp, PvpSeason, PcCnt, AcCnt
    }

    public enum SCurrency
    {
        Sc, Tc
    }

    public enum SaveResult
    {
        Error,
        BadUrl,
        Success
    }

    public enum FColor : uint
    {
        None = 0x000000,
        CleanColor = 0x111111,

        Violet = 0xFF2BDF,
        LightPink = 0xDBB0EF,
        Pink = 0xFF0090,
        Green = 0x33CC66,
        LightGreen = 0x93F600,
        LightOrange = 0xFFC125,
        Orange = 0xFF731C,
        DarkGreen = 0x808000,
        LightYellow = 0xE2E1A3,
        Yellow = 0xFDE456,
        Grey = 0x828282,
        LightBlue = 0x7FFFD4,
        Purple = 0x9A49EE,
        Brown = 0xA85100,
        Red = 0xF31400,
        AgainBlue = 0x11FAFF,
        AgainPinkish = 0xFF8C8C,
        DefinitelyNotWhite = 0xFFFFFE,

        GrassGreen = 0x66FF66,
        SeaTurquoise = 0x33CCCC,
        Beige = 0xA68064,
        Pistachio = 0x8FBC8F,
        DarkSkyBlue = 0x5959AB,
        Lilac = 0xCCCCFF,
        SkyBlue = 0x99CCFF,

        NeonPink = 0xE1137A,
        ApplePink = 0xFF0033,
        RosePink = 0xFF3366,
        LightLilac = 0xFF99CC,
        PowderPink = 0xFF66CC,
        CherryPurple = 0xCC0099,
        BalloonPurple = 0xBE4DCC,
        SoftPurple = 0xE37EEB,
        CleanSkyBlue = 0x6666FF,
        WeirdGreen = 0x97F5AE,
        DirtyGreen = 0x739546,
        LightBeige = 0xCCCC66,
        TrueYellow = 0xFFFF00,
        OrangeFox = 0xFF9900,
        Salmon = 0xFF8049,
        BearBrown = 0xCC3300,
        DarkRose = 0x993333,
        LightRose = 0xAD4A4A,
        DarkBeige = 0x996633,
        SkinColor = 0xFFC18A,
        DirtyLilac = 0x996666,
        Silver = 0xC0C0C0,

        Ejzur = 0x007FFF,
        BlueBlueBlue = 0x1F75FE,
    }

    public class Profile
    {
        private DiscordSocketClient _client;
        private ShindenClient _shClient;
        private ImageProcessing _img;
        private ILogger _logger;
        private IConfig _config;
        private Timer _timer;

        public Profile(DiscordSocketClient client, ShindenClient shClient, ImageProcessing img, ILogger logger, IConfig config)
        {
            _shClient = shClient;
            _client = client;
            _logger = logger;
            _config = config;
            _img = img;

            _timer = new Timer(async _ =>
            {
                try
                {
                    using (var db = new Database.UserContext(_config))
                    {
                        using (var dbg = new Database.GuildConfigContext(_config))
                        {
                            await CyclicCheckAsync(db, dbg);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log($"in profile check: {ex}");
                }
            },
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
        }

        private async Task CyclicCheckAsync(Database.UserContext context, Database.GuildConfigContext guildContext)
        {
            var subs = context.TimeStatuses.AsNoTracking().FromCache(new[] { "users" }).Where(x => x.Type.IsSubType());
            foreach (var sub in subs)
            {
                if (sub.IsActive())
                    continue;

                var guild = _client.GetGuild(sub.Guild);
                switch (sub.Type)
                {
                    case StatusType.Globals:
                        var guildConfig = await guildContext.GetCachedGuildFullConfigAsync(sub.Guild);
                        await RemoveRoleAsync(guild, guildConfig?.GlobalEmotesRole ?? 0, sub.UserId);
                        break;

                    case StatusType.Color:
                        await RomoveUserColorAsync(guild.GetUser(sub.UserId));
                        break;

                    default:
                        break;
                }
            }
        }

        private async Task RemoveRoleAsync(SocketGuild guild, ulong roleId, ulong userId)
        {
            var role = guild.GetRole(roleId);
            if (role == null) return;

            var user = guild.GetUser(userId);
            if (user == null) return;

            await user.RemoveRoleAsync(role);
        }

        public bool HasSameColor(SocketGuildUser user, FColor color)
        {
            if (user == null) return false;

            var colorNumeric = (uint)color;
            return user.Roles.Any(x => x.Name == colorNumeric.ToString());
        }

        public async Task<bool> SetUserColorAsync(SocketGuildUser user, ulong adminRole, FColor color)
        {
            if (user == null) return false;

            var colorNumeric = (uint)color;
            var aRole = user.Guild.GetRole(adminRole);
            if (aRole == null) return false;

            var cRole = user.Guild.Roles.FirstOrDefault(x => x.Name == colorNumeric.ToString());
            if (cRole == null)
            {
                var dColor = new Color(colorNumeric);
                var createdRole = await user.Guild.CreateRoleAsync(colorNumeric.ToString(), GuildPermissions.None, dColor, false, false);
                await createdRole.ModifyAsync(x => x.Position = aRole.Position + 1);
                await user.AddRoleAsync(createdRole);
                return true;
            }

            if (!user.Roles.Contains(cRole))
                await user.AddRoleAsync(cRole);

            return true;
        }

        public async Task RomoveUserColorAsync(SocketGuildUser user)
        {
            if (user == null) return;

            foreach(uint color in Enum.GetValues(typeof(FColor)))
            {
                var cR = user.Roles.FirstOrDefault(x => x.Name == color.ToString());
                if (cR != null)
                {
                    if (cR.Members.Count() == 1)
                    {
                        await cR.DeleteAsync();
                        return;
                    }
                    await user.RemoveRoleAsync(cR);
                }
            }
        }

        public List<User> GetTopUsers(List<User> list, TopType type)
            => GetRangeMax(OrderUsersByTop(list, type), 50);

        private List<T> GetRangeMax<T>(List<T> list, int range)
            => list.GetRange(0, list.Count > range ? range : list.Count);

        private List<User> OrderUsersByTop(List<User> list, TopType type)
        {
            switch (type)
            {
                default:
                case TopType.Level:
                    return list.OrderByDescending(x => x.ExpCnt).ToList();

                case TopType.ScCnt:
                    return list.OrderByDescending(x => x.ScCnt).ToList();

                case TopType.TcCnt:
                    return list.OrderByDescending(x => x.TcCnt).ToList();

                case TopType.AcCnt:
                    return list.OrderByDescending(x => x.AcCnt).ToList();

                case TopType.PcCnt:
                    return list.OrderByDescending(x => x.GameDeck.PVPCoins).ToList();

                case TopType.Posts:
                    return list.OrderByDescending(x => x.MessagesCnt).ToList();

                case TopType.PostsMonthly:
                    return list.Where(x => x.IsCharCounterActive()).OrderByDescending(x => x.MessagesCnt - x.MessagesCntAtDate).ToList();

                case TopType.PostsMonthlyCharacter:
                    return list.Where(x => x.IsCharCounterActive() && x.SendAnyMsgInMonth()).OrderByDescending(x => x.CharacterCntFromDate / (x.MessagesCnt - x.MessagesCntAtDate)).ToList();

                case TopType.Commands:
                    return list.OrderByDescending(x => x.CommandsCnt).ToList();

                case TopType.Card:
                    return list.OrderByDescending(x => x.GameDeck.GetStrongestCardPower()).ToList();

                case TopType.Cards:
                    return list.OrderByDescending(x => x.GameDeck.Cards.Count).ToList();

                case TopType.CardsPower:
                    return list.OrderByDescending(x => x.GameDeck.Cards.Sum(c => c.GetCardPower())).ToList();

                case TopType.Karma:
                    return list.OrderByDescending(x => x.GameDeck.Karma).ToList();

                case TopType.KarmaNegative:
                    return list.OrderBy(x => x.GameDeck.Karma).ToList();

                case TopType.Pvp:
                    return list.Where(x => x.GameDeck.GlobalPVPRank > 0).OrderByDescending(x => x.GameDeck.GlobalPVPRank).ToList();

                case TopType.PvpSeason:
                    return list.Where(x => x.IsPVPSeasonalRankActive() && x.GameDeck.SeasonalPVPRank > 0).OrderByDescending(x => x.GameDeck.SeasonalPVPRank).ToList();
            }
        }

        public List<string> BuildListView(List<User> list, TopType type, SocketGuild guild)
        {
            var view = new List<string>();

            foreach (var user in list)
            {
                var bUsr = guild.GetUser(user.Id);
                if (bUsr == null) continue;

                view.Add($"{bUsr.Mention}: {user.GetViewValueForTop(type)}");
            }

            return view;
        }

        public Stream GetColorList(SCurrency currency)
        {
            using (var image = _img.GetFColorsView(currency))
            {
                return image.ToPngStream();
            }
        }

        public async Task<Stream> GetProfileImageAsync(SocketGuildUser user, Database.Models.User botUser, long topPosition)
        {
            var response = await _shClient.User.GetAsync(botUser.Shinden);

            using (var image = await _img.GetUserProfileAsync(response.Body, botUser, user.GetAvatarUrl()?.Split("?")[0] ?? "https://i.imgur.com/xVIMQiB.jpg",
                topPosition, user.Nickname ?? user.Username, user.Roles.OrderByDescending(x => x.Position).FirstOrDefault()?.Color ?? Discord.Color.DarkerGrey))
            {
                return image.ToPngStream();
            }
        }

        public async Task<SaveResult> SaveProfileImageAsync(string imgUrl, string path, int width = 0, int height = 0, bool streach = false)
        {
            if (imgUrl == null)
                return SaveResult.BadUrl;

            if (!imgUrl.IsURLToImage())
                return SaveResult.BadUrl;

            try
            {
                if (File.Exists(path)) File.Delete(path);
                await _img.SaveImageFromUrlAsync(imgUrl, path, new Size(width, height), streach);
            }
            catch (Exception)
            {
                return SaveResult.Error;
            }

            return SaveResult.Success;
        }
    }
}