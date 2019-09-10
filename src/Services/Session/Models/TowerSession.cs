#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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

        public Embed BuildEmbed(string additionalNote = "")
        {
            return new EmbedBuilder
            {
                Color = EMType.Error.Color(),
                Description = $"{GetName()}\n\n**Ty: **{PlayingCard.GetTowerBaseStats()}\n\n{GetContent()}\n\n{additionalNote}"
            }.Build();
        }

        private async Task UpdateOriginalMsgAsync(Embed embed = null, string additionalNote = "")
        {
            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = embed ?? BuildEmbed(additionalNote));
            }
        }

        private string GetName() => $"🗼 **Wieża wyzwań - piętro {PlayingCard.Profile.CurrentRoom.FloorId}**:\n*Pokój: {PlayingCard.Profile.CurrentRoom.Type} [{PlayingCard.Profile.CurrentRoomId}]*";

        private string GetContent()
        {
            if (IsCurrentRoomCleared()) return GetConqueredRoomContent();
            return GetNotConqueredRoomContent();
        }

        private bool IsCurrentRoomCleared()
        {
            bool cnq = IsCurrentRoomConquered();
            switch (PlayingCard.Profile.CurrentRoom.Type)
            {
                case RoomType.BossBattle:
                case RoomType.Fight:
                    return PlayingCard.Profile.Enemies?.Count < 1 && cnq;

                case RoomType.Event:
                    return PlayingCard.Profile.CurrentEvent == null && cnq;

                default:
                    return cnq;
            }
        }

        private bool IsCurrentRoomConquered() => IsRoomConquered(PlayingCard.Profile.CurrentRoomId);

        private bool IsRoomConquered(ulong id)
        {
            var conqueredRooms = PlayingCard.Profile.ConqueredRoomsFromFloor.Split(";").Select(x => ulong.Parse(x));
            return conqueredRooms.Any(x => x == id);
        }

        private string GetNotConqueredRoomContent()
        {
            return PlayingCard.Profile.CurrentRoom.GetRoomContent();
        }

        private string GetConqueredRoomContent()
        {
            var routes = new List<string>();
            var rooms = new List<Room>();

            if (PlayingCard.Profile.CurrentRoom.ConnectedRooms?.Count > 0)
                rooms.AddRange(PlayingCard.Profile.CurrentRoom.ConnectedRooms.Select(x => x.ConnectedRoom));

            if (PlayingCard.Profile.CurrentRoom.RetConnectedRooms?.Count > 0)
                rooms.AddRange(PlayingCard.Profile.CurrentRoom.RetConnectedRooms.Select(x => x.MainRoom));

            for (int i = 0; i < rooms.Count; i++)
            {
                var number = $"{i+1}[{rooms[i].Id}]";
                var prefix = IsRoomConquered(rooms[i].Id) ? "!" : "";
                if (rooms[i].ItemType == ItemInRoomType.ToOpen)
                {
                    var hasItem = PlayingCard.Profile.Items.Any(x => x.ItemId == rooms[i].ItemId);
                    if (!hasItem) prefix = "?";
                    if (rooms[i].IsHidden)
                    {
                        if (hasItem)
                        {
                            routes.Add($"`{prefix}{number}`");
                            continue;
                        }
                    }
                }
                routes.Add($"`{prefix}{number}`");
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

            if (cmdType.Contains("idź") || cmdType.Contains("idz") && IsCurrentRoomCleared())
            {
                if (splitedCmd.Length >= 2 && PlayingCard.Profile.ActionPoints > 0)
                {
                    if (uint.TryParse(splitedCmd[1], out var roomNr))
                    {
                        if (await MoveToRoomAsync((int) roomNr - 1))
                        {
                            await context.Message.DeleteAsync();
                            await UpdateOriginalMsgAsync();
                            return;
                        }
                    }
                }
                RestartTimer();
                await context.Message.AddReactionAsync(ErrEmote);
            }

            //TODO: command handler, fight, use spell, use item, choose answer of event...
        }

        private async Task<bool> MoveToRoomAsync(int roomNr)
        {
            var rooms = new List<Room>();

            if (PlayingCard.Profile.CurrentRoom.ConnectedRooms?.Count > 0)
                rooms.AddRange(PlayingCard.Profile.CurrentRoom.ConnectedRooms.Select(x => x.ConnectedRoom));

            if (PlayingCard.Profile.CurrentRoom.RetConnectedRooms?.Count > 0)
                rooms.AddRange(PlayingCard.Profile.CurrentRoom.RetConnectedRooms.Select(x => x.MainRoom));

            if (roomNr >= rooms.Count) return false;

            var room = rooms[roomNr];
            if (room.ItemType == ItemInRoomType.ToOpen)
            {
                if (!PlayingCard.Profile.Items.Any(x => x.ItemId == room.ItemId))
                    return false;
            }

            using (var db = new Database.UserContext(_config))
            {
                var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);

                thisCard.Profile.CurrentRoomId = room.Id;
                thisCard.Profile.ActionPoints -= 1;

                switch (room.Type)
                {
                    case RoomType.BossBattle:
                        thisCard.Profile.Enemies.Add(room.Floor.GetBossOfFloor());

                        if (room.Count > 0)
                            foreach (var en in room.GetTowerNewEnemies())
                                thisCard.Profile.Enemies.Add(en);
                    break;

                    case RoomType.Event:
                        thisCard.Profile.CurrentEvent = room.GetTowerEvent(db.TEvent);
                    break;

                    case RoomType.Treasure:
                    case RoomType.Fight:
                    break;

                    default:
                        if (room.ItemType == ItemInRoomType.Loot)
                        {
                            var thisItem = thisCard.Profile.Items.FirstOrDefault(x => x.ItemId == room.ItemId);
                            if (thisItem == null)
                            {
                                thisItem = new ItemInProfile
                                {
                                    Count = room.Count,
                                    Item = room.Item,
                                };
                                thisCard.Profile.Items.Add(thisItem);
                            }
                            else thisItem.Count += room.Count;
                        }
                    break;
                }

                await db.SaveChangesAsync();
                QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
            }
            return true;
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

                if (!reaction.Emote.Equals(DeclineEmote) && !reaction.Emote.Equals(AcceptEmote))
                    return false;

                if (IsCurrentRoomConquered()) return false;

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
                        using (var db = new Database.UserContext(_config))
                        {
                            var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                            var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);

                            if (accepted) thisCard.RecoverFromRest(true);
                            else thisCard.Profile.ActionPoints += 1;
                            thisCard.MarkCurrentRoomAsConquered();

                            await db.SaveChangesAsync();
                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                            PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
                            await UpdateOriginalMsgAsync(null, accepted ? "Odzyskano część punktów życia." : "Otrzymano dodatkowy punkt akcji.");
                        }
                    }
                    break;

                    case RoomType.Treasure:
                    {
                        using (var db = new Database.UserContext(_config))
                        {
                            var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                            var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);

                            string message = "Skrzynia okazała się mimikiem, tracisz punkty życia.";
                            thisCard.MarkCurrentRoomAsConquered();
                            if (accepted)
                            {
                                if (thisCard.CheckLuck(100))
                                {
                                    thisCard.Profile.Health -= 10 + (int) (10 * (thisCard.Profile.CurrentRoom.FloorId / 15));
                                    if (thisCard.Profile.Health < 1)
                                    {
                                        message += "\nUmarłeś.";
                                        //TODO: restart level
                                    }
                                }
                                else
                                {
                                    //TODO: generate or randomize items
                                }
                            }

                            await db.SaveChangesAsync();
                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                            PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
                            await UpdateOriginalMsgAsync(null, accepted ? message : "");
                        }
                    }
                    break;

                    case RoomType.Empty:
                    {
                        using (var db = new Database.UserContext(_config))
                        {
                            var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                            var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);

                            thisCard.MarkCurrentRoomAsConquered();
                            if (accepted)
                            {
                                if (thisCard.Profile.ActionPoints > 0)
                                {
                                    thisCard.RecoverFromRest();
                                    thisCard.Profile.ActionPoints -= 1;
                                }
                                else
                                {
                                    await UpdateOriginalMsgAsync(null, "Brakuje Ci punktów akcji aby tutaj odpocząć.");
                                    break;
                                }
                            }

                            await db.SaveChangesAsync();
                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                            PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
                            await UpdateOriginalMsgAsync(null, accepted ? "Odzyskano trochę punktów życia." : "");
                        }
                    }
                    break;

                    case RoomType.Fight:
                    {
                        if (PlayingCard.Profile.Enemies?.Count > 0)
                            return false;

                        using (var db = new Database.UserContext(_config))
                        {
                            var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                            var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);

                            thisCard.MarkCurrentRoomAsConquered();
                            var ext = thisCard.CheckLuck(100);

                            if (accepted || !ext)
                            {
                                foreach (var enemy in thisCard.Profile.CurrentRoom.GetTowerNewEnemies())
                                    thisCard.Profile.Enemies.Add(enemy);
                            }

                            await db.SaveChangesAsync();
                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                            PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
                            await UpdateOriginalMsgAsync(null, (!accepted && ext) ? "Udało Ci się uciec." : "Nie udało Ci się uciec.");
                            //TODO: list enemies
                        }
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