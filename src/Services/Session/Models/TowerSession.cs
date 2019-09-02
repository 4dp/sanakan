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
using Sanakan.Database.Models.Tower;
using Sanakan.Extensions;
using Sanakan.Services.PocketWaifu;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.Session.Models
{
    public class TowerSession : Session
    {
        public IMessage Message { get; set; }
        public Card PlayingCard { get; set; }
        public PlayerInfo P1 { get; set; }

        private IConfig _config;

        private readonly Emoji ErrEmote = new Emoji("❌");
        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        public IEmote[] StartReactions => new IEmote[] { AcceptEmote, DeclineEmote };

        public TowerSession(IUser owner, IConfig config) : base(owner)
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
                Color = EMType.Error.Color(),
                Description = $"{GetName()}\n\n{GetContent()}"
            }.Build();
        }

        private string GetName() => $"🗼 **Wieża wyzwań - piętro {PlayingCard.Profile.CurrentRoom.FloorId}**:";

        private string GetContent()
        {
            var conqueredRooms = PlayingCard.Profile.ConqueredRoomsFromFloor.Split(";").Select(x => ulong.Parse(x));
            if (conqueredRooms.Any(x => x == PlayingCard.Profile.CurrentRoomId)) return GetConqueredRoomContent();
            return GetNotConqueredRoomContent();
        }

        private string GetNotConqueredRoomContent()
        {
            return PlayingCard.Profile.CurrentRoom.GetRoomContent();
        }

        private string GetConqueredRoomContent()
        {
            int path = 1;
            var routes = new List<string>();
            foreach (var cn in PlayingCard.Profile.CurrentRoom.ConnectedRooms)
            {
                if (cn.ConnectedRoom.ItemType == ItemInRoomType.ToOpen)
                {
                    var hasItem = PlayingCard.Profile.Items.Any(x => x.ItemId == cn.ConnectedRoom.ItemId);
                    if (cn.ConnectedRoom.IsHidden)
                    {
                        if (hasItem) routes.Add($"`{path++}`");
                    }
                    else routes.Add(hasItem ? $"`{path++}`" : $"`?-{path++}`");
                }
                else routes.Add($"`{path++}`");
            }

            return $"**Dostępne przejścia**:\n{string.Join(",", routes)}";
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
            var cmdType = splitedCmd[0];
            if (cmdType == null) return;

            if (cmdType.Contains("idź") || cmdType.Contains("idz"))
            {
                if (splitedCmd.Length >= 2)
                {
                    if (uint.TryParse(splitedCmd[1], out var roomNr))
                    {

                        RestartTimer();
                        await context.Message.DeleteAsync();
                        return;
                    }
                }
                await context.Message.AddReactionAsync(ErrEmote);
            }

            //TODO: command handler, move to room, fight, use spell...
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

                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, context.User);
                }
                catch (Exception) { }

                if (!reaction.Emote.Equals(DeclineEmote) || !reaction.Emote.Equals(AcceptEmote))
                    return false;

                bool accepted = reaction.Emote.Equals(AcceptEmote);
                switch (PlayingCard.Profile.CurrentRoom.Type)
                {
                    default:
                    case RoomType.BossBattle:
                    case RoomType.Event:
                    case RoomType.Start:
                        return false;

                    case RoomType.Campfire:
                    {
                        //TODO: yes/no recover
                    }
                    break;

                    case RoomType.Empty:
                    {
                        //TODO: yes/no recover
                    }
                    break;

                    case RoomType.Fight:
                    {
                        //TODO: yes/no fight
                    }
                    break;
                }

                RestartTimer();
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
                P1 = null;
            }
        }
    }
}