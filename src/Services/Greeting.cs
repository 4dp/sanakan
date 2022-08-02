#pragma warning disable 1591

using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Shinden.Logger;
using Z.EntityFramework.Plus;

namespace Sanakan.Services
{
    public class Greeting
    {
        private DiscordSocketClient _client { get; set; }
        private IExecutor _executor { get; set; }
        private ILogger _logger { get; set; }
        private IConfig _config { get; set; }

        public Greeting(DiscordSocketClient client, ILogger logger, IConfig config, IExecutor exe)
        {
            _client = client;
            _logger = logger;
            _config = config;
            _executor = exe;

#if !DEBUG
            _client.LeftGuild += BotLeftGuildAsync;
            _client.UserJoined += UserJoinedAsync;
            _client.UserLeft += UserLeftAsync;
#endif
        }

        private async Task BotLeftGuildAsync(SocketGuild guild)
        {
            using (var db = new Database.GuildConfigContext(_config))
            {
                var gConfig = await db.GetGuildConfigOrCreateAsync(guild.Id);
                db.Guilds.Remove(gConfig);

                var stats = db.TimeStatuses.AsQueryable().AsSplitQuery().Where(x => x.Guild == guild.Id).ToList();
                db.TimeStatuses.RemoveRange(stats);

                await db.SaveChangesAsync();
            }

            using (var db = new Database.ManagmentContext(_config))
            {
                var mute = db.Penalties.AsQueryable().AsSplitQuery().Where(x => x.Guild == guild.Id).ToList();
                db.Penalties.RemoveRange(mute);

                await db.SaveChangesAsync();
            }
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook) return;

            if (_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
                return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config?.WelcomeMessage == null) return;
                if (config.WelcomeMessage == "off") return;

                await SendMessageAsync(ReplaceTags(user, config.WelcomeMessage), user.Guild.GetTextChannel(config.GreetingChannel));

                if (config?.WelcomeMessagePW == null) return;
                if (config.WelcomeMessagePW == "off") return;

                try
                {
                    var pw = await user.CreateDMChannelAsync();
                    await pw.SendMessageAsync(ReplaceTags(user, config.WelcomeMessagePW));
                    await pw.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.Log($"Greeting: {ex}");
                }
            }
        }

        private async Task UserLeftAsync(SocketGuild guild, SocketUser user)
        {
            ulong channelId = 0;
            ulong adminRoleId = 0;

            if (user.IsBot || user.IsWebhook) return;

            if (!_config.Get().BlacklistedGuilds.Any(x => x == guild.Id))
            {
                using (var db = new Database.GuildConfigContext(_config))
                {
                    var config = await db.GetCachedGuildFullConfigAsync(guild.Id);
                    if (config?.GoodbyeMessage == null) return;
                    if (config.GoodbyeMessage == "off") return;

                    await SendMessageAsync(ReplaceTags(user, config.GoodbyeMessage), guild.GetTextChannel(config.GreetingChannel));

                    channelId = config.GreetingChannel;
                    adminRoleId = config.AdminRole;
                }
            }

            if (_client.Guilds.Any(x => x.Id != guild.Id && x.Users.Any(u => u.Id == user.Id)))
                return;

            var moveTask = new Task<Task>(async () =>
            {
                try
                {
                    using (var db = new Database.UserContext(_config))
                    {
                        var duser = await db.GetUserOrCreateAsync(user.Id);
                        var fakeu = await db.GetUserOrCreateAsync(1);

                        foreach (var card in duser.GameDeck.Cards)
                        {
                            card.InCage = false;
                            card.TagList.Clear();
                            card.LastIdOwner = user.Id;
                            card.GameDeckId = fakeu.GameDeck.Id;
                        }

                        foreach (var w in duser.GameDeck.Wishes.Where(x => x.Type == Database.Models.WishlistObjectType.Character))
                        {
                            await db.WishlistCountData.CreateOrChangeWishlistCountByAsync(w.ObjectId, w.ObjectName, -1);
                        }

                        db.Users.Remove(duser);

                        await db.SaveChangesAsync();

                        QueryCacheManager.ExpireTag(new string[] { "users" });
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log("In user leave:" + ex.ToString());
                    await SendMessageAsync($"**ERR:** `{user.Id}` {guild.GetRole(adminRoleId)?.Mention}", guild.GetTextChannel(channelId));
                }
            });

            await _executor.TryAdd(new Executable("delete user", moveTask, Priority.High), TimeSpan.FromSeconds(1));
        }

        private async Task SendMessageAsync(string message, ITextChannel channel)
        {
            if (channel != null) await channel.SendMessageAsync(message);
        }

        private string ReplaceTags(SocketUser user, string message)
            => message.Replace("^nick", user.Username).Replace("^mention", user.Mention);
    }
}