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

        private Waifu _waifu;
        private IConfig _config;

        private readonly Emoji ErrEmote = new Emoji("❌");
        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        private List<string> Nicknames = new List<string>();
        public IEmote[] StartReactions => new IEmote[] { AcceptEmote, DeclineEmote };

        public TowerSession(IUser owner, IConfig config, Waifu waifu) : base(owner)
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

            if (Nicknames.Count() < 1)
                Nicknames = context.Client.Guilds.SelectMany(x => x.Users).Select(x => x.Nickname ?? x.Username).ToList();

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
            if (IsCurrentRoomConquered()) return GetNotClearedRoomContent();
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
            return PlayingCard.Profile.CurrentRoom.GetRoomContent(PlayingCard.GetTowerEnemiesString());
        }

        private string GetNotClearedRoomContent()
        {
            switch (PlayingCard.Profile.CurrentRoom.Type)
            {
                case RoomType.BossBattle:
                case RoomType.Fight:
                    return PlayingCard.GetTowerEnemiesString();

                case RoomType.Event:
                    return PlayingCard.GetTowerEventString();

                default:
                    return GetNotConqueredRoomContent();
            }
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

            if ((cmdType.Contains("idź") || cmdType.Contains("idz")) && IsCurrentRoomCleared())
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
            else if ((cmdType.Contains("atakuj") || cmdType.Contains("atak")) && PlayingCard.Profile.Enemies?.Count > 0)
            {
                if (splitedCmd.Length >= 2 && PlayingCard.Profile.ActionPoints > 0)
                {
                    if (ulong.TryParse(splitedCmd[1], out var enemyId))
                    {
                        var enemy = PlayingCard.Profile.Enemies.FirstOrDefault(x => x.Id == enemyId);
                        if (enemy != null)
                        {
                            await context.Message.DeleteAsync();
                            await UpdateOriginalMsgAsync(null, await AttackEnemy(enemyId));
                            return;
                        }
                    }
                    RestartTimer();
                    await context.Message.AddReactionAsync(ErrEmote);
                }
            }
            else if ((cmdType.Contains("czar") || cmdType.Contains("spell")))
            {
                if (splitedCmd.Length >= 2 && PlayingCard.Profile.ActionPoints > 0 && PlayingCard.Profile.Energy > 0)
                {
                    if (ulong.TryParse(splitedCmd[1], out var spellId))
                    {
                        var spell = PlayingCard.Profile.Spells.FirstOrDefault(x => x.Id == spellId);
                        if (spell != null)
                        {
                            if (PlayingCard.Profile.Energy >= spell.Spell.EnergyCost)
                            {
                                switch (spell.Spell.Target)
                                {
                                    case SpellTarget.Ally:
                                    case SpellTarget.Self:
                                    case SpellTarget.AllyGroup:
                                    {
                                        await context.Message.DeleteAsync();
                                        await UpdateOriginalMsgAsync(null, await UseSpellOnSelf(spellId));
                                        return;
                                    }

                                    case SpellTarget.Enemy:
                                    {
                                        if (splitedCmd.Length >= 3)
                                        {
                                            if (ulong.TryParse(splitedCmd[2], out var enemyId))
                                            {
                                                var enemy = PlayingCard.Profile.Enemies.FirstOrDefault(x => x.Id == enemyId);
                                                if (enemy != null)
                                                {
                                                    await context.Message.DeleteAsync();
                                                    await UpdateOriginalMsgAsync(null, await AttackEnemy(enemyId, spell.Spell));
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                    break;

                                    case SpellTarget.EnemyGroup:
                                    {
                                        await context.Message.DeleteAsync();
                                        await UpdateOriginalMsgAsync(null, await AttackEnemy(0, spell.Spell));
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                RestartTimer();
                await context.Message.AddReactionAsync(ErrEmote);
            }

            //TODO: command handler - use item, choose answer of event...
        }

        private async Task<string> UseSpellOnSelf(ulong spellId)
        {
            string msg = "";
            using (var db = new Database.UserContext(_config))
            {
                var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);
                var thisSpell = thisCard.Profile.Spells.FirstOrDefault(x => x.Id == spellId);

                thisCard.Profile.ActionPoints -= 1;
                thisCard.Profile.Energy -= thisSpell.Spell.EnergyCost;
                if (thisSpell.UsesCount < 100000) thisSpell.UsesCount++;

                msg += thisCard.InflictEffect(thisSpell.Spell.Effect) + "\n";

                msg += InflictChangesFromActiveEffects(thisCard, thisCard.Profile.Enemies.Count < 1);
                if (thisCard.Profile.Enemies.Count > 0)
                    msg += MakeEnemiesMove(thisCard);

                await db.SaveChangesAsync();
                QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
            }
            return msg;
        }

        private async Task<string> AttackEnemy(ulong enemyId, Spell customDmg = null)
        {
            string msg = "";
            using (var db = new Database.UserContext(_config))
            {
                var thisUser = await db.GetUserOrCreateAsync(P1.User.Id);
                var thisCard = thisUser.GameDeck.Cards.FirstOrDefault(x => x.Id == PlayingCard.Id);
                var enemies = (enemyId == 0) ? thisCard.Profile.Enemies.ToList() : thisCard.Profile.Enemies.Where(x => x.Id == enemyId).ToList();

                thisCard.Profile.ActionPoints -= 1;

                if (customDmg != null)
                {
                    thisCard.Profile.Energy -= customDmg.EnergyCost;
                    var thisSpell = thisCard.Profile.Spells.FirstOrDefault(x => x.Id == customDmg.Id);
                    if (thisSpell.UsesCount < 100000) thisSpell.UsesCount++;
                }

                foreach (var thisEnemy in enemies)
                {
                    int? cDmg = null;
                    bool done = false;
                    if (customDmg != null)
                    {
                        switch (customDmg.Effect.ValueType)
                        {
                            case Database.Models.Tower.ValueType.Percent:
                                cDmg = thisEnemy.Health * customDmg.Effect.Value / 100;
                                break;

                            default:
                                cDmg = customDmg.Effect.Value;
                                break;
                        }

                        switch (customDmg.Effect.Target)
                        {
                            case EffectTarget.Attack:
                            {
                                msg = $"Obniżyłeś atak przeciwnika o {cDmg}! **[{thisEnemy.Id}]**\n";
                                thisEnemy.Attack -= cDmg.Value;
                                done = true;

                                if (thisEnemy.Attack < 1)
                                    thisEnemy.Attack = 1;
                            }
                            break;

                            case EffectTarget.Energy:
                            {
                                msg = $"Obniżyłeś energię przeciwnika o {cDmg}! **[{thisEnemy.Id}]**\n";
                                thisEnemy.Energy -= cDmg.Value;
                                done = true;

                                if (thisEnemy.Energy < 0)
                                    thisEnemy.Energy = 0;
                            }
                            break;

                            case EffectTarget.Defence:
                            {
                                msg = $"Obniżyłeś obronę przeciwnika o {cDmg}! **[{thisEnemy.Id}]**\n";
                                thisEnemy.Defence -= cDmg.Value;
                                done = true;

                                if (thisEnemy.Defence < 0)
                                    thisEnemy.Defence = 0;
                            }
                            break;

                            default:
                            break;
                        }
                    }

                    if (!done)
                    {
                        msg = $"Zadałeś {thisCard.DealDmgToEnemy(thisEnemy, cDmg)} obrażeń! **[{thisEnemy.Id}]**\n";
                        if (thisEnemy.Health < 1)
                        {
                            var exp = thisCard.TowerGrantExpAndLoot(thisEnemy);
                            msg += $"Przeciwnik umiera! (+{exp}exp) **[{thisEnemy.Id}]**\n\n";
                            thisCard.Profile.Enemies.Remove(thisEnemy);
                        }
                    }
                }

                if (thisCard.Profile.Enemies.Count < 1)
                {
                    if (thisCard.Profile.CurrentRoom.ItemType == ItemInRoomType.Loot)
                    {
                        var thisItem = thisCard.Profile.Items.FirstOrDefault(x => x.ItemId == thisCard.Profile.CurrentRoom.ItemId);
                        if (thisItem == null)
                        {
                            thisItem = new ItemInProfile
                            {
                                Count = thisCard.Profile.CurrentRoom.Count,
                                Item = thisCard.Profile.CurrentRoom.Item,
                            };
                            thisCard.Profile.Items.Add(thisItem);
                            msg += $"Otrzymałeś przedmiot: *{thisItem.Item.Name}*\n";
                        }
                        else if (thisItem.Item.UseType == ItemUseType.Usable)
                        {
                            thisItem.Count += thisCard.Profile.CurrentRoom.Count;
                            msg += $"Otrzymałeś przedmiot: *{thisItem.Item.Name}*\n";
                        }
                    }

                    if (thisCard.Profile.CurrentRoom.Type == RoomType.BossBattle)
                    {
                        msg += $"Przechodzisz na następne piętro!\n";
                        if (thisCard.Profile.CurrentRoom.Floor.UserIdFirstToBeat < 1)
                            thisCard.Profile.CurrentRoom.Floor.UserIdFirstToBeat = thisCard.GameDeckId;

                        if (thisUser.GameDeck.MaxTowerFloor < thisCard.Profile.CurrentRoom.FloorId)
                            thisUser.GameDeck.MaxTowerFloor = thisCard.Profile.CurrentRoom.FloorId;

                        var nextFloor = await db.GetOrCreateFloorAsync(thisCard.Profile.CurrentRoom.FloorId + 1);
                        var startRoom = nextFloor.Rooms.FirstOrDefault(x => x.Type == RoomType.Start);

                        thisCard.Profile.ConqueredRoomsFromFloor = $"{startRoom.Id}";
                        thisCard.Profile.CurrentRoom = startRoom;
                    }
                }

                msg += InflictChangesFromActiveEffects(thisCard);
                msg += MakeEnemiesMove(thisCard);

                await db.SaveChangesAsync();
                QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
            }
            return msg;
        }

        private string MakeEnemiesMove(Card card)
        {
            string msg = "";
            foreach (var enemy in card.Profile.Enemies)
            {
                //TODO: check if can use skill
                msg += $"Otrzymałeś {card.ReciveDmgFromEnemy(enemy)} obrażeń! **[{enemy.Id}]**\n";
            }

            if (card.Profile.Health < 1)
            {
                msg += "Umarłeś!";
                card.RestartTowerFloor();
            }

            return msg;
        }

        private string InflictChangesFromActiveEffects(Card card, bool checkDeath = false)
        {
            string msg = "";
            foreach (var effect in card.Profile.ActiveEffects.Where(x => x.Remaining > 0).ToList())
            {
                msg += $"{card.InflictEffect(effect.Effect, true, effect.Multiplier)}\n";
                if (--effect.Remaining < 1)
                    card.Profile.ActiveEffects.Remove(effect);
            }

            if (card.Profile.Health < 1 && checkDeath)
            {
                msg += "Umarłeś!";
                card.RestartTowerFloor();
            }

            return msg;
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

                if (!IsRoomConquered(room.Id))
                {
                    switch (room.Type)
                    {
                        case RoomType.BossBattle:
                            thisCard.Profile.Enemies.Add(room.Floor.GetBossOfFloor());

                            if (room.Count > 0)
                                foreach (var en in room.GetTowerNewEnemies(_waifu, Nicknames))
                                    thisCard.Profile.Enemies.Add(en);
                        break;

                        case RoomType.Event:
                            thisCard.Profile.CurrentEvent = room.GetTowerEvent(db.TEvent);
                            if (thisCard.Profile.CurrentEvent == null) thisCard.MarkCurrentRoomAsConquered();
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
                                else if (thisItem.Item.UseType == ItemUseType.Usable)
                                    thisItem.Count += room.Count;
                            }
                        break;
                    }
                }

                _ = InflictChangesFromActiveEffects(thisCard);

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
                                if (!thisCard.CheckLuck(900))
                                {
                                    thisCard.Profile.Health -= 10 + (int) (10 * (thisCard.Profile.CurrentRoom.FloorId / 15));
                                    if (thisCard.Profile.Health < 1)
                                    {
                                        message += "\nUmarłeś.";
                                        thisCard.RestartTowerFloor();
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
                                foreach (var enemy in thisCard.Profile.CurrentRoom.GetTowerNewEnemies(_waifu, Nicknames))
                                    thisCard.Profile.Enemies.Add(enemy);
                            }

                            await db.SaveChangesAsync();
                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", "users" });

                            PlayingCard = await db.GetCachedFullCardAsync(PlayingCard.Id);
                            await UpdateOriginalMsgAsync(null, (!accepted && ext) ? "Udało Ci się uciec." : (accepted ? "" : $"Nie udało Ci się uciec.\n{thisCard.GetTowerEnemiesString()}"));
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