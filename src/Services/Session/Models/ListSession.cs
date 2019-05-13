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

            int index = 1;
            string pageString = "";
            foreach (var itm in itemsOnPage)
                pageString += $"**{index + (page * ItemsPerPage)}**: {itm.ToString()}\n";

            Embed.Description = pageString.TrimToLength(1800);

            return Embed.Build();
        }

        private int MaxPage() => (((ListItems.Count % 10) == 0) ? (ListItems.Count / 10) : ((ListItems.Count / 10) + 1));

        private int MaxPageReal() => MaxPage() - 1;

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
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

                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, GetOwner());
                }
                catch (Exception _) { }
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
                    catch (Exception _)
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