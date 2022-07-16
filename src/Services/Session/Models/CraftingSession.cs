#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.PocketWaifu;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.Session.Models
{
    public class CraftingSession : Session
    {
        public IMessage Message { get; set; }
        public List<Item> Items { get; set; }
        public PlayerInfo P1 { get; set; }
        public string Name { get; set; }
        public string Tips { get; set; }

        private IConfig _config;
        private Waifu _waifu;

        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        private readonly Emoji InEmote = new Emoji("📥");
        private readonly Emoji ErrEmote = new Emoji("❌");
        private readonly Emoji OutEmote = new Emoji("📤");

        public IEmote[] StartReactions => new IEmote[] { AcceptEmote, DeclineEmote };

        public CraftingSession(IUser owner, Waifu waifu, IConfig config) : base(owner)
        {
            Event = ExecuteOn.AllEvents;
            RunMode = RunMode.Sync;
            TimeoutMs = 120000;
            _config = config;
            _waifu = waifu;

            Message = null;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            if (P1 == null || Message == null)
                return true;

            await HandleMessageAsync(context);
            return await HandleReactionAsync(context);
        }

        public Embed BuildEmbed()
        {
            return new EmbedBuilder
            {
                Color = EMType.Bot.Color(),
                Description = $"{Name}\n\n{GetCraftingView()}\n\n{Tips}"
            }.Build();
        }

        private string GetCraftingView()
        {
            var used = P1.Items.ToItemListString();
            return $"**Posiadane:**\nLista poszła na PW!\n**Użyte:**\n{used}\n**Karta:** {GetCardClassFromItems()}";
        }

        private string GetCardClassFromItems()
        {
            var value = GetValue();
            if (value > 1000)
            {
                P1.Accepted = true;
                return GetRarityFromValue(value).ToString();
            }
            else P1.Accepted = false;

            return "---";
        }

        private long GetValue() => P1.Items.Sum(x => x.Type.CValue() * x.Count);

        private Rarity GetRarityFromValue(long value)
        {
            if (value > 100000) return Rarity.SS;
            if (value > 10000) return Rarity.S;
            if (value > 8000) return Rarity.A;
            if (value > 6000) return Rarity.B;
            if (value > 4000) return Rarity.C;
            if (value > 2000) return Rarity.D;
            return Rarity.E;
        }

        private async Task HandleMessageAsync(SessionContext context)
        {
            if (context.Message.Id == Message.Id)
                return;

            if (context.Message.Channel.Id != Message.Channel.Id)
                return;

            var cmd = context.Message?.Content?.ToLower();
            if (cmd == null) return;

            var splitedCmd = cmd.Replace("\n", " ").Split(" ");
            if (splitedCmd.Length < 2) return;

            var cmdType = splitedCmd[0];
            if (cmdType == null) return;

            int itemNum = -1;
            var itemNr = splitedCmd[1];
            if (!string.IsNullOrEmpty(itemNr))
            {
                if (int.TryParse(itemNr, out var num))
                {
                    itemNum = num;
                }
            }

            int itemCount = 1;
            if (splitedCmd.Length > 2)
            {
                var itemCnt = splitedCmd[2];
                if (!string.IsNullOrEmpty(itemCnt))
                {
                    if (int.TryParse(itemCnt, out var count))
                    {
                        itemCount = count;
                    }
                }
            }

            if (itemNum < 1)
            {
                await context.Message.AddReactionAsync(ErrEmote);
                return;
            }

            if (cmdType.Contains("usuń") || cmdType.Contains("usun"))
            {
                await HandleDeleteAsync(itemNum -1, itemCount, context.Message);
                RestartTimer();
            }
            else if (cmdType.Contains("dodaj"))
            {
                await HandleAddAsync(itemNum -1, itemCount, context.Message);
                RestartTimer();
            }
        }

        private async Task HandleAddAsync(int number, long count, IUserMessage message)
        {
            if (number >= Items.Count)
            {
                await message.AddReactionAsync(ErrEmote);
                return;
            }

            var thisItem = Items[number];
            if (thisItem.Count <= count)
            {
                count = thisItem.Count;
                Items.Remove(thisItem);
            }
            else thisItem.Count -= count;

            var thisItem2 = P1.Items.FirstOrDefault(x => x.Type == thisItem.Type && x.Quality == thisItem.Quality);
            if (thisItem2 == null)
            {
                thisItem2 = thisItem.Type.ToItem(count, thisItem.Quality);
                P1.Items.Add(thisItem2);
            }
            else thisItem2.Count += count;

            await message.AddReactionAsync(InEmote);

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
            }
        }

        private async Task HandleDeleteAsync(int number, long count, IUserMessage message)
        {
            if (number >= P1.Items.Count)
            {
                await message.AddReactionAsync(ErrEmote);
                return;
            }

            var thisItem = P1.Items[number];
            if (thisItem.Count <= count)
            {
                count = thisItem.Count;
                P1.Items.Remove(thisItem);
            }
            else thisItem.Count -= count;

            var thisItem2 = Items.FirstOrDefault(x => x.Type == thisItem.Type && x.Quality == thisItem.Quality);
            if (thisItem2 == null)
            {
                thisItem2 = thisItem.Type.ToItem(count, thisItem.Quality);
                Items.Add(thisItem2);
            }
            else thisItem2.Count += count;

            await message.AddReactionAsync(OutEmote);

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
            }
        }

        private async Task<bool> HandleReactionAsync(SessionContext context)
        {
            bool end = false;
            if (context.Message.Id != Message.Id)
                return false;

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                var reaction = context.ReactionAdded ?? context.ReactionRemoved;
                if (reaction == null) return false;

                if (reaction.Emote.Equals(DeclineEmote))
                {
                    await msg.ModifyAsync(x => x.Embed = $"{Name}\n\nOdrzucono tworzenie karty.".ToEmbedMessage(EMType.Bot).Build());

                    QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                    return true;
                }

                if (reaction.Emote.Equals(AcceptEmote))
                {
                    bool error = true;

                    if (P1.Accepted)
                    {
                        error = false;
                        using (var db = new Database.UserContext(_config))
                        {
                            var character = await _waifu.GetRandomCharacterAsync();
                            if (character == null)
                            {
                                await msg.ModifyAsync(x => x.Embed = $"{Name}\n\nBrak połączenia z shindenem!".ToEmbedMessage(EMType.Error).Build());
                                return true;
                            }

                            var user = await db.GetUserOrCreateAsync(P1.User.Id);
                            var newCard = _waifu.GenerateNewCard(P1.User, character, GetRarityFromValue(GetValue()));

                            var wwc = await db.WishlistCountData.AsQueryable().FirstOrDefaultAsync(x => x.Id == newCard.Character);
                            newCard.WhoWantsCount = wwc?.Count ?? 0;

                            newCard.Source = CardSource.Crafting;
                            newCard.Affection = user.GameDeck.AffectionFromKarma();

                            foreach (var item in P1.Items)
                            {
                                var thisItem = user.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type && x.Quality == item.Quality);
                                if (thisItem == null)
                                {
                                    error = true;
                                    break;
                                }

                                if (thisItem.Count < item.Count)
                                {
                                    error = true;
                                    break;
                                }
                                thisItem.Count -= item.Count;
                                if (thisItem.Count < 1) user.GameDeck.Items.Remove(thisItem);
                            }

                            if (!error)
                            {
                                user.GameDeck.Cards.Add(newCard);

                                await db.SaveChangesAsync();

                                await msg.ModifyAsync(x => x.Embed = $"{Name}\n\n**Utworzono:** {newCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
                            }
                        }
                    }

                    if (error) await msg.ModifyAsync(x => x.Embed = $"{Name}\n\nBrakuje przedmiotów, tworzenie karty nie powiodło się.".ToEmbedMessage(EMType.Bot).Build());

                    QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                    return true;
                }
            }

            return end;
        }

        private async Task DisposeAction()
        {
            if (Message != null)
            {
                if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
                {
                    try
                    {
                        await msg.RemoveAllReactionsAsync();
                    }
                    catch (Exception) { }
                }

                Message = null;
                Name = null;
                Tips = null;
                P1 = null;
            }
        }
    }
}