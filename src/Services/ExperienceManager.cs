using Discord.WebSocket;
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
        private const double LM = 0.35;

        private Dictionary<ulong, double> _mem;

        private DiscordSocketClient _client;
        private IExecutor _executor;
        private IConfig _config;

        public ExperienceManager(DiscordSocketClient client, IExecutor executor, IConfig config)
        {
            _executor = executor;
            _client = client;
            _config = config;

            _mem = new Dictionary<ulong, double>();
#if !DEBUG
            _client.MessageReceived += HandleMessageAsync;
#endif
        }

        public long CalculateExpForLevel(long level) => (level <= 0) ? 0 : Convert.ToInt64(Math.Floor(Math.Pow(level / LM, 2)) + 1);
        public long CalculateLevel(long exp) => Convert.ToInt64(Math.Floor(LM * Math.Sqrt(exp)));

        public async Task NotifyAboutLevelAsync(SocketGuildUser user, ISocketMessageChannel channel, long level)
        {
            await channel.SendMessageAsync("", embed: $"{user.Nickname ?? user.Username} awansował na {level} poziom!".ToEmbedMessage(EMType.Bot).Build());
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook) return;

            var user = message.Author as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config != null)
                {
                    if (config.ChannelsWithoutExp != null)
                        if (config.ChannelsWithoutExp.Any(x => x.Channel == message.Channel.Id))
                            return;

                    var role = user.Guild.GetRole(config.UserRole);
                    if (role != null)
                        if (!user.Roles.Contains(role))
                            return;
                }
            }

            CalculateExpAndCreateTask(user, message);
        }

        private void CalculateExpAndCreateTask(SocketGuildUser user, SocketMessage message)
        {
            var exp = GetPointsFromMsg(message);
            if (!_mem.Any(x => x.Key == user.Id))
            {
                _mem.Add(message.Author.Id, exp);
                return;
            }
            _mem[user.Id] += exp;

            var saved = _mem[user.Id];
            if (saved < 1) return;

            var fullP = (long)Math.Floor(saved);
            _mem[message.Author.Id] -= fullP;

            var task = CreateTask(user, message.Channel, fullP);
            _executor.TryAdd(new Executable(task), TimeSpan.FromSeconds(1));
        }

        private double GetPointsFromMsg(SocketMessage message)
        {
            int emoteChars = message.Tags.CountEmotesTextLenght();
            int linkChars = message.Content.CountLinkTextLength();
            int nonWhiteSpaceChars = message.Content.Count(c => c != ' ');
            double charsThatMatters = nonWhiteSpaceChars - linkChars - emoteChars;

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

        private Task<bool> CreateTask(SocketGuildUser user, ISocketMessageChannel channel, long exp)
        {
            return new Task<bool>(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var usr = db.Users.FirstOrDefault(x => x.Id == user.Id);
                    if (usr == null) return false;

                    usr.ExpCnt += exp;

                    var newLevel = CalculateLevel(usr.ExpCnt);
                    if (newLevel != usr.Level)
                    {
                        usr.Level = newLevel;
                        _ = Task.Run(async () => { await NotifyAboutLevelAsync(user, channel, newLevel); });
                    }

                    db.SaveChanges();
                }

                return true;
            });
        }
    }
}
