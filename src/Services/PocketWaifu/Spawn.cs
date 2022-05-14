#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Shinden.Logger;
using Shinden.Models;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.PocketWaifu
{
    public class Spawn
    {
        private DiscordSocketClient _client;
        private IExecutor _executor;
        private ILogger _logger;
        private IConfig _config;
        private Waifu _waifu;

        private Dictionary<ulong, long> ServerCounter;
        private Dictionary<ulong, long> UserCounter;

        private Emoji ClaimEmote = new Emoji("🖐");

        public Spawn(DiscordSocketClient client, IExecutor executor, Waifu waifu, IConfig config, ILogger logger)
        {
            _executor = executor;
            _client = client;
            _logger = logger;
            _config = config;
            _waifu = waifu;

            ServerCounter = new Dictionary<ulong, long>();
            UserCounter = new Dictionary<ulong, long>();
#if !DEBUG
            _client.MessageReceived += HandleMessageAsync;
#endif
        }

        private void HandleGuildAsync(ITextChannel spawnChannel, ITextChannel trashChannel, long daily, string mention, bool noExp, SocketRole muteRole)
        {
            if (!ServerCounter.Any(x => x.Key == spawnChannel.GuildId))
            {
                ServerCounter.Add(spawnChannel.GuildId, 0);
                return;
            }

            if (ServerCounter[spawnChannel.GuildId] == 0)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromDays(1));
                    ServerCounter[spawnChannel.GuildId] = 0;
                });
            }

            int chance = noExp ? 285 : 85;
            if (daily > 0 && ServerCounter[spawnChannel.GuildId] >= daily) return;
            if (!_config.Get().SafariEnabled) return;
            if (!Fun.TakeATry(chance)) return;

            ServerCounter[spawnChannel.GuildId] += 1;
            _ = Task.Run(async () =>
            {
                await SpawnCardAsync(spawnChannel, trashChannel, mention, muteRole);
            });
        }

        private void RunSafari(EmbedBuilder embed, IUserMessage msg, Card newCard,
            SafariImage pokeImage, ICharacterInfo character, ITextChannel trashChannel, SocketRole mutedRole)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));

                    var usersReacted = await msg.GetReactionUsersAsync(ClaimEmote, 300).FlattenAsync();
                    var users = usersReacted.ToList();

                    IUser winner = null;
                    using (var db = new Database.UserContext(_config))
                    {
                        var watch = Stopwatch.StartNew();
                        while (winner == null)
                        {
                            if (watch.ElapsedMilliseconds > 60000)
                                throw new Exception("Timeout");

                            if (users.Count < 1)
                            {
                                embed.Description = $"Na polowanie nie stawił się żaden łowca!";
                                await msg.ModifyAsync(x => x.Embed = embed.Build());
                                return;
                            }

                            bool muted = false;
                            var selected = Fun.GetOneRandomFrom(users);
                            if (mutedRole != null && selected is SocketGuildUser su)
                            {
                                if (su.Roles.Any(x => x.Id == mutedRole.Id))
                                    muted = true;
                            }

                            var dUser = await db.GetCachedFullUserAsync(selected.Id);

                            if (dUser != null && !muted)
                            {
                                if (!dUser.IsBlacklisted && dUser.GameDeck.MaxNumberOfCards > dUser.GameDeck.Cards.Count)
                                {
                                    winner = selected;
                                }
                            }
                            users.Remove(selected);
                        }
                    }

                    var exe = GetSafariExe(embed, msg, newCard, pokeImage, character, trashChannel, winner);
                    await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
                    await msg.RemoveAllReactionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Log($"In Safari: {ex}");
                    await msg.ModifyAsync(x => x.Embed = "Karta uciekła!".ToEmbedMessage(EMType.Error).Build());
                    await msg.RemoveAllReactionsAsync();
                }
            });
        }

        private Executable GetSafariExe(EmbedBuilder embed, IUserMessage msg, Card newCard,
            SafariImage pokeImage, ICharacterInfo character, ITextChannel trashChannel, IUser winner)
        {
            return new Executable("safari", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var botUser = await db.GetUserOrCreateAsync(winner.Id);

                    newCard.FirstIdOwner = winner.Id;
                    newCard.Affection += botUser.GameDeck.AffectionFromKarma();
                    botUser.GameDeck.RemoveCharacterFromWishList(newCard.Character);

                    botUser.GameDeck.Cards.Add(newCard);
                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users" });

                    using (var dba = new Database.AnalyticsContext(_config))
                    {
                        dba.UsersData.Add(new Database.Models.Analytics.UserAnalytics
                        {
                            Value = 1,
                            UserId = winner.Id,
                            MeasureDate = DateTime.Now,
                            GuildId = trashChannel?.Guild?.Id ?? 0,
                            Type = Database.Models.Analytics.UserAnalyticsEventType.Card
                        });
                        await dba.SaveChangesAsync();
                    }
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        embed.ImageUrl = await _waifu.GetSafariViewAsync(pokeImage, newCard, trashChannel);
                        embed.Description = $"{winner.Mention} zdobył na polowaniu i wsadził do klatki:\n"
                                        + $"{newCard.GetString(false, false, true)}\n({newCard.Title})";
                        await msg.ModifyAsync(x => x.Embed = embed.Build());

                        var privEmb = new EmbedBuilder()
                        {
                            Color = EMType.Info.Color(),
                            Description = $"Na [polowaniu]({msg.GetJumpUrl()}) zdobyłeś: {newCard.GetString(false, false, true)}"
                        };

                        var priv = await winner.CreateDMChannelAsync();
                        if (priv != null) await priv.SendMessageAsync("", false, privEmb.Build());
                    }
                    catch (Exception ex)
                    {
                        _logger.Log($"In Safari: {ex}");
                    }
                });
            }));
        }

        private async Task SpawnCardAsync(ITextChannel spawnChannel, ITextChannel trashChannel, string mention, SocketRole muteRole)
        {
            var character = await _waifu.GetRandomCharacterAsync();
            if (character == null)
            {
                _logger.Log("In Satafi: bad shinden connection");
                return;
            }

            var newCard = _waifu.GenerateNewCard(null, character);
            newCard.Source = CardSource.Safari;
            newCard.Affection -= 1.8;
            newCard.InCage = true;

            var pokeImage = _waifu.GetRandomSarafiImage();
            var time = DateTime.Now.AddMinutes(5);
            var embed = new EmbedBuilder
            {
                Color = EMType.Bot.Color(),
                Description = $"**Polowanie zakończy się o**: `{time.ToShortTimeString()}:{time.Second.ToString("00")}`",
                ImageUrl = await _waifu.GetSafariViewAsync(pokeImage, trashChannel)
            };

            var msg = await spawnChannel.SendMessageAsync(mention, embed: embed.Build());
            RunSafari(embed, msg, newCard, pokeImage, character, trashChannel, muteRole);
            await msg.AddReactionAsync(ClaimEmote);
        }

        private void HandleUserAsync(SocketUserMessage message)
        {
            var author = message.Author;
            if (!UserCounter.Any(x => x.Key == author.Id))
            {
                UserCounter.Add(author.Id, GetMessageRealLenght(message));
                return;
            }

            var charNeeded = _config.Get().CharPerPacket;
            if (charNeeded <= 0) charNeeded = 3250;

            UserCounter[author.Id] += GetMessageRealLenght(message);
            if (UserCounter[author.Id] > charNeeded)
            {
                UserCounter[author.Id] = 0;
                SpawnUserPacket(author, message.Channel);
            }
        }

        private void SpawnUserPacket(SocketUser user, ISocketMessageChannel channel)
        {
            var exe = new Executable($"packet u{user.Id}", new Task<Task>(async () =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var botUser = await db.GetUserOrCreateAsync(user.Id);
                    if (botUser.IsBlacklisted) return;

                    var pCnt = botUser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Packet);
                    if (pCnt == null)
                    {
                        pCnt = Database.Models.StatusType.Packet.NewTimeStatus();
                        botUser.TimeStatuses.Add(pCnt);
                    }

                    if (!pCnt.IsActive())
                    {
                        pCnt.EndsAt = DateTime.Now.Date.AddDays(1);
                        pCnt.IValue = 0;
                    }

                    if (++pCnt.IValue > 3) return;

                    botUser.GameDeck.BoosterPacks.Add(new BoosterPack
                    {
                        CardCnt = 2,
                        MinRarity = Rarity.E,
                        IsCardFromPackTradable = true,
                        Name = "Pakiet kart za aktywność",
                        CardSourceFromPack = CardSource.Activity
                    });
                    await db.SaveChangesAsync();

                    _ = Task.Run(async () =>
                    {
                        await channel.SendMessageAsync("", embed: $"{user.Mention} otrzymał pakiet losowych kart.".ToEmbedMessage(EMType.Bot).Build());

                        var gUser = user as SocketGuildUser;
                        using (var dba = new Database.AnalyticsContext(_config))
                        {
                            dba.UsersData.Add(new Database.Models.Analytics.UserAnalytics
                            {
                                Value = 1,
                                UserId = user.Id,
                                MeasureDate = DateTime.Now,
                                GuildId = gUser?.Guild?.Id ?? 0,
                                Type = Database.Models.Analytics.UserAnalyticsEventType.Pack
                            });
                            await dba.SaveChangesAsync();
                        }
                    });
                }
            }));

            _executor.TryAdd(exe, TimeSpan.FromSeconds(1));
        }

        private long GetMessageRealLenght(SocketUserMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
                return 1;

            int emoteChars = message.Tags.CountEmotesTextLenght();
            int linkChars = message.Content.CountLinkTextLength();
            int nonWhiteSpaceChars = message.Content.Count(c => c != ' ');
            int quotedChars = message.Content.CountQuotedTextLength();
            long charsThatMatters = nonWhiteSpaceChars - linkChars - emoteChars - quotedChars;
            return charsThatMatters < 1 ? 1 : charsThatMatters;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            var user = msg.Author as SocketGuildUser;
            if (user == null) return;

            if (_config.Get().BlacklistedGuilds.Any(x => x == user.Guild.Id))
                return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config == null) return;

                var noExp = config.ChannelsWithoutExp.Any(x => x.Channel == msg.Channel.Id);
                if (!noExp) HandleUserAsync(msg);

                var sch = user.Guild.GetTextChannel(config.WaifuConfig.SpawnChannel);
                var tch = user.Guild.GetTextChannel(config.WaifuConfig.TrashSpawnChannel);
                if (sch != null && tch != null)
                {
                    string mention = "";
                    var wRole = user.Guild.GetRole(config.WaifuRole);
                    if (wRole != null) mention = wRole.Mention;

                    var muteRole = user.Guild.GetRole(config.MuteRole);

                    HandleGuildAsync(sch, tch, config.SafariLimit, mention, noExp, muteRole);
                }
            }
        }
    }
}