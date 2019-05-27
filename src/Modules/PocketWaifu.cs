#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;
using Sden = Shinden;

namespace Sanakan.Modules
{
    [Name("PocketWaifu"), RequireUserRole]
    public class PocketWaifu : SanakanModuleBase<SocketCommandContext>
    {
        private Database.GuildConfigContext _dbGuildConfigContext;
        private Database.UserContext _dbUserContext;
        private Sden.ShindenClient _shclient;
        private SessionManager _session;
        private IConfig _config;
        private Waifu _waifu;

        public PocketWaifu(Waifu waifu, Sden.ShindenClient client, Database.UserContext userContext,
            Database.GuildConfigContext dbGuildConfigContext, SessionManager session, IConfig config)
        {
            _waifu = waifu;
            _config = config;
            _shclient = client;
            _session = session;
            _dbUserContext = userContext;
            _dbGuildConfigContext = dbGuildConfigContext;
        }

        [Command("harem", RunMode = RunMode.Async)]
        [Alias("cards", "karty")]
        [Summary("wyświetla wszystkie posaidane karty")]
        [Remarks("klatka"), RequireWaifuCommandChannel]
        public async Task ShowCardsAsync([Summary("typ sortowania(klatka/jakość/atak/obrona/relacja)")]HaremType type = HaremType.Rarity)
        {
            var session = new ListSession<Card>(Context.User, Context.Client.CurrentUser);
            await _session.KillSessionIfExistAsync(session);

            var user = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
            if (user?.GameDeck?.Cards?.Count() < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych kart.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            session.Enumerable = false;
            session.ListItems = _waifu.GetListInRightOrder(user.GameDeck.Cards, type);
            session.Embed = new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Title = "Harem"
            };

            try
            {
                var dm = await Context.User.GetOrCreateDMChannelAsync();
                var msg = await dm.SendMessageAsync("", embed: session.BuildPage(0));
                await msg.AddReactionsAsync( new [] { new Emoji("⬅"), new Emoji("➡") });

                session.Message = msg;
                await _session.TryAddSession(session);

                await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
            }
            catch (Exception)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
            }
        }

