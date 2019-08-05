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
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden.Logger;
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
        private Sden.ShindenClient _shclient;
        private SessionManager _session;
        private IExecutor _executor;
        private ILogger _logger;
        private IConfig _config;
        private Waifu _waifu;

        public PocketWaifu(Waifu waifu, Sden.ShindenClient client, ILogger logger,
            SessionManager session, IConfig config, IExecutor executor)
        {
            _waifu = waifu;
            _logger = logger;
            _config = config;
            _shclient = client;
            _session = session;
            _executor = executor;
        }

        [Command("harem", RunMode = RunMode.Async)]
        [Alias("cards", "karty")]
        [Summary("wyświetla wszystkie posaidane karty")]
        [Remarks("tag konie"), RequireWaifuCommandChannel]
        public async Task ShowCardsAsync([Summary("typ sortowania(klatka/jakość/atak/obrona/relacja/życie/tag)")]HaremType type = HaremType.Rarity, [Summary("tag)")][Remainder]string tag = null)
        {
            var session = new ListSession<Card>(Context.User, Context.Client.CurrentUser);
            await _session.KillSessionIfExistAsync(session);

            if (type == HaremType.Tag && tag == null)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} musisz sprecyzować tag!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var user = await db.GetCachedFullUserAsync(Context.User.Id);
                if (user?.GameDeck?.Cards?.Count() < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                session.Enumerable = false;
                session.ListItems = _waifu.GetListInRightOrder(user.GameDeck.Cards, type, tag);
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
        }

        [Command("przedmioty", RunMode = RunMode.Async)]
        [Alias("items")]
        [Summary("wypisuje posiadane przedmioty(informacje o przedmiocie gdy podany jego numer)")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ShowItemsAsync([Summary("nr przedmiotu")]int numberOfItem = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(Context.User.Id);
                var itemList = bUser.GameDeck.Items.OrderBy(x => x.Type).ToList();

                if (itemList.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych przemiotów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (numberOfItem <= 0)
                {
                    await ReplyAsync("", embed: _waifu.GetItemList(Context.User, itemList));
                    return;
                }

                if (bUser.GameDeck.Items.Count < numberOfItem)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var item = itemList[numberOfItem - 1];
                var embed = new EmbedBuilder
                {
                    Color = EMType.Info.Color(),
                    Author = new EmbedAuthorBuilder().WithUser(Context.User),
                    Description = $"**{item.Name}**\n_{item.Type.Desc()}_\n\nLiczba: **{item.Count}**".TrimToLength(1900)
                };

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [Command("karta-", RunMode = RunMode.Async)]
        [Alias("card-")]
        [Summary("pozwala wyświetlić kartę w prostej postaci")]
        [Remarks("685"), RequireWaifuCommandChannel]
        public async Task ShowCardStringAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var card = (await db.Cards.Include(x => x.GameDeck).Include(x => x.ArenaStats).FromCacheAsync(new[] { "users" })).FirstOrDefault(x => x.Id == wid);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                SocketUser user = Context.Guild.GetUser(card.GameDeck.UserId);
                if (user == null) user = Context.Client.GetUser(card.GameDeck.UserId);

                await ReplyAsync("", embed: $"**[{card.Id}]** *({card.Character})*\n{card.GetString(true, true, true)}\n_{card.Title}_\n\n{card.GetAffectionString()}\n{card.ExpCnt.ToString("F")} exp\n\n{card.Tags ?? "---"}\n{card.GetStatusIcons()}".TrimToLength(2000)
                    .ToEmbedMessage(EMType.Info).WithAuthor(new EmbedAuthorBuilder().WithUser(user)).Build());
            }
        }

        [Command("karta", RunMode = RunMode.Async)]
        [Alias("card")]
        [Summary("pozwala wyświetlić kartę")]
        [Remarks("685"), RequireWaifuCommandChannel]
        public async Task ShowCardAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var card  = (await db.Cards.Include(x => x.GameDeck).Include(x => x.ArenaStats).FromCacheAsync( new[] { "users" })).FirstOrDefault(x => x.Id == wid);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                SocketUser user = Context.Guild.GetUser(card.GameDeck.UserId);
                if (user == null) user = Context.Client.GetUser(card.GameDeck.UserId);

                using (var cdb = new Database.GuildConfigContext(Config))
                {
                    var gConfig = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                    var trashChannel = Context.Guild.GetTextChannel(gConfig.WaifuConfig.TrashCommandsChannel);
                    await ReplyAsync("", embed: await _waifu.BuildCardViewAsync(card, trashChannel, user));
                }
            }
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

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
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

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} zakupił: _{thisItem.Item.Name}{boosterPackTitleName}{count}_.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("użyj")]
        [Alias("uzyj", "use")]
        [Summary("używa przedmiot na karcie")]
        [Remarks("1 4212 2"), RequireWaifuCommandChannel]
        public async Task UseItemAsync([Summary("nr przedmiotu")]int itemNumber, [Summary("WID")]ulong wid, [Summary("liczba przedmiotów")]int itemCnt = 1)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var itemList = bUser.GameDeck.Items.OrderBy(x => x.Type).ToList();

                if (itemList.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych pakietów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (itemNumber <= 0 || itemNumber > itemList.Count)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var item = itemList[itemNumber - 1];
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
                    case ItemType.AffectionRecoveryGreat:
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
                    case ItemType.AffectionRecoveryGreat:
                        affectionInc = 1.6 * itemCnt;
                        bUser.GameDeck.Karma += 0.3 * itemCnt;
                        embed.Description += "Bardzo powiekszyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoveryBig:
                        affectionInc = 1 * itemCnt;
                        bUser.GameDeck.Karma += 0.1 * itemCnt;
                        embed.Description += "Znacznie powiekszyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoveryNormal:
                        affectionInc = 0.12 * itemCnt;
                        bUser.GameDeck.Karma += 0.01 * itemCnt;
                        embed.Description += "Powiekszyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoverySmall:
                        affectionInc = 0.03 * itemCnt;
                        bUser.GameDeck.Karma += 0.001 * itemCnt;
                        embed.Description += "Powiekszyła się trochę relacja z kartą!";
                        break;

                    case ItemType.BetterIncreaseUpgradeCnt:
                        if (card.Rarity == Rarity.SSS)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} karty **SSS** nie można już ulepszyć!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (!card.CanGiveBloodOrUpgradeToSSS())
                        {
                            if (card.HasNoNegativeEffectAfterBloodUsage())
                            {
                                if (card.CanGiveRing())
                                {
                                    affectionInc = 1.7;
                                    bUser.GameDeck.Karma += 0.6;
                                    embed.Description += "Bardzo powiekszyła się relacja z kartą!";
                                }
                                else
                                {
                                    affectionInc = 1.2;
                                    bUser.GameDeck.Karma += 0.4;
                                    embed.Color = EMType.Warning.Color();
                                    embed.Description += $"Karta się zmartwiła!";
                                }
                            }
                            else
                            {
                                affectionInc = -5;
                                bUser.GameDeck.Karma -= 0.5;
                                embed.Color = EMType.Error.Color();
                                embed.Description += $"Karta się przeraziła!";
                            }
                        }
                        else
                        {
                            affectionInc = 1.5;
                            card.UpgradesCnt += 2;
                            bUser.GameDeck.Karma += 2;
                            embed.Description += $"Zwiększono liczbę ulepszeń do {card.UpgradesCnt}!";
                        }
                        break;

                    case ItemType.IncreaseUpgradeCnt:
                        if (!card.CanGiveRing())
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} karta musi mieć min. poziom relacji: *Miłość*.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (card.Rarity == Rarity.SSS)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} karty **SSS** nie można już ulepszyć!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        affectionInc = 0.7;
                        bUser.GameDeck.Karma += 1;
                        embed.Description += $"Zwiększono liczbę ulepszeń do {++card.UpgradesCnt}!";
                        break;

                    case ItemType.DereReRoll:
                        affectionInc = 0.1;
                        bUser.GameDeck.Karma += 0.02;
                        card.Dere = _waifu.RandomizeDere();
                        embed.Description += $"Nowy charakter to: {card.Dere}!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.CardParamsReRoll:
                        affectionInc = 0.2;
                        bUser.GameDeck.Karma += 0.03;
                        card.Attack = _waifu.RandomizeAttack(card.Rarity);
                        card.Defence = _waifu.RandomizeDefence(card.Rarity);
                        embed.Description += $"Nowa moc karty to: 🔥{card.GetAttackWithBonus()} 🛡{card.GetDefenceWithBonus()}!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    default:
                        await ReplyAsync("", embed: $"{Context.User.Mention} tego przedmiotu nie powinno tutaj być!".ToEmbedMessage(EMType.Error).Build());
                        return;
                }

                if (card.Character == bUser.GameDeck.Waifu)
                    affectionInc *= 1.2;

                var response = await _shclient.GetCharacterInfoAsync(card.Character);
                if (response.IsSuccessStatusCode())
                {
                    if (response.Body?.Points != null)
                    {
                        var ordered = response.Body.Points.OrderByDescending(x => x.Points);
                        if (ordered.Any(x => x.Name == embed.Author.Name))
                            affectionInc *= 1.2;
                    }
                }

                if (card.Dere == Dere.Tsundere)
                    affectionInc *= 1.2;

                item.Count -= itemCnt;
                card.Affection += affectionInc;

                if (item.Count <= 0)
                    bUser.GameDeck.Items.Remove(item);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [Command("pakiet")]
        [Alias("pakiet kart", "booster", "booster pack", "pack")]
        [Summary("wypisuje dostępne pakiety/otwiera pakiet")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task UpgradeCardAsync([Summary("nr pakietu kart")]int numberOfPack = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);

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

                if (pack.CardSourceFromPack == CardSource.Activity)
                    bUser.Stats.OpenedBoosterPacksActivity += 1;
                else
                    bUser.Stats.OpenedBoosterPacks += 1;

                bUser.GameDeck.BoosterPacks.Remove(pack);

                var sp = new List<string>();
                if (bUser.GameDeck.Wishlist != null)
                    sp = bUser.GameDeck.Wishlist.Split(";").ToList();

                foreach (var card in cards)
                {
                    card.Affection += bUser.GameDeck.AffectionFromKarma();
                    bUser.GameDeck.Cards.Add(card);

                    if (sp.Contains($"p{card.Character}"))
                        sp.Remove($"p{card.Character}");
                }

                if (sp.Count > 0)
                    bUser.GameDeck.Wishlist = string.Join(";", sp);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                string openString = "";
                foreach (var card in cards)
                    openString += $"{card.GetString(false, false, true)}\n";

                await ReplyAsync("", embed: $"{Context.User.Mention} z pakietu **{pack.Name}** wypadło:\n\n{openString.TrimToLength(1900)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("reset")]
        [Alias("restart")]
        [Summary("restartuj kartę SSS na kartę E i dodaje stały bonus")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task ResetCardAsync([Summary("WID")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var card = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == id);

                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.Rarity != Rarity.SSS)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie ma najwyższego poziomu.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                bUser.GameDeck.Karma -= 2;

                card.Defence = _waifu.RandomizeDefence(Rarity.E);
                card.Attack = _waifu.RandomizeAttack(Rarity.E);
                card.Dere = _waifu.RandomizeDere();
                card.Rarity = Rarity.E;
                card.UpgradesCnt = 2;
                card.RestartCnt += 1;
                card.Affection = 0;
                card.ExpCnt = 0;

                await db.SaveChangesAsync();
                _waifu.DeleteCardImageIfExist(card);

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} zrestartował kartę do: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ulepsz")]
        [Alias("upgrade")]
        [Summary("ulepsza kartę na lepszą jakość (min. 30 exp)")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task UpgradeCardAsync([Summary("WID")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
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

                if (card.UpgradesCnt < 5 && card.Rarity == Rarity.SS)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą ilość ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (!card.CanGiveBloodOrUpgradeToSSS() && card.Rarity == Rarity.SS)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą relacje aby ją ulepszyć.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                ++bUser.Stats.UpgaredCards;
                bUser.GameDeck.Karma += 1;

                card.Defence = _waifu.GetDefenceAfterLevelUp(card.Rarity, card.Defence);
                card.Attack = _waifu.GetAttactAfterLevelUp(card.Rarity, card.Attack);
                card.UpgradesCnt -= (card.Rarity == Rarity.SS ? 5 : 1);
                card.Rarity = --card.Rarity;
                card.Affection += 1;
                card.ExpCnt = 0;

                await db.SaveChangesAsync();
                _waifu.DeleteCardImageIfExist(card);

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę do: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("uwolnij")]
        [Alias("release", "pusmje")]
        [Summary("uwalnia posiadaną kartę")]
        [Remarks("5412 5413"), RequireWaifuCommandChannel]
        public async Task ReleaseCardAsync([Summary("WID kart")]params ulong[] ids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardsToSac = bUser.GameDeck.Cards.Where(x => ids.Any(c => c == x.Id)).ToList();

                if (cardsToSac.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takich kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.InCage || (card.Tags != null && card.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        broken.Add(card);
                        continue;
                    }

                    bUser.GameDeck.Karma += 0.7;
                    bUser.Stats.ReleasedCards += 1;
                    bUser.GameDeck.Cards.Remove(card);
                    _waifu.DeleteCardImageIfExist(card);
                }

                string response = $"kartę: {cardsToSac.First().GetString(false, false, true)}";
                if (cardsToSac.Count > 1) response = $" {cardsToSac.Count} kart";

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                if (broken.Count != cardsToSac.Count)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} uwolnił {response}".ToEmbedMessage(EMType.Success).Build());
                }

                if (broken.Count > 0)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się uwolnić {broken.Count} kart, najpewniej znajdują się w klatce lub są oznaczone jako ulubione.".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("zniszcz")]
        [Alias("destroy")]
        [Summary("niszczy posiadaną kartę")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task DestroyCardAsync([Summary("WID kart")]params ulong[] ids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardsToSac = bUser.GameDeck.Cards.Where(x => ids.Any(c => c == x.Id)).ToList();

                if (cardsToSac.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takich kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.InCage || (card.Tags != null && card.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        broken.Add(card);
                        continue;
                    }

                    bUser.GameDeck.Karma -= 1;
                    bUser.Stats.DestroyedCards += 1;
                    bUser.GameDeck.CTCnt += card.GetValue();
                    bUser.GameDeck.Cards.Remove(card);
                    _waifu.DeleteCardImageIfExist(card);
                }

                string response = $"kartę: {cardsToSac.First().GetString(false, false, true)}";
                if (cardsToSac.Count > 1) response = $" {cardsToSac.Count} kart";

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                if (broken.Count != cardsToSac.Count)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} zniszczył {response}".ToEmbedMessage(EMType.Success).Build());
                }

                if (broken.Count > 0)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się zniszczyć {broken.Count} kart, najpewniej znajdują się w klatce lub są oznaczone jako ulubione.".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("karta+")]
        [Alias("free card")]
        [Summary("dostajesz jedną darmową kartę")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task GetFreeCardAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                var freeCard = botuser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Card);
                if (freeCard == null)
                {
                    freeCard = new TimeStatus
                    {
                        Type = StatusType.Card,
                        EndsAt = DateTime.MinValue
                    };
                    botuser.TimeStatuses.Add(freeCard);
                }

                if (freeCard.IsActive())
                {
                    var timeTo = (int)freeCard.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz otrzymać następną darmową kartę dopiero za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                freeCard.EndsAt = DateTime.Now.AddHours(22);

                var card = _waifu.GenerateNewCard(await _waifu.GetRandomCharacterAsync(), new List<Rarity>() { Rarity.SS, Rarity.S, Rarity.A });
                card.Affection += botuser.GameDeck.AffectionFromKarma();
                card.Source = CardSource.Daily;

                var sp = new List<string>();
                if (botuser.GameDeck.Wishlist != null)
                    sp = botuser.GameDeck.Wishlist.Split(";").ToList();

                if (sp.Contains($"p{card.Character}"))
                {
                    sp.Remove($"p{card.Character}");
                    botuser.GameDeck.Wishlist = string.Join(";", sp);
                }

                botuser.GameDeck.Cards.Add(card);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users"});

                await ReplyAsync("", embed: $"{Context.User.Mention} otrzymałeś {card.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("rynek")]
        [Alias("market")]
        [Summary("udajesz się na rynek z wybraną przez Ciebie kartą aby pohandlować")]
        [Remarks("2145"), RequireWaifuCommandChannel]
        public async Task GoToMarketAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.GameDeck.IsMarketDisabled())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} wszyscy na twój widok się rozbiegli, nic dziś nie zdziałasz.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var card = botuser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.IsUnusable())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ktoś kto Cie nienawidzi, nie pomoże Ci w niczym.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var market = botuser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Market);
                if (market == null)
                {
                    market = new TimeStatus
                    {
                        Type = StatusType.Market,
                        EndsAt = DateTime.MinValue
                    };
                    botuser.TimeStatuses.Add(market);
                }

                if (market.IsActive())
                {
                    var timeTo = (int)market.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz udać się ponownie na rynek za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                int nextMarket = 20 - (int)(botuser.GameDeck.Karma / 100);
                if (nextMarket > 22) nextMarket = 22;
                if (nextMarket < 4) nextMarket = 4;

                int itemCnt = 1 + (int)(card.Affection / 15);
                itemCnt += (int)(botuser.GameDeck.Karma / 180);
                if (itemCnt > 10) itemCnt = 10;
                if (itemCnt < 1) itemCnt = 1;

                if (card.CanGiveRing()) ++itemCnt;
                if (botuser.GameDeck.CanCreateAngel()) ++itemCnt;

                market.EndsAt = DateTime.Now.AddHours(nextMarket);
                card.Affection += 0.1;

                string reward = "";
                for (int i = 0; i < itemCnt; i++)
                {
                    var item = _waifu.RandomizeItemFromMarket().ToItem();
                    var thisItem = botuser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type);
                    if (thisItem == null)
                    {
                        thisItem = item;
                        botuser.GameDeck.Items.Add(thisItem);
                    }
                    else ++thisItem.Count;

                    reward += $"+{item.Name}\n";
                }

                if (Services.Fun.TakeATry(3))
                {
                    botuser.GameDeck.CTCnt += 1;
                    reward += "+1CT";
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} udało Ci się zdobyć:\n\n{reward}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("czarny rynek")]
        [Alias("black market")]
        [Summary("udajesz się na czarny rynek z wybraną przez Ciebie kartą, wolałbym nie wiedzieć co tam będziesz robić")]
        [Remarks("2145"), RequireWaifuCommandChannel]
        public async Task GoToBlackMarketAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.GameDeck.IsBlackMarketDisabled())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} halo koleżko, to nie miejsce dla Ciebie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var card = botuser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var market = botuser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Market);
                if (market == null)
                {
                    market = new TimeStatus
                    {
                        Type = StatusType.Market,
                        EndsAt = DateTime.MinValue
                    };
                    botuser.TimeStatuses.Add(market);
                }

                if (market.IsActive())
                {
                    var timeTo = (int)market.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz udać się ponownie na czarny rynek za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                int nextMarket = 20 + (int)(botuser.GameDeck.Karma / 100);
                if (nextMarket > 22) nextMarket = 22;
                if (nextMarket < 4) nextMarket = 4;

                int itemCnt = 1 + (int)(card.Affection / 15);
                itemCnt -= (int)(botuser.GameDeck.Karma / 180);
                if (itemCnt > 10) itemCnt = 10;
                if (itemCnt < 1) itemCnt = 1;

                if (card.CanGiveBloodOrUpgradeToSSS()) ++itemCnt;
                if (botuser.GameDeck.CanCreateDemon()) ++itemCnt;

                market.EndsAt = DateTime.Now.AddHours(nextMarket);
                card.Affection -= 0.2;

                string reward = "";
                for (int i = 0; i < itemCnt; i++)
                {
                    var item = _waifu.RandomizeItemFromBlackMarket().ToItem();
                    var thisItem = botuser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type);
                    if (thisItem == null)
                    {
                        thisItem = item;
                        botuser.GameDeck.Items.Add(thisItem);
                    }
                    else ++thisItem.Count;

                    reward += $"+{item.Name}\n";
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} udało Ci się zdobyć:\n\n{reward}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("poświęćm")]
        [Alias("killm", "sacrificem", "poswiecm", "poświecm", "poświećm", "poswięćm", "poswiećm")]
        [Summary("dodaje exp do karty, poświęcając kilka innych")]
        [Remarks("5412 5411 5410"), RequireWaifuCommandChannel]
        public async Task SacraficeCardMultiAsync([Summary("WID(do ulepszenia)")]ulong idToUp, [Summary("WID kart(do poświęcenia)")]params ulong[] idsToSac)
        {
            if (idsToSac.Any(x => x == idToUp))
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} podałeś ten sam WID do ulepszenia i zniszczenia.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardToUp = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == idToUp);
                var cardsToSac = bUser.GameDeck.Cards.Where(x => idsToSac.Any(c => c == x.Id)).ToList();

                if (cardsToSac.Count < 1 || cardToUp == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (cardToUp.InCage)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta znajduje się w klatce.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                double totalExp = 0;
                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.IsBroken() || card.InCage || (card.Tags != null && card.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase)))
                    {
                        broken.Add(card);
                        continue;
                    }

                    ++bUser.Stats.SacraficeCards;
                    bUser.GameDeck.Karma -= 0.18;

                    var exp = _waifu.GetExpToUpgrade(cardToUp, card);
                    cardToUp.Affection += 0.08;
                    cardToUp.ExpCnt += exp;
                    totalExp += exp;

                    bUser.GameDeck.Cards.Remove(card);
                    _waifu.DeleteCardImageIfExist(card);
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę: {cardToUp.GetString(false, false, true)} o {totalExp.ToString("F")} exp.".ToEmbedMessage(EMType.Success).Build());

                if (broken.Count > 0)
                {
                     await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się poświęcić {broken.Count} kart.".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("poświęć")]
        [Alias("kill", "sacrifice", "poswiec", "poświec", "poświeć", "poswięć", "poswieć")]
        [Summary("dodaje exp do karty, poświęcając inną")]
        [Remarks("5412 5411"), RequireWaifuCommandChannel]
        public async Task SacraficeCardAsync([Summary("WID(do poświęcenia)")]ulong idToSac, [Summary("WID(do ulepszenia)")]ulong idToUp)
        {
            if (idToSac == idToUp)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} podałeś dwa razy ten sam WID.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardToSac = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == idToSac);
                var cardToUp = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == idToUp);

                if (cardToSac == null || cardToUp == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (cardToSac.IsBroken())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie może zostać poświęcona.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (cardToSac.InCage || cardToUp.InCage)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta znajduje się w klatce.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (cardToSac.Tags != null && cardToSac.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase))
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie możesz poświęcić karty oznaczonej jako ulubiona.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                ++bUser.Stats.SacraficeCards;
                bUser.GameDeck.Karma -= 0.18;

                var exp = _waifu.GetExpToUpgrade(cardToUp, cardToSac);
                cardToUp.Affection += 0.08;
                cardToUp.ExpCnt += exp;

                bUser.GameDeck.Cards.Remove(cardToSac);

                await db.SaveChangesAsync();
                _waifu.DeleteCardImageIfExist(cardToSac);

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę: {cardToUp.GetString(false, false, true)} o {exp.ToString("F")} exp.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("klatka")]
        [Alias("cage")]
        [Summary("otwiera klatkę z kartami(sprecyzowanie wid wyciąga tylko jedną kartę)")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task OpenCageAsync([Summary("WID(opcjonalne)")]ulong wid = 0)
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(user.Id);
                var cardsInCage = bUser.GameDeck.Cards.Where(x => x.InCage);

                var cntIn = cardsInCage.Count();
                if (cntIn < 1)
                {
                    await ReplyAsync("", embed: $"{user.Mention} nie posiadasz kart w klatce.".ToEmbedMessage(EMType.Info).Build());
                    return;
                }

                if (wid == 0)
                {
                    bUser.GameDeck.Karma += 0.01;

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

                    bUser.GameDeck.Karma -= 0.1;
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

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} wyciągnął {cntIn} kart z klatki.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("żusuń")]
        [Alias("wremove", "zusuń", "żusun", "zusun")]
        [Summary("usuwa kartę/tytuł/postać z listy życzeń")]
        [Remarks("karta 4212"), RequireWaifuCommandChannel]
        public async Task RemoveFromWishlistAsync([Summary("typ id(p - postać, t - tytuł, c - karta)")]WishlistObjectType type, [Summary("id/WID")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                string toAdd = "";
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);

                switch (type)
                {
                    case WishlistObjectType.Card:
                        toAdd = $"c{id}";
                        break;

                    case WishlistObjectType.Title:
                        toAdd = $"t{id}";
                        break;

                    case WishlistObjectType.Character:
                        toAdd = $"p{id}";
                        break;
                }

                var arr = new List<string>();
                if (bUser.GameDeck.Wishlist != null)
                    arr = bUser.GameDeck.Wishlist.Split(";").ToList();

                if (!arr.Any(x => x == toAdd))
                {
                    await ReplyAsync("", embed: "Nie posiadasz takiej pozycji na liście życzeń!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                arr.Remove(toAdd);
                bUser.GameDeck.Wishlist = string.Join(";", arr);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} usunął pozycję z listy życzeń.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("żdodaj")]
        [Alias("wadd", "zdodaj")]
        [Summary("dodaje kartę/tytuł/postać do listy życzeń")]
        [Remarks("karta 4212"), RequireWaifuCommandChannel]
        public async Task AddToWishlistAsync([Summary("typ id(p - postać, t - tytuł, c - karta)")]WishlistObjectType type, [Summary("id/WID")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                string toAdd = "";
                string response = "";
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);

                switch (type)
                {
                    case WishlistObjectType.Card:
                        toAdd = $"c{id}";
                        var card = db.Cards.FirstOrDefault(x => x.Id == id);
                        if (card == null)
                        {
                            await ReplyAsync("", embed: "Taka karta nie istnieje!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (card.GameDeckId == bUser.Id)
                        {
                            await ReplyAsync("", embed: "Już posiadasz taką kartę!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        response = card.GetString(false, false, true);
                        break;

                    case WishlistObjectType.Title:
                        toAdd = $"t{id}";
                        var res1 = await _shclient.Title.GetInfoAsync(id);
                        if (!res1.IsSuccessStatusCode())
                        {
                            await ReplyAsync("", embed: $"Nie odnaleziono serii!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        response = res1.Body.Title;
                        break;

                    case WishlistObjectType.Character:
                        toAdd = $"p{id}";
                        var res2 = await _shclient.GetCharacterInfoAsync(id);
                        if (!res2.IsSuccessStatusCode())
                        {
                            await ReplyAsync("", embed: $"Nie odnaleziono postaci!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        response = res2.Body.ToString();
                        break;
                }

                var arr = new List<string>();
                if (bUser.GameDeck.Wishlist != null)
                    arr = bUser.GameDeck.Wishlist.Split(";").ToList();

                if (arr.Any(x => x == toAdd))
                {
                    await ReplyAsync("", embed: "Już posaidasz taki wpis w liście życzeń!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                arr.Add(toAdd);
                bUser.GameDeck.Wishlist = string.Join(";", arr);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} dodał do listy życzeń: {response}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("na życzeniach", RunMode = RunMode.Async)]
        [Alias("on wishlist", "na zyczenia")]
        [Summary("wyświetla obiekty dodane do listy życzeń")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task ShowThingsOnWishlistAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(Context.User.Id);
                if (bUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Wishlist == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma nic na liście życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var p = bUser.GameDeck.GetCharactersWishList();
                var t = bUser.GameDeck.GetTitlesWishList();
                var c = bUser.GameDeck.GetCardsWishList();

                foreach (var emb in await _waifu.GetContentOfWishlist(c, p, t))
                {
                    await ReplyAsync("", embed: emb);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        [Command("życzenia", RunMode = RunMode.Async)]
        [Alias("wishlist", "zyczenia")]
        [Summary("wyświetla liste życzeń użytkownika")]
        [Remarks("Dzida"), RequireWaifuCommandChannel]
        public async Task ShowWishlistAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser usr = null, [Summary("czy pokazać ulubione(true/false) domyślnie ukryte, wymaga podania użytkownika")]bool showFavs = false)
        {
            var user = (usr ?? Context.User) as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(user.Id);
                if (bUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Wishlist == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma nic na liście życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var p = bUser.GameDeck.GetCharactersWishList();
                var t = bUser.GameDeck.GetTitlesWishList();
                var c = bUser.GameDeck.GetCardsWishList();

                var cards = await _waifu.GetCardsFromWishlist(c, p ,t, db, bUser.GameDeck.Cards);
                cards = cards.Where(x => x.GameDeckId != bUser.Id);

                if (!showFavs)
                    cards = cards.Where(x => x.Tags == null || (x.Tags != null && !x.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase)));

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client))
                {
                    await ReplyAsync("", embed: emb);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        [Command("wyzwól")]
        [Alias("unleash", "wyzwol")]
        [Summary("zmienia karte niewymienialną na wymienialną (200 CT)")]
        [Remarks("8651"), RequireWaifuCommandChannel]
        public async Task UnleashCardAsync([Summary("WID")]ulong wid)
        {
            int cost = 200;
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var thisCard = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);

                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (thisCard.IsTradable)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest wymienialna.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.CTCnt < cost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby CT.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                bUser.Stats.UnleashedCards += 1;
                bUser.GameDeck.CTCnt -= cost;
                bUser.GameDeck.Karma += 2;
                thisCard.IsTradable = true;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} wyzwolił kartę {thisCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz")]
        [Alias("tag")]
        [Summary("zmienia tag na karcie")]
        [Remarks("1 konie"), RequireWaifuCommandChannel]
        public async Task ChangeCardTagAsync([Summary("WID")]ulong wid, [Summary("tag(nie podanie - kasowanie)")][Remainder]string tag = null)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var thisCard = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);

                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (thisCard.Tags == null || tag == null)
                {
                    thisCard.Tags = tag;
                }
                else
                {
                    if (!thisCard.Tags.Contains(tag))
                        thisCard.Tags += $" {tag}";
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} oznaczył kartę {thisCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz puste")]
        [Alias("tag empty")]
        [Summary("zmienia tag na kartach które nie są oznaczone")]
        [Remarks("konie"), RequireWaifuCommandChannel]
        public async Task ChangeCardsTagAsync([Summary("tag")][Remainder]string tag)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var untaggedCards = bUser.GameDeck.Cards.Where(x => x.Tags == null).ToList();

                if (untaggedCards.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono nieoznaczonych kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var card in untaggedCards)
                    card.Tags = tag;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} oznaczył {untaggedCards.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("talia")]
        [Alias("deck", "aktywne")]
        [Summary("wyświetla aktywne karty/ustawia karte jako aktywną")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ChangeDeckCardStatusAsync([Summary("WID(opcjonalne)")]ulong wid = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetCachedFullUserAsync(Context.User.Id);
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

                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
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
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                string message = thisCard.Active ? "aktywował: " : "dezaktywował: ";
                await ReplyAsync("", embed: $"{Context.User.Mention} {message}{thisCard.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
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

            using (var db = new Database.UserContext(Config))
            {
                var cards = await db.Cards.Include(x => x.GameDeck).Where(x => x.Character == id).FromCacheAsync( new[] {"users"});

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart {response.Body}".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                await ReplyAsync("", embed: _waifu.GetWaifuFromCharacterSearchResult($"[**{response.Body}**]({response.Body.CharacterUrl}) posiadają:", cards, Context.Guild));
            }
        }

        [Command("ulubione", RunMode = RunMode.Async)]
        [Alias("favs")]
        [Summary("pozwala wyszukac użytkowników posiadających karty z naszej listy ulubionych postaci")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsFromFavListAsync([Summary("czy pokazać ulubione(true/false) domyślnie ukryte")]bool showFavs = false)
        {
            using (var db = new Database.UserContext(Config))
            {
                var user = await db.GetCachedFullUserAsync(Context.User.Id);
                if (user == null)
                {
                    await ReplyAsync("", embed: "Nie posiadasz profilu bota!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var response = await _shclient.User.GetFavCharactersAsync(user.Shinden);
                if (!response.IsSuccessStatusCode())
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono listy ulubionych postaci!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var cards = db.Cards.Include(x => x.GameDeck).Where(x => response.Body.Any(r => r.Id == x.Character));

                if (!showFavs)
                    cards = cards.Where(x => x.Tags == null || (x.Tags != null && !x.Tags.Contains("ulubione", StringComparison.CurrentCultureIgnoreCase)));

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client))
                {
                    await ReplyAsync("", embed: emb);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        [Command("jakie", RunMode = RunMode.Async)]
        [Alias("which")]
        [Summary("pozwala wyszukac użytkowników posiadających karty z danego tytułu")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsFromTitleAsync([Summary("id serii na shinden")]ulong id)
        {
            var response = await _shclient.Title.GetCharactersAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: $"Nie odnaleziono postaci z serii na shindenie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var cards = db.Cards.Include(x => x.GameDeck).Where(x => response.Body.Any(r => r.CharacterId == x.Character));

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client))
                {
                    await ReplyAsync("", embed: emb);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
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

            using (var db = new Database.UserContext(Config))
            {
                var duser1 = await db.GetCachedFullUserAsync(user1.Id);
                var duser2 = await db.GetCachedFullUserAsync(user2.Id);
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
        }

        [Command("arena")]
        [Alias("wild")]
        [Summary("walka z losowo wygenerowaną kartą")]
        [Remarks("1"), RequireWaifuFightChannel]
        public async Task FightInArenaAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var thisCard = botUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

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

                var enemyCard = _waifu.GenerateNewCard(enemyCharacter, _waifu.RandomizeRarity(_waifu.GetExcludedArenaRarity(thisCard.Rarity)));
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

                        if (Services.Fun.TakeATry(4))
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

                botUser.GameDeck.Karma -= 0.01;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users"});

                using (var cdb = new Database.GuildConfigContext(Config))
                {
                    var config = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);

                    try
                    {
                        embed.ImageUrl = await _waifu.GetArenaViewAsync(dInfo, Context.Guild.GetTextChannel(config.WaifuConfig.TrashFightChannel));
                    }
                    catch (Exception) { }
                    await ReplyAsync("", embed: embed.Build());
                }
            }
        }

        [Command("arenam", RunMode = RunMode.Async)]
        [Alias("wildm")]
        [Summary("walka przeciwko botowi w stylu GMwK")]
        [Remarks("1"), RequireWaifuFightChannel]
        public async Task StartPvEMassacreAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetCachedFullUserAsync(Context.User.Id);
                var thisCard = botUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var rUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var rcrd = rUser.GameDeck.Cards.First(x => x.Id == thisCard.Id);

                thisCard.Health = rcrd.Health;
                thisCard.Affection = rcrd.Affection;

                if (!thisCard.CanFightOnPvEGMwK())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} masz zbyt niską relację z tą kartą, aby mogła walczyć na arenie.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var players = new List<PlayerInfo>
                {
                    new PlayerInfo
                    {
                        User = Context.User as SocketGuildUser,
                        Cards = new List<Card> { thisCard },
                        Dbuser = botUser
                    }
                };

                var items = new List<Item> { _waifu.RandomizeItemFromFight().ToItem() };
                var characters = new List<BoosterPackCharacter>();

                var cardsCnt = Services.Fun.GetRandomValue(4, 9);
                var excludedRarity = _waifu.GetExcludedArenaRarity(thisCard.Rarity);
                for (int i = 0; i < cardsCnt; i++)
                {
                    var character = await _waifu.GetRandomCharacterAsync();
                    var botCard = _waifu.GenerateNewCard(character, thisCard.Rarity);
                    characters.Add(new BoosterPackCharacter { Character = character.Id });

                    botCard.GameDeckId = (ulong)i;
                    botCard.Id = (ulong)i;

                    if (i == 0 && botCard.Health < thisCard.Health)
                        botCard.Health = thisCard.Health;

                    if (i == 0 && botCard.Defence < thisCard.Defence)
                        botCard.Defence = thisCard.Defence;

                    players.Add(new PlayerInfo { Cards = new List<Card> { botCard } });
                }

                var history = await _waifu.MakeFightAsync(players, true);
                var deathLog = _waifu.GetDeathLog(history, players);

                bool userWon = history.Winner?.User != null;
                string resultString = $"Niestety przegrałeś {Context.User.Mention}\n\n";
                if (userWon)
                {
                    resultString = $"Wygrałeś {Context.User.Mention}!\n\n";
                    for (int i = 0; i < cardsCnt - 3; i++)
                    {
                        await Task.Delay(15);
                        if (Services.Fun.TakeATry(2))
                            items.Add(_waifu.RandomizeItemFromFight().ToItem());
                    }
                }

                items.Add(_waifu.RandomizeItemFromFight().ToItem());
                var boosterPack = new BoosterPack
                {
                    RarityExcludedFromPack = new List<RarityExcluded>(),
                    CardSourceFromPack = CardSource.PvE,
                    Name = "Losowa karta z masakry PvE",
                    IsCardFromPackTradable = true,
                    Characters = characters,
                    MinRarity = Rarity.E,
                    CardCnt = 1
                };

                bool userWonPack = Services.Fun.TakeATry(10);
                var thisCharacter = (await thisCard.GetCardInfoAsync(_shclient)).Info;

                var blowsDeal = history.Rounds.Select(x => x.Fights.Where(c => c.AtkCardId == thisCard.Id)).Sum(x => x.Count());
                double exp = 0.22 * blowsDeal;

                double affection = thisCharacter.HasImage ? 0.15 : 0.5;
                affection *= blowsDeal;

                if (!userWon)
                {
                    affection *= 1.5;
                    exp /= 1.5;
                }

                if (thisCard.IsUnusable())
                {
                    affection *= 1.5;
                    exp /= 5;
                }
                exp += 0.001;

                int chance = 10 - (int)thisCard.Affection;
                if (chance < 6) chance = 6;

                bool allowItems = Services.Fun.TakeATry(chance);
                if (thisCard.IsBroken()) allowItems = false;

                if ((userWon && !thisCard.IsUnusable()) || allowItems)
                {
                    foreach (var item in items)
                        resultString += $"+{item.Name}\n";
                }
                resultString += $"+{exp.ToString("F")} exp\n";

                if (userWonPack && userWon)
                    resultString += $"+{boosterPack.Name}";

                var exe = new Executable($"GMwK PvE - {thisCard.Name}", new Task(() =>
                {
                    using (var dbu = new Database.UserContext(_config))
                    {
                        var trUser = dbu.GetUserOrCreateAsync(Context.User.Id).Result;
                        var trCard = trUser.GameDeck.Cards.First(x => x.Id == thisCard.Id);

                        trCard.Affection -= affection;
                        trCard.ExpCnt += exp;

                        if (userWon)
                            trCard.ArenaStats.Wins += 1;
                        else
                            trCard.ArenaStats.Loses += 1;

                        if ((userWon && !thisCard.IsUnusable()) || allowItems)
                        {
                            foreach (var item in items)
                            {
                                var thisItem = trUser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type);
                                if (thisItem == null)
                                {
                                    thisItem = item;
                                    trUser.GameDeck.Items.Add(thisItem);
                                }
                                else ++thisItem.Count;
                            }
                        }

                        if (userWonPack && userWon)
                            trUser.GameDeck.BoosterPacks.Add(boosterPack);

                        trUser.GameDeck.Karma -= 0.03;

                        dbu.SaveChanges();

                        QueryCacheManager.ExpireTag(new string[] { $"user-{trUser.Id}", "users" });
                    }
                }));
                await _executor.TryAdd(exe, TimeSpan.FromSeconds(1));

                await ReplyAsync("", embed: $"**Arena GMwK**:\n\n**Twoja karta**: {thisCard.GetString(false, false, true)}\n\n{deathLog.TrimToLength(1900)}{resultString}".ToEmbedMessage(EMType.Bot).Build());
            }
        }

        [Command("masakra", RunMode = RunMode.Async)]
        [Alias("massacre")]
        [Summary("rozpoczyna odliczanie do startu GMwK")]
        [Remarks("SS"), RequireWaifuFightChannel]
        public async Task StartMassacreAsync([Summary("maksymalna ranga uczestniczącej karty")]Rarity max = Rarity.SSS)
        {
            var addEmote = Emote.Parse("<:twa_podobaju_mie_sie:573904566836396032>");
            var msg = await ReplyAsync("", embed: _waifu.GetGMwKView(addEmote, max));
            await msg.AddReactionAsync(addEmote);

            await Task.Delay(TimeSpan.FromMinutes(3));

            await msg.RemoveReactionAsync(addEmote, Context.Client.CurrentUser);
            var users = await msg.GetReactionUsersAsync(addEmote, 300).FlattenAsync();

            using (var db = new Database.UserContext(Config))
            {
                var players = new List<PlayerInfo>();
                foreach (var u in users)
                {
                    var user = Context.Guild.GetUser(u.Id);
                    if (user == null) continue;

                    var botUser = await db.GetCachedFullUserAsync(user.Id);
                    if (botUser == null) continue;

                    var activeCards = botUser.GameDeck.Cards.Where(x => x.Active && x.Rarity >= max).ToList();
                    if (activeCards.Count < 1) continue;

                    players.Add(new PlayerInfo
                    {
                        Cards = new List<Card> { Services.Fun.GetOneRandomFrom(activeCards) },
                        Dbuser = botUser,
                        User = user
                    });
                }

                if (players.Count < 5)
                {
                    await msg.ModifyAsync(x => x.Embed = $"Na **GMwK** nie zgłosiła się wystarczająca liczba graczy!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                string playerList = "Lista graczy:\n";
                foreach (var p in players)
                {
                    var card = p.Cards.First();
                    var rUser = await db.GetUserOrCreateAsync(p.User.Id);
                    card.Health = rUser.GameDeck.Cards.First(x => x.Id == card.Id).Health;

                    playerList += $"{p.User.Mention}: {card.GetString(true, false, true)}\n";
                }

                var history = await _waifu.MakeFightAsync(players, true);
                var deathLog = _waifu.GetDeathLog(history, players);

                string resultString = $"Nikt nie wygrał tego pojedynku!";
                if (history.Winner != null)
                    resultString = $"Zwycięża {history.Winner.User.Mention}!";

                await _executor.TryAdd(_waifu.GetExecutableGMwK(history, players), TimeSpan.FromSeconds(1));

                await msg.ModifyAsync(x => x.Embed = $"**GMwK**:\n\n{playerList.TrimToLength(900)}\n{deathLog.TrimToLength(1000)}{resultString}".ToEmbedMessage(EMType.Error).Build());
                await msg.RemoveAllReactionsAsync();
            }
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

            using (var db = new Database.UserContext(Config))
            {
                var duser1 = await db.GetCachedFullUserAsync(user1.Id);
                var duser2 = await db.GetCachedFullUserAsync(user2.Id);

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
        }

        [Command("waifu")]
        [Alias("husbando")]
        [Summary("pozwala ustawić sobie ulubioną postać na profilu(musisz posiadać jej karte)")]
        [Remarks("451"), RequireWaifuCommandChannel]
        public async Task SetProfileWaifuAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (wid == 0)
                {
                    if (bUser.GameDeck.Waifu != 0)
                    {
                        bUser.GameDeck.Waifu = 0;
                        await db.SaveChangesAsync();
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
                    foreach (var card in allPrevWaifus) card.Affection -= 2;
                }

                bUser.GameDeck.Waifu = thisCard.Character;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ustawił {thisCard.Name} jako ulubioną postać.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ofiaruj")]
        [Alias("doante")]
        [Summary("ofiaruj trzy krople swojej krwii, aby przeistoczyć kartę w anioła lub demona(wymagany odpowiedni poziom karmy)")]
        [Remarks("451"), RequireWaifuCommandChannel]
        public async Task ChangeCardAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (!bUser.GameDeck.CanCreateDemon() || !bUser.GameDeck.CanCreateAngel())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie jesteś zły, ani dobry - po prostu nijaki.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

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

                if (!thisCard.CanGiveBloodOrUpgradeToSSS())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt niską relacje".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var blood = bUser.GameDeck.Items.FirstOrDefault(x => x.Type == ItemType.BetterIncreaseUpgradeCnt);
                if (blood == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} odziwo nie posiadasz kropli swojej krwii.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (blood.Count < 3)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} odziwo posaidasz za mało kropli swojej krwii.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (blood.Count > 3) blood.Count -= 3;
                else bUser.GameDeck.Items.Remove(blood);

                if (bUser.GameDeck.CanCreateDemon())
                {
                    if (thisCard.Dere == Dere.Yami)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} ta karta została już przeistoczona wcześniej.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    if (thisCard.Dere == Dere.Raito)
                    {
                        thisCard.Dere = Dere.Yato;
                        bUser.Stats.YatoUpgrades += 1;
                    }
                    else
                    {
                        thisCard.Dere = Dere.Yami;
                        bUser.Stats.YamiUpgrades += 1;
                    }
                }
                else if (bUser.GameDeck.CanCreateAngel())
                {
                    if (thisCard.Dere == Dere.Raito)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} ta karta została już przeistoczona wcześniej.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    if (thisCard.Dere == Dere.Yami)
                    {
                        thisCard.Dere = Dere.Yato;
                        bUser.Stats.YatoUpgrades += 1;
                    }
                    else
                    {
                        thisCard.Dere = Dere.Raito;
                        bUser.Stats.RaitoUpgrades += 1;
                    }
                }

                await db.SaveChangesAsync();
                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} nowy charakter to {thisCard.Dere}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("karcianka", RunMode = RunMode.Async)]
        [Alias("cpf")]
        [Summary("wyświetla profil PocketWaifu")]
        [Remarks("Karna"), RequireWaifuCommandChannel]
        public async Task ShowProfileAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser usr = null)
        {
            var user = (usr ?? Context.User) as SocketGuildUser;
            if (user == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(user.Id);
                if (bUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var sssCnt = bUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.SSS);
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

                string sssString = "";
                if (sssCnt > 0)
                    sssString = $"**SSS**: {sssCnt} ";

                var embed = new EmbedBuilder()
                {
                    Color = EMType.Bot.Color(),
                    Author = new EmbedAuthorBuilder().WithUser(user),
                    Description = $"*{bUser.GameDeck.GetUserNameStatus()}*\n\n"
                                + $"**Uwolnione:** {bUser.Stats.ReleasedCards}\n**Zniszczone:** {bUser.Stats.DestroyedCards}\n**Poświęcone:** {bUser.Stats.SacraficeCards}\n**Ulepszone:** {bUser.Stats.UpgaredCards}\n**Wyzwolone:** {bUser.Stats.UnleashedCards}\n\n"
                                + $"**CT:** {bUser.GameDeck.CTCnt}\n**Karma:** {bUser.GameDeck.Karma.ToString("F")}\n\n**Posiadane karty**: {bUser.GameDeck.Cards.Count}\n"
                                + $"{sssString}**SS**: {ssCnt} **S**: {sCnt} **A**: {aCnt} **B**: {bCnt} **C**: {cCnt} **D**: {dCnt} **E**:{eCnt}\n\n"
                                + $"**1vs1** Rozegrane: {a1vs1ac} Wygrane: {w1vs1ac}\n"
                                + $"**GMwK** Rozegrane: {abr} Wygrane: {wbr}"
                };

                if (bUser.GameDeck?.Waifu != 0)
                {
                    var tChar = bUser.GameDeck.Cards.OrderBy(x => x.Rarity).FirstOrDefault(x => x.Character == bUser.GameDeck.Waifu);
                    if (tChar != null)
                    {
                        var response = await _shclient.GetCharacterInfoAsync(tChar.Character);
                        if (response.IsSuccessStatusCode())
                        {
                            using (var cdb = new Database.GuildConfigContext(Config))
                            {
                                var config = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                                var channel = Context.Guild.GetTextChannel(config.WaifuConfig.TrashCommandsChannel);

                                embed.WithImageUrl(await _waifu.GetWaifuProfileImageAsync(tChar, response.Body, channel));
                                string wfi = (response.Body.Gender == Sden.Models.Sex.Male) ? "Husbando" : "Waifu";
                                embed.WithFooter(new EmbedFooterBuilder().WithText($"{wfi}: {response.Body}"));
                            }
                        }
                    }
                }

                await ReplyAsync("", embed: embed.Build());
            }
        }
    }
}
