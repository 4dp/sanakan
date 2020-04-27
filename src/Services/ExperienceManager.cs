#pragma warning disable 1591

using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Services
{
    public class ExperienceManager
    {
        private const double SAVE_AT = 3;
        private const double LM = 0.35;

        private Dictionary<ulong, double> _exp;
        private Dictionary<ulong, ulong> _messages;
        private Dictionary<ulong, ulong> _commands;
        private Dictionary<ulong, DateTime> _saved;
        private Dictionary<ulong, ulong> _characters;

        private DiscordSocketClient _client;
        private ImageProcessing _img;
        private IExecutor _executor;
        private IConfig _config;

        public ExperienceManager(DiscordSocketClient client, IExecutor executor, IConfig config, ImageProcessing img)
        {
            _executor = executor;
            _client = client;
            _config = config;
            _img = img;

            _exp = new Dictionary<ulong, double>();
            _saved = new Dictionary<ulong, DateTime>();
            _messages = new Dictionary<ulong, ulong>();
            _commands = new Dictionary<ulong, ulong>();
            _characters = new Dictionary<ulong, ulong>();

#if !DEBUG
            _client.MessageReceived += HandleMessageAsync;
#endif
        }

        public static long CalculateExpForLevel(long level) => (level <= 0) ? 0 : Convert.ToInt64(Math.Floor(Math.Pow(level / LM, 2)) + 1);
        public static long CalculateLevel(long exp) => Convert.ToInt64(Math.Floor(LM * Math.Sqrt(exp)));

        public async Task NotifyAboutLevelAsync(SocketGuildUser user, ISocketMessageChannel channel, long level)
        {
            using (var badge = await _img.GetLevelUpBadgeAsync(user.Nickname ?? user.Username,
                level, user.GetAvatarUrl() ?? "https://i.imgur.com/xVIMQiB.jpg", user.Roles.OrderByDescending(x => x.Position).First().Color))
            {
                using (var badgeStream = badge.ToPngStream())
                {
                    await channel.SendFileAsync(badgeStream, $"{user.Id}.png");
                }
            }

            using (var dba = new Database.AnalyticsContext(_config))
            {
                dba.UsersData.Add(new Database.Models.Analytics.UserAnalytics
                {
                    Value = level,
                    UserId = user.Id,
                    GuildId = user.Guild.Id,
                    MeasureDate = DateTime.Now,
                    Type = Database.Models.Analytics.UserAnalyticsEventType.Level
                });
                dba.SaveChanges();
            }
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook) return;

            var user = message.Author as SocketGuildUser;
            if (user == null) return;

            if (_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
                return;

            bool calculateExp = true;
            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config != null)
                {
                    var role = user.Guild.GetRole(config.UserRole);
                    if (role != null)
                    {
                        if (!user.Roles.Contains(role))
                            return;
                    }

                    if (config.ChannelsWithoutExp != null)
                    {
                        if (config.ChannelsWithoutExp.Any(x => x.Channel == message.Channel.Id))
                            calculateExp = false;
                    }
                }
            }

            if (!_messages.Any(x => x.Key == user.Id))
            {
                using (var db = new Database.UserContext(_config))
                {
                    if (!db.Users.AsNoTracking().Any(x => x.Id == user.Id))
                    {
                        var task = CreateUserTask(user);
                        await _executor.TryAdd(new Executable("add user", task), TimeSpan.FromSeconds(1));
                    }
                }
            }

            CountMessage(user.Id, message.Content.IsCommand(_config.Get().Prefix));
            CalculateExpAndCreateTask(user, message, calculateExp);
        }

        private bool CheckLastSave(ulong userId)
        {
            if (!_saved.Any(x => x.Key == userId))
            {
                _saved.Add(userId, DateTime.Now);
                return false;
            }

            return (DateTime.Now - _saved[userId].AddMinutes(30)).TotalSeconds > 1;
        }

        private void CountMessage(ulong userId, bool isCommand)
        {
            if (!_messages.Any(x => x.Key == userId))
                _messages.Add(userId, 1);
            else
                _messages[userId]++;

            if (!_commands.Any(x => x.Key == userId))
                _commands.Add(userId, isCommand ? 1u : 0u);
            else
                if (isCommand)
                _commands[userId]++;
        }

        private void CountCharacters(ulong userId, ulong characters)
        {
            if (!_characters.Any(x => x.Key == userId))
                _characters.Add(userId, characters);
            else
                _characters[userId] += characters;
        }

        private void CalculateExpAndCreateTask(SocketGuildUser user, SocketMessage message, bool calculateExp)
        {
            var exp = GetPointsFromMsg(message);
            if (!calculateExp) exp = 0;

            if (!_exp.Any(x => x.Key == user.Id))
            {
                _exp.Add(message.Author.Id, exp);
                return;
            }

            _exp[user.Id] += exp;

            var saved = _exp[user.Id];
            if (saved < SAVE_AT && !CheckLastSave(user.Id)) return;

            var fullP = (long)Math.Floor(saved);
            _exp[message.Author.Id] -= fullP;
            _saved[user.Id] = DateTime.Now;

            var task = CreateTask(user, message.Channel, fullP, _messages[user.Id], _commands[user.Id], _characters[user.Id], calculateExp);
            _characters[user.Id] = 0;
            _messages[user.Id] = 0;
            _commands[user.Id] = 0;

            _executor.TryAdd(new Executable("add exp", task), TimeSpan.FromSeconds(1));
        }

        private double GetPointsFromMsg(SocketMessage message)
        {
            int emoteChars = message.Tags.CountEmotesTextLenght();
            int linkChars = message.Content.CountLinkTextLength();
            int nonWhiteSpaceChars = message.Content.Count(c => c != ' ');
            int quotedChars = message.Content.CountQuotedTextLength();
            double charsThatMatters = nonWhiteSpaceChars - linkChars - emoteChars - quotedChars;

            CountCharacters(message.Author.Id, (ulong)(charsThatMatters < 0 ? 1 : charsThatMatters));
            return GetExpPointBasedOnCharCount(charsThatMatters);
        }

        private double GetExpPointBasedOnCharCount(double charCount)
        {
            var tmpCnf = _config.Get();
            double cpp = tmpCnf.Exp.CharPerPoint;
            double min = tmpCnf.Exp.MinPerMessage;
            double max = tmpCnf.Exp.MaxPerMessage;

            double experience = charCount / cpp;
            if (experience < min) return min;
            if (experience > max) return max;
            return experience;
        }

        private Task CreateUserTask(SocketGuildUser user)
        {
            return new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    if (!db.Users.Any(x => x.Id == user.Id))
                    {
                        var bUser = new Database.Models.User().Default(user.Id);
                        db.Users.Add(bUser);
                        db.SaveChanges();
                    }
                }
            });
        }

        private Task CreateTask(SocketGuildUser user, ISocketMessageChannel channel, long exp, ulong messages, ulong commands, ulong characters, bool calculateExp)
        {
            return new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var usr = db.GetUserOrCreateAsync(user.Id).Result;
                    if (usr == null) return;

                    if ((DateTime.Now - usr.MeasureDate.AddMonths(1)).TotalSeconds > 1)
                    {
                        usr.MeasureDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                        usr.MessagesCntAtDate = usr.MessagesCnt;
                        usr.CharacterCntFromDate = characters;
                    }
                    else
                        usr.CharacterCntFromDate += characters;

                    usr.ExpCnt += exp;
                    usr.MessagesCnt += messages;
                    usr.CommandsCnt += commands;

                    var newLevel = CalculateLevel(usr.ExpCnt);
                    if (newLevel != usr.Level && calculateExp)
                    {
                        usr.Level = newLevel;
                        _ = Task.Run(async () => { await NotifyAboutLevelAsync(user, channel, newLevel); });
                    }

                    _ = Task.Run(async () =>
                    {
                        using (var dbc = new Database.GuildConfigContext(_config))
                        {
                            var config = await dbc.GetCachedGuildFullConfigAsync(user.Guild.Id);
                            if (config == null) return;
                            if (!calculateExp) return;

                            foreach (var lvlRole in config.RolesPerLevel)
                            {
                                var role = user.Guild.GetRole(lvlRole.Role);
                                if (role == null) continue;

                                bool hasRole = user.Roles.Any(x => x.Id == role.Id);
                                if (newLevel >= (long)lvlRole.Level)
                                {
                                    if (!hasRole)
                                        await user.AddRoleAsync(role);
                                }
                                else if (hasRole)
                                {
                                    await user.RemoveRoleAsync(role);
                                }
                            }
                        }
                    });

                    db.SaveChanges();
                }
            });
        }
    }
}