        [Command("przedmioty", RunMode = RunMode.Async)]
        [Alias("items")]
        [Summary("wypisuje posiadane przedmioty(informacje o przedmiocie gdy podany jego numer)")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ShowItemsAsync([Summary("nr przedmiotu")]int numberOfItem = 0)
        {
            var bUser = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
            if (bUser.GameDeck.Items.Count < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych przemiotów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (numberOfItem <= 0)
            {
                await ReplyAsync("", embed: _waifu.GetItemList(Context.User, bUser.GameDeck.Items.ToList()));
                return;
            }

            if (bUser.GameDeck.Items.Count < numberOfItem)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var item = bUser.GameDeck.Items.ToArray()[numberOfItem - 1];
            var embed = new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Author = new EmbedAuthorBuilder().WithUser(Context.User),
                Description = $"**{item.Name}**\n_{item.Type.Desc()}_\n\nLiczba: **{item.Count}**".TrimToLength(1900)
            };

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("karta", RunMode = RunMode.Async)]
        [Alias("card")]
        [Summary("pozwala wyświetlić kartę")]
        [Remarks("685"), RequireWaifuCommandChannel]
        public async Task ShowCardAsync([Summary("WID")]ulong wid)
        {
            var card  = (await _dbUserContext.Cards.Include(x => x.GameDeck).Include(x => x.ArenaStats).FromCacheAsync( new[] { "users" })).FirstOrDefault(x => x.Id == wid);
            if (card == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            SocketUser user = Context.Guild.GetUser(card.GameDeck.UserId);
            if (user == null) user = Context.Client.GetUser(card.GameDeck.UserId);

            var gConfig = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
            var trashChannel = Context.Guild.GetTextChannel(gConfig.WaifuConfig.TrashCommandsChannel);

            await ReplyAsync("", embed: await _waifu.BuildCardViewAsync(card, trashChannel, user));
        }

        [Command("sklepik")]
        [Alias("shop", "p2w")]
        [Summary("listowanie/zakup przedmiotu/wypisanie informacji")]
        [Remarks("1 info"), RequireWaifuCommandChannel]
        public async Task BuyItemAsync([Summary("nr przedmiotu")]int itemNumber = 0, [Summary("info/4(jako liczba przedmiotów do zakupu/lub id)")]string info = "0")
        {
            var itemsToBuy = _waifu.GetItemsWithCost();
            if (itemNumber <= 0)
            {
                await ReplyAsync("", embed: _waifu.GetShopView(itemsToBuy));
                return;
            }

            if (itemNumber > itemsToBuy.Length)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} w sklepiku nie ma takiego przedmiotu.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var thisItem = itemsToBuy[--itemNumber];
            if (info == "info")
            {
                await ReplyAsync("", embed: _waifu.GetItemShopInfo(thisItem));
                return;
            }

            int itemCount = 0;
            if (!int.TryParse(info, out itemCount))
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} liczbe poproszę, a nie jakieś bohomazy.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            ulong boosterPackTitleId = 0;
            string boosterPackTitleName = "";

            switch (thisItem.Item.Type)
            {
                case ItemType.RandomTitleBoosterPackSingleE:
                    if (itemCount < 0) itemCount = 0;
                    var response = await _shclient.Title.GetInfoAsync((ulong)itemCount);
                    if (!response.IsSuccessStatusCode())
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono tytułu o podanym id.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    boosterPackTitleName = $" ({response.Body.Title})";
                    boosterPackTitleId = response.Body.Id;
                    itemCount = 1;
                    break;

                default:
                    if (itemCount < 1) itemCount = 1;
                    break;
            }

            var realCost = itemCount * thisItem.Cost;
            string count = (itemCount > 1) ? $" x{itemCount}" : "";

            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            if (bUser.TcCnt < realCost)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby TC!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (thisItem.Item.Type.IsBoosterPack())
            {
                for (int i = 0; i < itemCount; i++)
                {
                    var booster = thisItem.Item.Type.ToBoosterPack();
                    if (boosterPackTitleId != 0)
                    {
                        booster.Title = boosterPackTitleId;
                        booster.Name += boosterPackTitleName;
                    }
                    if (booster != null) bUser.GameDeck.BoosterPacks.Add(booster);
                }

                bUser.Stats.WastedTcOnCards += realCost;
            }
            else
            {
                var inUserItem = bUser.GameDeck.Items.FirstOrDefault(x => x.Type == thisItem.Item.Type);
                if (inUserItem == null)
                {
                    inUserItem = thisItem.Item.Type.ToItem(itemCount);
                    bUser.GameDeck.Items.Add(inUserItem);
                }
                else inUserItem.Count += itemCount;

                bUser.Stats.WastedTcOnCookies += realCost;
            }

            bUser.TcCnt -= realCost;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

            await ReplyAsync("", embed: $"{Context.User.Mention} zakupił: _{thisItem.Item.Name}{boosterPackTitleName}{count}_.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("użyj")]
        [Alias("uzyj", "use")]
        [Summary("używa przedmiot na karcie")]
        [Remarks("1 4212 2"), RequireWaifuCommandChannel]
        public async Task UseItemAsync([Summary("nr przedmiotu")]int itemNumber, [Summary("WID")]ulong wid, [Summary("liczba przedmiotów")]int itemCnt = 1)
        {
            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);

            if (bUser.GameDeck.Items.Count < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych pakietów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (itemNumber <= 0 || itemNumber > bUser.GameDeck.Items.Count)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var item = bUser.GameDeck.Items.ToArray()[itemNumber - 1];
            if (item.Count < itemCnt)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz tylu sztuk tego przedmiotu.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            switch (item.Type)
            {
                case ItemType.AffectionRecoveryBig:
                case ItemType.AffectionRecoverySmall:
                case ItemType.AffectionRecoveryNormal:
                    break;

                default:
                    if (itemCnt != 1)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} możesz użyć tylko jeden przedmiot tego typu na raz!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    break;
            }

            var card = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
            if (card == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            double affectionInc = 0;
            string cnt = (itemCnt > 1) ? $"x{itemCnt}" : "";
            var embed = new EmbedBuilder
            {
                Color = EMType.Bot.Color(),
                Author = new EmbedAuthorBuilder().WithUser(Context.User),
                Description = $"Użyto _{item.Name}_ {cnt} na {card.GetString(false, false, true)}\n\n"
            };

            switch (item.Type)
            {
                case ItemType.AffectionRecoveryBig:
                    affectionInc = 1 * itemCnt;
                    embed.Description += "Znacznie powiekszyła się relacja z kartą!";
                    break;

                case ItemType.AffectionRecoveryNormal:
                    affectionInc = 0.1 * itemCnt;
                    embed.Description += "Powiekszyła się relacja z kartą!";
                    break;

                case ItemType.AffectionRecoverySmall:
                    affectionInc = 0.025 * itemCnt;
                    embed.Description += "Powiekszyła się trochę relacja z kartą!";
                    break;

                case ItemType.IncreaseUpgradeCnt:
                    if (card.GetAffectionString() != "Miłość")
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} karta musi mieć poziom relacji: *Miłość*.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    if (card.Rarity == Rarity.SSS)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} karty **SSS** nie można już ulepszyć!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                    affectionInc = 0.7;
                    embed.Description += $"Zwiększono liczbę ulepszeń do {++card.UpgradesCnt}!";
                    break;

                case ItemType.DereReRoll:
                    affectionInc = 0.1;
                    card.Dere = _waifu.RandomizeDere();
                    embed.Description += $"Nowy charakter to: {card.Dere}!";
                    break;

                case ItemType.CardParamsReRoll:
                    affectionInc = 0.2;
                    card.Attack = _waifu.RandomizeAttack(card.Rarity);
                    card.Defence = _waifu.RandomizeDefence(card.Rarity);
                    embed.Description += $"Nowa moc karty to: 🔥{card.Attack} 🛡{card.Defence}!";
                    break;

                default:
                    await ReplyAsync("", embed: $"{Context.User.Mention} tego przedmiotu nie powinno tutaj być!".ToEmbedMessage(EMType.Error).Build());
                    return;
            }

            if (card.Character == bUser.GameDeck.Waifu)
                affectionInc *= 1.5;

            var response = await _shclient.GetCharacterInfoAsync(card.Character);
            if (response.IsSuccessStatusCode())
            {
                if (response.Body?.Points != null)
                {
                    var ordered = response.Body.Points.OrderByDescending(x => x.Points);
                    if (ordered.Any(x => x.Name == embed.Author.Name))
                        affectionInc += 1.2;
                }
            }

            if (card.Dere == Dere.Tsundere)
                affectionInc *= 2;

            item.Count -= itemCnt;
            card.Affection += affectionInc;

            if (item.Count <= 0)
                bUser.GameDeck.Items.Remove(item);
            
            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("pakiet")]
        [Alias("pakiet kart", "booster", "booster pack", "pack")]
        [Summary("wypisuje dostępne pakiety/otwiera pakiet")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task UpgradeCardAsync([Summary("nr pakietu kart")]int numberOfPack = 0)
        {
            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);

            if (bUser.GameDeck.BoosterPacks.Count < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych pakietów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (numberOfPack == 0)
            {
                await ReplyAsync("", embed: _waifu.GetBoosterPackList(Context.User, bUser.GameDeck.BoosterPacks.ToList()));
                return;
            }

            if (bUser.GameDeck.BoosterPacks.Count < numberOfPack || numberOfPack <= 0)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu pakietów.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var pack = bUser.GameDeck.BoosterPacks.ToArray()[numberOfPack - 1];
            var cards = await _waifu.OpenBoosterPackAsync(pack);
            if (cards.Count < pack.CardCnt)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się otworzyć pakietu.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            bUser.GameDeck.BoosterPacks.Remove(pack);
            
            foreach (var card in cards)
                bUser.GameDeck.Cards.Add(card);

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

            string openString = "";
            foreach (var card in cards)
                openString += $"{card.GetString(false, false, true)}\n";

            await ReplyAsync("", embed: $"{Context.User.Mention} z pakietu **{pack.Name}** wypadło:\n\n{openString.TrimToLength(1900)}".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("ulepsz")]
        [Alias("upgrade")]
        [Summary("ulepsza kartę na lepszą jakość (min. 30 exp)")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task UpgradeCardAsync([Summary("WID")]ulong id)
        {
            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var card = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == id);

            if (card == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (card.Rarity == Rarity.SSS)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma już najwyższy poziom.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.UpgradesCnt < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie ma już dostępnych ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.ExpCnt < card.ExpToUpgrade())
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma niewystarczającą ilość punktów doświadczenia.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.UpgradesCnt < 10 && card.Rarity == Rarity.SS)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą ilość ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.GetAffectionString() != "Obsesyjna miłość" && card.Rarity == Rarity.SS)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą relacje aby ją ulepszyć.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            ++bUser.Stats.UpgaredCards;

            card.Defence = _waifu.GetDefenceAfterLevelUp(card.Rarity, card.Defence);
            card.Attack = _waifu.GetAttactAfterLevelUp(card.Rarity, card.Attack);
            card.UpgradesCnt -= (card.Rarity == Rarity.SS ? 10 : 1);
            card.Rarity = --card.Rarity;
            card.Affection += 1;
            card.ExpCnt = 0;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę do: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("poświęć")]
        [Alias("kill", "sacrifice", "poswiec", "poświec", "poświeć", "poswięć", "poswieć")]
        [Summary("dodaje exp do karty, poświęcając inną")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task SacraficeCardAsync([Summary("WID(do poświęcenia)")]ulong idToSac, [Summary("WID(do ulepszenia)")]ulong idToUp)
        {
            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var cardToSac = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == idToSac);
            var cardToUp = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == idToUp);

