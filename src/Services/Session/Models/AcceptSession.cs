#pragma warning disable 1591

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Sanakan.Services.Session.Models
{
    public class AcceptSession : Session
    {
        public IMessage Message { get; set; }
        public IAcceptActions Actions { get; set; }

        private readonly IUser Bot;
        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emoji DeclineEmote = new Emoji("❎");

        public AcceptSession(IUser owner, IUser bot) : base(owner)
        {
            Event = ExecuteOn.AllReactions;
            RunMode = RunMode.Sync;
            TimeoutMs = 120000;
            Bot = bot;

            Message = null;
            Actions = null;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            if (context.Message.Id != Message.Id)
                return false;

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                var reaction = context.ReactionAdded ?? context.ReactionRemoved;
                if (reaction.Emote.Equals(AcceptEmote))
                {
                    return await Actions?.OnAccept(context);
                }
                else if (reaction.Emote.Equals(DeclineEmote))
                {
                    return await Actions?.OnDecline(context);
                }

                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, GetOwner());
                }
                catch (Exception) { }
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
                        await msg.RemoveReactionsAsync(Bot, new IEmote[] { AcceptEmote, DeclineEmote });
                    }
                }

                Message = null;
            }

            Actions = null;
        }
    }
}