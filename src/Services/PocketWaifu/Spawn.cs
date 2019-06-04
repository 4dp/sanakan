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

        private void HandleGuildAsync(ITextChannel spawnChannel, ITextChannel trashChannel, long daily)
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

            if (daily > 0 && ServerCounter[spawnChannel.GuildId] >= daily) return;
            if (!_config.Get().SafariEnabled) return;
            if (!Fun.TakeATry(55)) return;

            ServerCounter[spawnChannel.GuildId] += 1;
            _ = Task.Run(async () =>
            {
                await SpawnCardAsync(spawnChannel, trashChannel);
            });
        }

        private void RunSafari(EmbedBuilder embed, IUserMessage msg, Card newCard,
            SafariImage pokeImage, ICharacterInfo character, ITextChannel trashChannel)
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

                            var selected = Fun.GetOneRandomFrom(users);
                            var dUser = await db.GetCachedFullUserAsync(selected.Id);

                            if (dUser != null)
                            {
                                if (!dUser.IsBlacklisted)
                                    winner = selected;
                            }
                            else users.Remove(selected);
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
            return new Executable("safari", new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var botUser = db.GetUserOrCreateAsync(winner.Id).Result;
                    botUser.GameDeck.Cards.Add(newCard);
                    db.SaveChanges();
                }

                _ = Task.Run(async () =>
                {
                    embed.ImageUrl = await _waifu.GetSafariViewAsync(pokeImage, character, newCard, trashChannel);
                    embed.Description = $"{winner.Mention} zdobył na polowaniu i wsadził do klatki:\n"
                                       + $"{newCard.GetString(false, false, true)}\n({newCard.Title})";
                    await msg.ModifyAsync(x => x.Embed = embed.Build());

                    try
                    {
                        var privEmb = new EmbedBuilder()
                        {
                            Color = EMType.Info.Color(),
                            Description = $"Na [polowaniu]({msg.GetJumpUrl()}) zdobyłeś: {newCard.GetString(false, false, true)}"
                        };

                        var priv = await winner.GetOrCreateDMChannelAsync();
                        if (priv != null) await priv.SendMessageAsync("", false, privEmb.Build());
                    }
                    catch (Exception) { }
                });
            }));
        }

        private async Task SpawnCardAsync(ITextChannel spawnChannel, ITextChannel trashChannel)
        {
            var character = await _waifu.GetRandomCharacterAsync();
            if (character == null)
            {
                _logger.Log("In Satafi: bad shinden connection");
                return;
            }

            var newCard = _waifu.GenerateNewCard(character);
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

            var msg = await spawnChannel.SendMessageAsync("", embed: embed.Build());
            RunSafari(embed, msg, newCard, pokeImage, character, trashChannel);
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

            UserCounter[author.Id] += GetMessageRealLenght(message);
            if (UserCounter[author.Id] > 3500)
            {
                UserCounter[author.Id] = 0;
                _ = Task.Run(async () =>
                {
                    if (!Fun.TakeATry(6))
                    {
                        SpawnUserPacket(author);
                        await message.Channel.SendMessageAsync("", embed: $"{author.Mention} otrzymał pakiet losowych kart."
                            .ToEmbedMessage(EMType.Bot).Build());
                    }
                });
            }
        }

        private void SpawnUserPacket(SocketUser user)
        {
            var exe = new Executable($"packet u{user.Id}", new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    var botUser = db.GetUserOrCreateAsync(user.Id).Result;
                    if (botUser.IsBlacklisted) return;

                    botUser.GameDeck.BoosterPacks.Add(new BoosterPack
                    {
                        CardCnt = 2,
                        MinRarity = Rarity.E,
                        IsCardFromPackTradable = true,
                        Name = "Pakiet kart za aktywność",
                        CardSourceFromPack = CardSource.Activity
                    });
                    db.SaveChanges();
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
            return nonWhiteSpaceChars - linkChars - emoteChars;
        }

        private async Task HandleMessageAsync(SocketMessage message)
        {
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.IsBot || msg.Author.IsWebhook) return;

            var user = msg.Author as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.GuildConfigContext(_config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(user.Guild.Id);
                if (config == null) return;

                if (config.ChannelsWithoutExp.Any(x => x.Channel == msg.Channel.Id))
                    return;

                HandleUserAsync(msg);

                var sch = user.Guild.GetTextChannel(config.WaifuConfig.SpawnChannel);
                var tch = user.Guild.GetTextChannel(config.WaifuConfig.TrashSpawnChannel);
                if (sch == null || tch == null) return;

                HandleGuildAsync(sch, tch, config.SafariLimit);
            }
        }
    }
}