            if (cardToSac == null || cardToUp == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            ++bUser.Stats.SacraficeCards;

            var exp = _waifu.GetExpToUpgrade(cardToUp, cardToSac);
            cardToUp.Affection += 0.01;
            cardToUp.ExpCnt += exp;

            bUser.GameDeck.Cards.Remove(cardToSac);

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę: {cardToUp.GetString(false, false, true)} o {exp.ToString("F")} exp.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("klatka")]
        [Alias("cage")]
        [Summary("otwiera klatkę z kartami(sprecyzowanie wid wyciąga tylko jedną kartę)")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task OpenCageAsync([Summary("WID(opcjonalne)")]ulong wid = 0)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            var bUser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            var cardsInCage = bUser.GameDeck.Cards.Where(x => x.InCage);

            var cntIn = cardsInCage.Count();
            if (cntIn < 1)
            {
                await ReplyAsync("", embed: $"{user.Mention} nie posiadasz kart w klatce.".ToEmbedMessage(EMType.Info).Build());
                return;
            }

            if (wid == 0)
            {
                foreach (var card in cardsInCage)
                {
                    card.InCage = false;
                    var response = await _shclient.GetCharacterInfoAsync(card.Id);
                    if (response.IsSuccessStatusCode())
                    {
                        if (response.Body?.Points != null)
                        {
                            if (response.Body.Points.Any(x => x.Name.Equals(user.Nickname ?? user.Username)))
                                card.Affection += 0.8;
                        }
                    }

                    var span = DateTime.Now - card.CreationDate;
                    if (span.TotalDays > 5) card.Affection -= (int)span.TotalDays * 0.1;
                }
            }
            else
            {
                var thisCard = cardsInCage.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{user.Mention} taka karta nie znajduje się w twojej klatce.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                thisCard.InCage = false;
                cntIn = 1;

                var span = DateTime.Now - thisCard.CreationDate;
                if (span.TotalDays > 5) thisCard.Affection -= (int)span.TotalDays * 0.1;

                foreach (var card in cardsInCage)
                {
                    if (card.Id != thisCard.Id)
                        card.Affection -= 0.3;
                }
            }
            
            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            await ReplyAsync("", embed: $"{user.Mention} wyciągnął {cntIn} kart z klatki.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("talia")]
        [Alias("deck", "aktywne")]
        [Summary("wyświetla aktywne karty/ustawia karte jako aktywną")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ChangeDeckCardStatusAsync([Summary("WID(opcjonalne)")]ulong wid = 0)
        {
            var botUser = await _dbUserContext.GetCachedFullUserAsync(Context.User.Id);
            var active = botUser.GameDeck.Cards.Where(x => x.Active);

            if (wid == 0)
            {
                if (active.Count() < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aktywnych kart.".ToEmbedMessage(EMType.Info).Build());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    await dm.SendMessageAsync("", embed: _waifu.GetActiveList(active));
                    await dm.CloseAsync();

                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }

                return;
            }

            if (active.Count() >= 3 && !active.Any(x => x.Id == wid))
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} możesz mieć tylko trzy aktywne karty.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var thisCard = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);

            if (thisCard == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (thisCard.InCage)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta znajduje się w klatce.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            thisCard.Active = !thisCard.Active;
            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            string message = thisCard.Active ? "aktywował: " : "dezaktywował: ";
            await ReplyAsync("", embed: $"{Context.User.Mention} {message}{thisCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("kto", RunMode = RunMode.Async)]
        [Alias("who")]
        [Summary("pozwala wyszukac użytkowników posiadających karte danej postaci")]
        [Remarks("51"), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsAsync([Summary("id postaci na shinden")]ulong id)
        {
            var response = await _shclient.GetCharacterInfoAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: $"Nie odnaleziono postaci na shindenie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }
            
            var cards = await _dbUserContext.Cards.Include(x => x.GameDeck).Where(x => x.Character == id).FromCacheAsync( new[] {"users"});

            if (cards.Count() < 1)
            {
                await ReplyAsync("", embed: $"Nie odnaleziono kart {response.Body}.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            await ReplyAsync("", embed: _waifu.GetWaifuFromCharacterSearchResult($"[**{response.Body}**]({response.Body.CharacterUrl}) posiadają:", cards, Context.Guild));
        }

        [Command("wymiana")]
        [Alias("exchange")]
        [Summary("propozycja wymiany z użytkownikiem")]
        [Remarks("Karna"), RequireWaifuMarketChannel]
        public async Task ExchangeCardsAsync([Summary("użytkownik")]SocketGuildUser user2)
        {
            var user1 = Context.User as SocketGuildUser;
            if (user1 == null) return;

            if (user1.Id == user2.Id)
            {
                await ReplyAsync("", embed: $"{user1.Mention} wymiana z samym sobą?".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var session = new ExchangeSession(user1, user2, _config);
            if (_session.SessionExist(session))
            {
                await ReplyAsync("", embed: $"{user1.Mention} Ty lub twój partner znajdujecie się obecnie w trakcie wymiany.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var duser1 = await _dbUserContext.GetCachedFullUserAsync(user1.Id);
            var duser2 = await _dbUserContext.GetCachedFullUserAsync(user2.Id);
            if (duser1 == null || duser2 == null)
            {
                await ReplyAsync("", embed: "Jeden z graczy nie posiada profilu!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            session.P1 = new PlayerInfo
            {
                User = user1,
                Dbuser = duser1,
                Accepted = false,
                CustomString = "",
                Cards = new List<Card>()
            };

            session.P2 = new PlayerInfo
            {
                User = user2,
                Dbuser = duser2,
                Accepted = false,
                CustomString = "",
                Cards = new List<Card>()
            };

            session.Name = "🔄 **Wymiana:**";
            session.Tips = $"Polecenia: `dodaj [WID]`, `usuń [WID]`.\n\n\u0031\u20E3 "
                + $"- zakończenie dodawania {user1.Mention}\n\u0032\u20E3 - zakończenie dodawania {user2.Mention}";

            var msg = await ReplyAsync("", embed: session.BuildEmbed());
            await msg.AddReactionsAsync(session.StartReactions);
            session.Message = msg;

            await _session.TryAddSession(session);
        }

        [Command("arena")]
        [Alias("wild")]
        [Summary("walka z losowo wygenerowaną kartą")]
        [Remarks("1"), RequireWaifuFightChannel]
        public async Task FightInArenaAsync([Summary("nr aktywnej karty(1-3)")]int activeCard)
        {
            var botUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            var active = botUser.GameDeck.Cards.Where(x => x.Active).ToList();
            if (active.Count < activeCard || activeCard <= 0)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie masz tylu aktywnych kart!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var thisCard = active[--activeCard];
            if (thisCard.IsUnusable())
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} masz zbyt niską relację z tą kartą, aby mogła walczyć na arenie.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var enemyCharacter = await _waifu.GetRandomCharacterAsync();
            var playerCharacter = (await _shclient.GetCharacterInfoAsync(thisCard.Character)).Body;
            if (enemyCharacter == null || playerCharacter == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się pobrać informacji z shindena.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var enemyCard = _waifu.GenerateNewCard(enemyCharacter);
            var embed = new EmbedBuilder
            {
                Color = EMType.Bot.Color(),
                Author = new EmbedAuthorBuilder().WithUser(Context.User)
            };

            var p1 = new CardInfo
            {
                Card = thisCard,
                Info = playerCharacter
            };

            var p2 = new CardInfo
            {
                Card = enemyCard,
                Info = enemyCharacter
            };

            var result = _waifu.GetFightWinner(p1, p2);
            thisCard.Affection -= playerCharacter.HasImage ? 0.05 : 0.2;
            var dInfo = new DuelInfo();

            switch (result)
            {
                case FightWinner.Card1:
                    ++thisCard.ArenaStats.Wins;
                    var exp = _waifu.GetExpToUpgrade(thisCard, enemyCard, true);
                    embed.Description = $"+{exp.ToString("F")} exp\n";
                    thisCard.ExpCnt += exp;

                    dInfo.Side = DuelInfo.WinnerSide.Left;
                    dInfo.Winner = p1;
                    dInfo.Loser = p2;

                    if (Services.Fun.TakeATry(6))
                    {
                        var item = _waifu.RandomizeItemFromFight().ToItem();
                        var thisItem = botUser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type);
                        if (thisItem == null)
                        {
                            thisItem = item;
                            botUser.GameDeck.Items.Add(thisItem);
                        }
                        else ++thisItem.Count;

                        embed.Description += $"+{item.Name}";
                    }
                    break;

                case FightWinner.Card2:
                    thisCard.Affection -= playerCharacter.HasImage ? 0.1 : 0.5;
                    ++thisCard.ArenaStats.Loses;

                    dInfo.Side = DuelInfo.WinnerSide.Right;
                    dInfo.Winner = p2;
                    dInfo.Loser = p1;
                    break;

                default:
                case FightWinner.Draw:
                    ++thisCard.ArenaStats.Draws;

                    dInfo.Side = DuelInfo.WinnerSide.Draw;
                    dInfo.Winner = p1;
                    dInfo.Loser = p2;
                    break;
            }

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}" });

            var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
            embed.ImageUrl = await _waifu.GetArenaViewAsync(dInfo, Context.Guild.GetTextChannel(config.WaifuConfig.TrashFightChannel));

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("pojedynek")]
        [Alias("duel")]
        [Summary("stajesz do walki na przeciw innemu graczowi")]
        [Remarks("Karna"), RequireWaifuFightChannel]
        public async Task MakeADuelAsync([Summary("użytkownik")]SocketGuildUser user2)
        {
            var user1 = Context.User as SocketGuildUser;
            if (user1 == null) return;

            if (user1.Id == user2.Id)
            {
                await ReplyAsync("", embed: $"{user1.Mention} walka na siebie samego?".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var session = new AcceptSession(user2, user1, Context.Client.CurrentUser);
            if (_session.SessionExist(session))
            {
                await ReplyAsync("", embed: $"{user1.Mention} Ty lub twój partner znajdujecie się obecnie w trakcie walki.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var duser1 = await _dbUserContext.GetCachedFullUserAsync(user1.Id);
            var duser2 = await _dbUserContext.GetCachedFullUserAsync(user2.Id);

            var active1 = duser1?.GameDeck?.Cards?.Where(x => x.Active).ToList();
            if (active1.Count < 1)
            {
                await ReplyAsync("", embed: $"{user1.Mention} nie ma aktywnych kart.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var active2 = duser2?.GameDeck?.Cards?.Where(x => x.Active).ToList();
            if (active2.Count < 1)
            {
                await ReplyAsync("", embed: $"{user2.Mention} nie ma aktywnych kart.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            string Name = $"⚔ Pojedynek:\n\n{user1.Mention} wyzywa {user2.Mention}\n\n";
            var msg = await ReplyAsync("", embed: $"{Name}{user2.Mention} przyjmujesz to wyzwanie?".ToEmbedMessage(EMType.Error).Build());
            await msg.AddReactionsAsync(session.StartReactions);

            session.Message = msg;
            session.Actions = new AcceptDuel(_waifu, _config)
            {
                Message = msg,
                DuelName = Name,
                P1 = new PlayerInfo
                {
                    User = user1,
                    Cards = active1
                },
                P2 = new PlayerInfo
                {
                    User = user2,
                    Cards = active2
                }
            };

            await _session.TryAddSession(session);
        }

        [Command("waifu")]
        [Alias("husbando")]
        [Summary("pozwala ustawić sobie ulubioną postać na profilu(musisz posiadać jej karte)")]
        [Remarks("451"), RequireWaifuCommandChannel]
        public async Task SetProfileWaifuAsync([Summary("WID")]ulong wid)
        {
            var bUser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
            if (wid == 0)
            {
                if (bUser.GameDeck.Waifu != 0)
                {
                    bUser.GameDeck.Waifu = 0;
                    await _dbUserContext.SaveChangesAsync();
                }

                await ReplyAsync("", embed: $"{Context.User.Mention} zresetował ulubioną karte.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            var thisCard = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid && !x.InCage);
            if (thisCard == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty lub znajduje się ona w klatce!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var prev = bUser.GameDeck.Cards.FirstOrDefault(x => x.Character == bUser.GameDeck.Waifu);
            if (prev != null)
            {
                var allPrevWaifus = bUser.GameDeck.Cards.Where(x => x.Id == prev.Id);
                foreach (var card in allPrevWaifus) card.Affection -= 1;
            }

            bUser.GameDeck.Waifu = thisCard.Character;
            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ustawił {thisCard.Name} jako ulubioną postać.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("karcianka", RunMode = RunMode.Async)]
        [Alias("cpf")]
        [Summary("wyświetla profil PocketWaifu")]
        [Remarks("Karna"), RequireWaifuCommandChannel]
        public async Task ShowProfileAsync([Summary("użytkownik")]SocketGuildUser usr = null)
        {
            var user = (usr ?? Context.User) as SocketGuildUser;
            if (user == null) return;

            var bUser = await _dbUserContext.GetCachedFullUserAsync(user.Id);
            if (bUser == null)
            {
                await ReplyAsync("", embed: $"{user.Mention} nie ma konta w bocie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var ssCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.SS);
            var sCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.S);
            var aCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.A);
            var bCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.B);
            var cCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.C);
            var dCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.D);
            var eCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.E);

            var a1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.Versus);
            var w1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.Versus);

            var abr = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.BattleRoyale);
            var wbr = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.BattleRoyale);

            var embed = new EmbedBuilder()
            {
                Color = EMType.Bot.Color(),
                Author = new EmbedAuthorBuilder().WithUser(user),
                Description = $"**Posiadane karty**: {bUser.GameDeck.Cards.Count}\n"
                            + $"**SS**: {ssCnt} **S**: {sCnt} **A**: {aCnt} **B**: {bCnt} **C**: {cCnt} **D**: {dCnt} **E**:{eCnt}\n\n"
                            + $"**1vs1** Rozegrane: {a1vs1ac} Wygrane: {w1vs1ac}\n"
                            + $"**GMwK** Rozegrane: {abr} Wygrane: {wbr}"
            };

            if (bUser.GameDeck?.Waifu != 0)
            {
                var tChar = bUser.GameDeck.Cards.FirstOrDefault(x => x.Character == bUser.GameDeck.Waifu);
                if (tChar != null)
                {
                    var response = await _shclient.GetCharacterInfoAsync(tChar.Character);
                    if (response.IsSuccessStatusCode())
                    {
                        var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                        var channel = Context.Guild.GetTextChannel(config.WaifuConfig.TrashCommandsChannel);

                        embed.WithImageUrl(await _waifu.GetWaifuProfileImageAsync(tChar, response.Body, channel));
                        string wfi = (response.Body.Gender == Sden.Models.Sex.Male) ? "Husbando" : "Waifu";
                        embed.WithFooter(new EmbedFooterBuilder().WithText($"{wfi}: {response.Body}"));
                    }
                }
            }

            await ReplyAsync("", embed: embed.Build());
        }
    }
}