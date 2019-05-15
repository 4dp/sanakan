#pragma warning disable 1591

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
        Level, ScCnt, TcCnt, Posts, PostsMonthly, PostsMonthlyCharacter, Commands, Cards
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
                        await CyclicCheckAsync(db);
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

        private async Task CyclicCheckAsync(Database.UserContext context)
        {
            var subs = context.TimeStatuses.Include(x => x.Guild).FromCache(new[] { "users" }).Where(x => x.Type.IsSubType());
            foreach (var sub in subs)
            {
                if (sub.IsActive())
                    continue;

                var guild = _client.GetGuild(sub.GuildId);
                switch (sub.Type)
                {
                    case StatusType.Globals:
                        await RemoveRoleAsync(guild, sub?.Guild?.GlobalEmotesRole ?? 0, sub.UserId);
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
                    return list.OrderByDescending(x => x.Level).ToList();

                case TopType.ScCnt:
                    return list.OrderByDescending(x => x.ScCnt).ToList();

                case TopType.TcCnt:
                    return list.OrderByDescending(x => x.TcCnt).ToList();

                case TopType.Posts:
                    return list.OrderByDescending(x => x.MessagesCnt).ToList();

                case TopType.PostsMonthly:
                    return list.Where(x => x.IsCharCounterActive()).OrderByDescending(x => x.MessagesCntAtDate - x.MessagesCnt).ToList();

                case TopType.PostsMonthlyCharacter:
                    return list.Where(x => x.IsCharCounterActive()).OrderByDescending(x => x.CharacterCntFromDate / (x.MessagesCntAtDate - x.MessagesCnt)).ToList();

                case TopType.Commands:
                    return list.OrderByDescending(x => x.CommandsCnt).ToList();

                case TopType.Cards:
                    return list.OrderByDescending(x => x.GameDeck.Cards.Count).ToList();
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

        public async Task<Stream> GetProfileImageAsync(SocketGuildUser user, Database.Models.User botUser, long topPosition)
        {
            var response = await _shClient.User.GetAsync(botUser.Shinden);

            using (var image = await _img.GetUserProfileAsync(response.Body, botUser, user.GetAvatarUrl().Split("?")[0],
                topPosition, user.Nickname ?? user.Username, user.Roles.OrderByDescending(x => x.Position).First().Color))
            {
                return image.ToPngStream();
            }
        }

        public async Task<bool> SaveProfileImageAsync(string imgUrl, string path, int width = 0, int height = 0, bool streach = false)
        {
            if (imgUrl == null)
                return false;

            if (!imgUrl.IsURLToImage())
                return false;

            try
            {
                await _img.SaveImageFromUrlAsync(imgUrl, path, new Size(width, height), streach);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}