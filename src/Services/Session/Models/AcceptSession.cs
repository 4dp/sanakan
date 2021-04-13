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
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        public IEmote[] StartReactions => new IEmote[] { AcceptEmote, DeclineEmote };

        public AcceptSession(IUser owner, IUser challenger, IUser bot) : base(owner)
        {
            Event = ExecuteOn.AllReactions;
            RunMode = RunMode.Sync;
            TimeoutMs = 120000;
            Bot = bot;

            if (challenger != null)
                AddParticipant(challenger);

            Message = null;
            Actions = null;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            if (Message == null || Actions == null)
                return true;

            if (context.Message.Id != Message.Id)
                return false;

            if (context.User.Id != GetOwner().Id)
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