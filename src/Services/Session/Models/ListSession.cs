#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Sanakan.Extensions;

namespace Sanakan.Services.Session.Models
{
    public class ListSession<T> : Session
    {
        public bool Enumerable { get; set; }
        public IMessage Message { get; set; }
        public int ItemsPerPage { get; set; }
        public List<T> ListItems { get; set; }
        public EmbedBuilder Embed { get; set; }

        private int CurrentPage { get; set; }

        private readonly IUser Bot;
        private readonly Emoji LeftEmote = new Emoji("⬅");
        private readonly Emoji RightEmote = new Emoji("➡");

        public ListSession(IUser owner, IUser bot) : base(owner)
        {
            Event = ExecuteOn.AllReactions;
            RunMode = RunMode.Async;
            Enumerable = true;
            TimeoutMs = 60000;
            Bot = bot;

            Embed = null;
            Message = null;
            CurrentPage = 0;
            ListItems = null;
            ItemsPerPage = 10;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        public Embed BuildPage(int page)
        {
            int firstItem = page * ItemsPerPage;
            int lastItem = (ItemsPerPage - 1) + (page * ItemsPerPage);
            bool toMuch = lastItem >= ListItems.Count;

            Embed.Footer = new EmbedFooterBuilder().WithText($"{CurrentPage + 1} z {MaxPage()}");
            var itemsOnPage = ListItems.GetRange(firstItem, toMuch ? (ListItems.Count - firstItem) : ItemsPerPage);

            string pageString = "";
            for (int i = 0; i < itemsOnPage.Count; i++)
            {
                string enumerable = Enumerable ? $"**{(i + 1) + (page * ItemsPerPage)}**: " : "";
                pageString += $"{enumerable}{itemsOnPage[i].ToString()}\n";
            }

            Embed.Description = pageString.TrimToLength(1800);

            return Embed.Build();
        }

        private int MaxPage() => (((ListItems.Count % 10) == 0) ? (ListItems.Count / 10) : ((ListItems.Count / 10) + 1));

        private int MaxPageReal() => MaxPage() - 1;

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            if (context.Message.Id != Message.Id)
                return false;

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                var reaction = context.ReactionAdded ?? context.ReactionRemoved;
                if (reaction.Emote.Equals(LeftEmote))
                {
                    if (--CurrentPage < 0) CurrentPage = MaxPageReal();
                    await msg.ModifyAsync(x => x.Embed = BuildPage(CurrentPage));

                    RestartTimer();
                }
                else if (reaction.Emote.Equals(RightEmote))
                {
                    if (++CurrentPage > MaxPageReal()) CurrentPage = 0;
                    await msg.ModifyAsync(x => x.Embed = BuildPage(CurrentPage));

                    RestartTimer();
                }
            }

            return false;
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
                    catch (Exception)
                    {
                        await msg.RemoveReactionsAsync(Bot, new IEmote[] { LeftEmote, RightEmote });
                    }
                }

                Message = null;
            }

            ListItems = null;
        }
    }
}