#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Extensions;
using Sanakan.Services.PocketWaifu;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.Session.Models
{
    public class ExchangeSession : Session
    {
        private enum ExchangeStatus
        {
            Add, AcceptP1, AcceptP2
        }

        public IMessage Message { get; set; }
        public PlayerInfo P1 { get; set; }
        public PlayerInfo P2 { get; set; }
        public string Name { get; set; }
        public string Tips { get; set; }

        private ExchangeStatus State;
        private IConfig _config;

        private readonly Emoji AcceptEmote = new Emoji("✅");
        private readonly Emote DeclineEmote = Emote.Parse("<:redcross:581152766655856660>");

        private readonly Emoji InEmote = new Emoji("📥");
        private readonly Emoji ErrEmote = new Emoji("❌");
        private readonly Emoji OutEmote = new Emoji("📤");

        private readonly Emoji OneEmote = new Emoji("\u0031\u20E3");
        private readonly Emoji TwoEmote = new Emoji("\u0032\u20E3");

        public IEmote[] StartReactions => new IEmote[] { OneEmote, TwoEmote };

        public ExchangeSession(IUser owner, IUser exchanger, IConfig config) : base(owner)
        {
            State = ExchangeStatus.Add;
            Event = ExecuteOn.AllEvents;
            AddParticipant(exchanger);
            RunMode = RunMode.Sync;
            TimeoutMs = 120000;
            _config = config;

            Message = null;

            OnExecute = ExecuteAction;
            OnDispose = DisposeAction;
        }

        private async Task<bool> ExecuteAction(SessionContext context, Session session)
        {
            if (P1 == null || P2 == null || Message == null)
                return true;

            await HandleMessageAsync(context);
            return await HandleReactionAsync(context);
        }

        public Embed BuildEmbed()
        {
            return new EmbedBuilder
            {
                Color = EMType.Warning.Color(),
                Description = $"{Name}\n\n{P1.CustomString}\n\n{P2.CustomString}\n\n{Tips}".TrimToLength(2000)
            }.Build();
        }

        private async Task HandleMessageAsync(SessionContext context)
        {
            if (State != ExchangeStatus.Add)
                return;

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

            PlayerInfo thisPlayer = null;
            PlayerInfo targetPlayer = null;
            if (context.User.Id == P1.User.Id)
            {
                thisPlayer = P1;
                targetPlayer = P2;
            }
            if (context.User.Id == P2.User.Id)
            {
                thisPlayer = P2;
                targetPlayer = P1;
            }
            if (thisPlayer == null) return;

            if (cmdType.Contains("usuń") || cmdType.Contains("usun"))
            {
                var WIDStr = splitedCmd?[1];
                if (string.IsNullOrEmpty(WIDStr))
                {
                    await context.Message.AddReactionAsync(ErrEmote);
                    return;
                }

                if (ulong.TryParse(WIDStr, out var WID))
                {
                    await HandleDeleteAsync(thisPlayer, WID, context.Message);
                }
                RestartTimer();
            }
            else if (cmdType.Contains("dodaj"))
            {
                var ids = new List<ulong>();
                foreach (var WIDStr in splitedCmd)
                    if (ulong.TryParse(WIDStr, out var WID))
                        ids.Add(WID);

                if (ids.Count > 0)
                {
                    await HandleAddAsync(thisPlayer, ids, context.Message, targetPlayer);
                }
                else await context.Message.AddReactionAsync(ErrEmote);
                RestartTimer();
            }
        }

        private async Task HandleAddAsync(PlayerInfo player, List<ulong> wid, IUserMessage message, PlayerInfo target)
        {
            bool error = false;
            bool added = false;

            foreach (var id in wid)
            {
                var card = player.Dbuser.GameDeck.Cards.FirstOrDefault(x => x.Id == id);
                if (card == null)
                {
                    error = true;
                    continue;
                }

                if (card.InCage || !card.IsTradable || card.IsBroken())
                {
                    error = true;
                    continue;
                }

                if (card.Dere == Database.Models.Dere.Yato)
                {
                    error = true;
                    continue;
                }

                if (card.Dere == Database.Models.Dere.Yami && target.Dbuser.GameDeck.IsGood())
                {
                    error = true;
                    continue;
                }

                if (card.Dere == Database.Models.Dere.Raito && target.Dbuser.GameDeck.IsEvil())
                {
                    error = true;
                    continue;
                }

                if (player.Cards.Any(x => x.Id == card.Id))
                    continue;

                player.Cards.Add(card);
                added = true;
            }

            player.Accepted = false;
            player.CustomString = BuildProposition(player);

            if (added) await message.AddReactionAsync(InEmote);
            if (error) await message.AddReactionAsync(ErrEmote);

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
            }
        }

        private async Task HandleDeleteAsync(PlayerInfo player, ulong wid, IUserMessage message)
        {
            var card = player.Cards.FirstOrDefault(x => x.Id == wid);
            if (card == null)
            {
                await message.AddReactionAsync(ErrEmote);
                return;
            }

            if (!player.Cards.Any(x => x.Id == card.Id))
                return;

            player.Accepted = false;
            player.Cards.Remove(card);
            player.CustomString = BuildProposition(player);

            await message.AddReactionAsync(OutEmote);

            if (await Message.Channel.GetMessageAsync(Message.Id) is IUserMessage msg)
            {
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
            }
        }

        public string BuildProposition(PlayerInfo player)
        {
            if (player.Cards.Count > 12)
                return $"{player.User.Mention} oferuje:\n\n**[{player.Cards.Count}]** kart";

            return $"{player.User.Mention} oferuje:\n{string.Join("\n", player.Cards.Select(x => x.GetString(false, false, true)))}";
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

                switch (State)
                {
                    case ExchangeStatus.AcceptP1:
                        end = await HandleUserReactionInAccept(reaction, P1, msg);
                        break;

                    case ExchangeStatus.AcceptP2:
                        end = await HandleUserReactionInAccept(reaction, P2, msg);
                        break;

                    default:
                    case ExchangeStatus.Add:
                        await HandleReactionInAdd(reaction, msg);
                        break;
                }

                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, context.User);
                }
                catch (Exception) { }
            }

            return end;
        }

        private async Task HandleReactionInAdd(SocketReaction reaction, IUserMessage msg)
        {
            if (reaction.Emote.Equals(OneEmote) && reaction.UserId == P1.User.Id)
            {
                P1.Accepted = true;
                RestartTimer();
            }
            else if (reaction.Emote.Equals(TwoEmote) && reaction.UserId == P2.User.Id)
            {
                P2.Accepted = true;
                RestartTimer();
            }

            if (P1.Accepted && P2.Accepted)
            {
                State = ExchangeStatus.AcceptP1;
                Tips = $"{P1.User.Mention} daj {AcceptEmote} aby zaakceptować, lub {DeclineEmote} aby odrzucić.";

                await msg.RemoveAllReactionsAsync();
                await msg.ModifyAsync(x => x.Embed = BuildEmbed());
                await msg.AddReactionsAsync(new IEmote[] { AcceptEmote, DeclineEmote });
            }
        }

        private async Task<bool> HandleUserReactionInAccept(SocketReaction reaction, PlayerInfo player, IUserMessage msg)
        {
            bool end = false;
            if (reaction.UserId == player.User.Id)
            {
                if (reaction.Emote.Equals(AcceptEmote))
                {
                    if (State != ExchangeStatus.AcceptP2)
                    {
                        RestartTimer();
                        State = ExchangeStatus.AcceptP2;
                        Tips = $"{P2.User.Mention} daj {AcceptEmote} aby zaakceptować, lub {DeclineEmote} aby odrzucić.";
                    }
                    else
                    {
                        Tips = $"Wymiana zakończona!";
                        end = true;

                        using (var db = new Database.UserContext(_config))
                        {
                            var user1 = await db.GetUserOrCreateAsync(P1.User.Id);
                            var user2 = await db.GetUserOrCreateAsync(P2.User.Id);

                            double avgValueP1 = P1.Cards.Sum(x => x.MarketValue) / ((P1.Cards.Count == 0) ? 1 : P1.Cards.Count);
                            double avgValueP2 = P2.Cards.Sum(x => x.MarketValue) / ((P2.Cards.Count == 0) ? 1 : P2.Cards.Count);

                            var divP1 = P1.Cards.Count / avgValueP1;
                            var divP2 = P2.Cards.Count / avgValueP2;

                            var exchangeRateP1 = divP2 / ((P1.Cards.Count == 0) ? 0.5 : divP1);
                            var exchangeRateP2 = divP1 / ((P2.Cards.Count == 0) ? 0.5 : divP2);

                            foreach (var c in P1.Cards)
                            {
                                var card = user1.GameDeck.Cards.FirstOrDefault(x => x.Id == c.Id);
                                if (card != null)
                                {
                                    card.Active = false;
                                    card.TagList.Clear();
                                    card.Affection -= 1.5;

                                    var valueDiff = card.MarketValue - exchangeRateP1;
                                    card.MarketValue -= valueDiff * 0.735;

                                    if (card.FirstIdOwner == 0)
                                        card.FirstIdOwner = user1.Id;

                                    user1.GameDeck.Cards.Remove(card);
                                    user2.GameDeck.Cards.Add(card);

                                    user2.GameDeck.RemoveCharacterFromWishList(card.Character);
                                    user2.GameDeck.RemoveCardFromWishList(card.Id);
                                }
                            }

                            foreach (var c in P2.Cards)
                            {
                                var card = user2.GameDeck.Cards.FirstOrDefault(x => x.Id == c.Id);
                                if (card != null)
                                {
                                    card.Active = false;
                                    card.TagList.Clear();
                                    card.Affection -= 1.5;

                                    var valueDiff = card.MarketValue - exchangeRateP2;
                                    card.MarketValue -= valueDiff * 0.735;

                                    if (card.FirstIdOwner == 0)
                                        card.FirstIdOwner = user2.Id;

                                    user2.GameDeck.Cards.Remove(card);
                                    user1.GameDeck.Cards.Add(card);

                                    user1.GameDeck.RemoveCharacterFromWishList(card.Character);
                                    user1.GameDeck.RemoveCardFromWishList(card.Id);
                                }
                            }

                            await db.SaveChangesAsync();

                            QueryCacheManager.ExpireTag(new string[] { $"user-{P1.User.Id}", $"user-{P2.User.Id}", "users" });
                        }
                    }
                }
                else if (reaction.Emote.Equals(DeclineEmote))
                {
                    RestartTimer();
                    Tips = $"{player.User.Mention} odrzucił propozycje wymiany!";
                    end = true;
                }

                if (msg != null) await msg.ModifyAsync(x => x.Embed = BuildEmbed());
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
                P2 = null;
            }
        }
    }
}