#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        private readonly Emoji InEmote = new Emoji("📥");
        private readonly Emoji ErrEmote = new Emoji("❌");
        private readonly Emoji OutEmote = new Emoji("📤");

        public IEmote[] StartReactions => new IEmote[] { AcceptEmote, DeclineEmote };

        public CraftingSession(IUser owner, IConfig config) : base(owner)
        {
            Event = ExecuteOn.AllEvents;
            RunMode = RunMode.Sync;
            TimeoutMs = 120000;
            _config = config;

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
            var owned = Items.ToItemList();
            var used = P1.Items.ToItemList();

            return $"**Posiadane:**\n{owned}\n**Użyte:**\n{used}\n**Karta:** {GetCardClassFromItems()}";
        }

        private string GetCardClassFromItems()
        {
            //TODO: check card and accept
            return "---";
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

        private async Task HandleAddAsync(int number, long count, SocketUserMessage message)
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

            var thisItem2 = P1.Items.FirstOrDefault(x => x.Type == thisItem.Type);
            if (thisItem2 == null)
            {
                thisItem2 = thisItem.Type.ToItem(count);
                P1.Items.Add(thisItem2);
            }
            else thisItem2.Count += count;

            await message.AddReactionAsync(InEmote);

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
            }
        }

        private async Task HandleDeleteAsync(int number, long count, SocketUserMessage message)
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

            var thisItem2 = Items.FirstOrDefault(x => x.Type == thisItem.Type);
            if (thisItem2 == null)
            {
                thisItem2 = thisItem.Type.ToItem(count);
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
                    if (P1.Accepted)
                    {
                        //TODO: save changes
                    }
                    else
                    {
                        await msg.ModifyAsync(x => x.Embed = $"{Name}\n\nBrakuje przedmiotów, tworzenie karty nie powiodło się.".ToEmbedMessage(EMType.Bot).Build());
                    }

                    QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                    return true;
                }

                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, context.User);
                }
                catch (Exception) { }
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