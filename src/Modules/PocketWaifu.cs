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
        [Summary("wyświetla wszystkie posiadane karty")]
        [Remarks("tag konie"), RequireWaifuCommandChannel]
        public async Task ShowCardsAsync([Summary("typ sortowania (klatka/jakość/atak/obrona/relacja/życie/tag(-)/uszkodzone/niewymienialne/obrazek(-/c)/unikat)")]HaremType type = HaremType.Rarity, [Summary("tag)")][Remainder]string tag = null)
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
        [Alias("items", "item", "przedmiot")]
        [Summary("wypisuje posiadane przedmioty (informacje o przedmiocie, gdy podany jego numer)")]
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
                var card = db.Cards.Include(x => x.GameDeck).Include(x => x.ArenaStats).Include(x => x.TagList).AsNoTracking().FirstOrDefault(x => x.Id == wid);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                SocketUser user = Context.Guild.GetUser(card.GameDeck.UserId);
                if (user == null) user = Context.Client.GetUser(card.GameDeck.UserId);

                await ReplyAsync("", embed: card.GetDescSmall().TrimToLength(2000).ToEmbedMessage(EMType.Info).WithAuthor(new EmbedAuthorBuilder().WithUser(user)).Build());
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
                var card = db.Cards.Include(x => x.GameDeck).Include(x => x.ArenaStats).Include(x => x.TagList).AsNoTracking().FirstOrDefault(x => x.Id == wid);
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

        [Command("koszary")]
        [Alias("pvp shop")]
        [Summary("listowanie/zakup przedmiotu/wypisanie informacji")]
        [Remarks("1 info"), RequireWaifuCommandChannel]
        public async Task BuyItemPvPAsync([Summary("nr przedmiotu")]int itemNumber = 0, [Summary("info/4 (liczba przedmiotów do zakupu/id tytułu)")]string info = "0")
        {
            await ReplyAsync("", embed: await  _waifu.ExecuteShopAsync(ShopType.Pvp, Config, Context.User, itemNumber, info));
        }

        [Command("kiosk")]
        [Alias("ac shop")]
        [Summary("listowanie/zakup przedmiotu/wypisanie informacji")]
        [Remarks("1 info"), RequireWaifuCommandChannel]
        public async Task BuyItemActivityAsync([Summary("nr przedmiotu")]int itemNumber = 0, [Summary("info/4 (liczba przedmiotów do zakupu/id tytułu)")]string info = "0")
        {
            await ReplyAsync("", embed: await  _waifu.ExecuteShopAsync(ShopType.Activity, Config, Context.User, itemNumber, info));
        }

        [Command("sklepik")]
        [Alias("shop", "p2w")]
        [Summary("listowanie/zakup przedmiotu/wypisanie informacji (du użycia wymagany 10 lvl)")]
        [Remarks("1 info"), RequireWaifuCommandChannel, RequireLevel(10)]
        public async Task BuyItemAsync([Summary("nr przedmiotu")]int itemNumber = 0, [Summary("info/4 (liczba przedmiotów do zakupu/id tytułu)")]string info = "0")
        {
            await ReplyAsync("", embed: await  _waifu.ExecuteShopAsync(ShopType.Normal, Config, Context.User, itemNumber, info));
        }

        [Command("użyj")]
        [Alias("uzyj", "use")]
        [Summary("używa przedmiot na karcie lub nie")]
        [Remarks("1 4212 2"), RequireWaifuCommandChannel]
        public async Task UseItemAsync([Summary("nr przedmiotu")]int itemNumber, [Summary("WID")]ulong wid = 0, [Summary("liczba przedmiotów/link do obrazka/typ gwiazdki")]string detail = "1")
        {
            var session = new CraftingSession(Context.User, _waifu, _config);
            if (_session.SessionExist(session))
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie możesz używać przedmiotów, gdy masz otwarte menu tworzenia kart.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var imgCnt = 0;
                var itemCnt = 1;
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var itemList = bUser.GameDeck.Items.OrderBy(x => x.Type).ToList();

                if (itemList.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz żadnych przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (itemNumber <= 0 || itemNumber > itemList.Count)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz aż tylu przedmiotów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var dis = int.TryParse(detail, out itemCnt);
                if (itemCnt < 1)
                {
                    dis = false;
                    itemCnt = 1;
                }

                var item = itemList[itemNumber - 1];
                switch (item.Type)
                {
                    case ItemType.AffectionRecoveryBig:
                    case ItemType.AffectionRecoverySmall:
                    case ItemType.AffectionRecoveryNormal:
                    case ItemType.AffectionRecoveryGreat:
                    case ItemType.IncreaseUpgradeCnt:
                    case ItemType.IncreaseExpSmall:
                    case ItemType.IncreaseExpBig:
                    // special case
                    case ItemType.CardParamsReRoll:
                    case ItemType.DereReRoll:
                        break;

                    case ItemType.ChangeCardImage:
                        if (dis) imgCnt = itemCnt;
                        if (imgCnt < 0) imgCnt = 0;
                        itemCnt = 1;
                        break;

                    default:
                        if (itemCnt != 1)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} możesz użyć tylko jeden przedmiot tego typu na raz!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        break;
                }

                if (item.Count < itemCnt)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz tylu sztuk tego przedmiotu.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                bool noCardOperation = item.Type.CanUseWithoutCard();
                var card = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (card == null && !noCardOperation)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.Expedition != CardExpedition.None && !noCardOperation)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var activeFigure = bUser.GameDeck.Figures.FirstOrDefault(x => x.IsFocus);
                if (activeFigure == null && noCardOperation)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz aktywnej figurki!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!noCardOperation && card.FromFigure)
                {
                    switch (item.Type)
                    {
                        case ItemType.FigureSkeleton:
                        case ItemType.IncreaseExpBig:
                        case ItemType.IncreaseExpSmall:
                        case ItemType.CardParamsReRoll:
                        case ItemType.IncreaseUpgradeCnt:
                        case ItemType.BetterIncreaseUpgradeCnt:
                            await ReplyAsync("", embed: $"{Context.User.Mention} tego przedmiotu nie można użyć na tej karcie.".ToEmbedMessage(EMType.Error).Build());
                            return;

                        default:
                            break;
                    }
                }

                double karmaChange = 0;
                bool consumeItem = true;
                var cnt = (itemCnt > 1) ? $"x{itemCnt}" : "";
                var bonusFromQ = item.Quality.GetQualityModifier();
                double affectionInc = item.Type.BaseAffection() * itemCnt;
                var textRelation = noCardOperation ? "" : card.GetAffectionString();
                var cardString = noCardOperation ? "" : " na " + card.GetString(false, false, true);
                var embed = new EmbedBuilder
                {
                    Color = EMType.Bot.Color(),
                    Author = new EmbedAuthorBuilder().WithUser(Context.User),
                    Description = $"Użyto _{item.Name}_ {cnt}{cardString}\n\n"
                };

                switch (item.Type)
                {
                    case ItemType.AffectionRecoveryGreat:
                        karmaChange += 0.3 * itemCnt;
                        embed.Description += "Bardzo powiększyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoveryBig:
                        karmaChange += 0.1 * itemCnt;
                        embed.Description += "Znacznie powiększyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoveryNormal:
                        karmaChange += 0.01 * itemCnt;
                        embed.Description += "Powiększyła się relacja z kartą!";
                        break;

                    case ItemType.AffectionRecoverySmall:
                        karmaChange += 0.001 * itemCnt;
                        embed.Description += "Powiększyła się trochę relacja z kartą!";
                        break;

                    case ItemType.IncreaseExpSmall:
                        var exS = 1.5 * itemCnt;
                        exS += exS * bonusFromQ;

                        card.ExpCnt += exS;
                        karmaChange += 0.1 * itemCnt;
                        embed.Description += "Twoja karta otrzymała odrobinę punktów doświadczenia!";
                        break;

                    case ItemType.IncreaseExpBig:
                        var exB = 5d * itemCnt;
                        exB += exB * bonusFromQ;

                        card.ExpCnt += exB;
                        karmaChange += 0.3 * itemCnt;
                        embed.Description += "Twoja karta otrzymała punkty doświadczenia!";
                        break;

                    case ItemType.ChangeStarType:
                        try
                        {
                            card.StarStyle = new StarStyle().Parse(detail);
                        }
                        catch (Exception)
                        {
                            await ReplyAsync("", embed: "Nie rozpoznano typu gwiazdki!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        karmaChange += 0.001 * itemCnt;
                        embed.Description += "Zmieniono typ gwiazdki!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.ChangeCardImage:
                        var res = await _shclient.GetCharacterInfoAsync(card.Character);
                        if (!res.IsSuccessStatusCode())
                        {
                            await ReplyAsync("", embed: "Nie odnaleziono postaci na shinden!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        var urls = res.Body.Pictures.GetPicList();
                        if (imgCnt == 0 || !dis)
                        {
                            int tidx = 0;
                            var ls = "Obrazki: \n" + string.Join("\n", urls.Select(x => $"{++tidx}: {x}"));
                            await ReplyAsync("", embed: ls.ToEmbedMessage(EMType.Info).Build());
                            return;
                        }
                        else
                        {
                            if (imgCnt > urls.Count)
                            {
                                await ReplyAsync("", embed: "Nie odnaleziono obrazka!".ToEmbedMessage(EMType.Error).Build());
                                return;
                            }
                            var turl = urls[imgCnt - 1];
                            if (card.GetImage() == turl)
                            {
                                await ReplyAsync("", embed: "Taki obrazek jest już ustawiony!".ToEmbedMessage(EMType.Error).Build());
                                return;
                            }
                            card.CustomImage = turl;
                        }
                        karmaChange += 0.001 * itemCnt;
                        embed.Description += "Ustawiono nowy obrazek.";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.SetCustomImage:
                        if (!detail.IsURLToImage())
                        {
                            await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (card.Image == null)
                        {
                            await ReplyAsync("", embed: "Aby ustawić własny obrazek, karta musi posiadać wcześniej ustawiony główny (na stronie)!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        card.CustomImage = detail;
                        consumeItem = !card.FromFigure;
                        karmaChange += 0.001 * itemCnt;
                        embed.Description += "Ustawiono nowy obrazek. Pamiętaj jednak, że dodanie nieodpowiedniego obrazka może skutkować skasowaniem karty!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.SetCustomBorder:
                        if (!detail.IsURLToImage())
                        {
                            await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (card.Image == null)
                        {
                            await ReplyAsync("", embed: "Aby ustawić ramkę, karta musi posiadać wcześniej ustawiony obrazek na stronie!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        card.CustomBorder = detail;
                        karmaChange += 0.001 * itemCnt;
                        embed.Description += "Ustawiono nowy obrazek jako ramkę. Pamiętaj jednak, że dodanie nieodpowiedniego obrazka może skutkować skasowaniem karty!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.BetterIncreaseUpgradeCnt:
                        if (card.Curse == CardCurse.BloodBlockade)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} na tej karcie ciąży klątwa!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
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
                                    karmaChange += 0.6;
                                    embed.Description += "Bardzo powiększyła się relacja z kartą!";
                                }
                                else
                                {
                                    affectionInc = 1.2;
                                    karmaChange += 0.4;
                                    embed.Color = EMType.Warning.Color();
                                    embed.Description += $"Karta się zmartwiła!";
                                }
                            }
                            else
                            {
                                affectionInc = -5;
                                karmaChange -= 0.5;
                                embed.Color = EMType.Error.Color();
                                embed.Description += $"Karta się przeraziła!";
                            }
                        }
                        else
                        {
                            karmaChange += 2;
                            affectionInc = 1.5;
                            card.UpgradesCnt += 2;
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
                        if (card.UpgradesCnt + itemCnt > 5)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} nie można mieć więcej jak pięć ulepszeń dostępnych na karcie.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        karmaChange += itemCnt;
                        card.UpgradesCnt += itemCnt;
                        embed.Description += $"Zwiększono liczbę ulepszeń do {card.UpgradesCnt}!";
                        break;

                    case ItemType.DereReRoll:
                        if (card.Curse == CardCurse.DereBlockade)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} na tej karcie ciąży klątwa!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        karmaChange += 0.02 * itemCnt;
                        card.Dere = _waifu.RandomizeDere();
                        embed.Description += $"Nowy charakter to: {card.Dere}!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.CardParamsReRoll:
                        karmaChange += 0.03 * itemCnt;
                        card.Attack = _waifu.RandomizeAttack(card.Rarity);
                        card.Defence = _waifu.RandomizeDefence(card.Rarity);
                        embed.Description += $"Nowa moc karty to: 🔥{card.GetAttackWithBonus()} 🛡{card.GetDefenceWithBonus()}!";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.CheckAffection:
                        karmaChange -= 0.01;
                        embed.Description += $"Relacja wynosi: `{card.Affection.ToString("F")}`";
                        break;

                    case ItemType.FigureSkeleton:
                        if (card.Rarity != Rarity.SSS)
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} karta musi być rangi **SSS**.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        karmaChange -= 1;
                        var figure = item.ToFigure(card);
                        if (figure != null)
                        {
                            bUser.GameDeck.Figures.Add(figure);
                            bUser.GameDeck.Cards.Remove(card);
                        }
                        embed.Description += $"Rozpoczęto tworzenie figurki.";
                        _waifu.DeleteCardImageIfExist(card);
                        break;

                    case ItemType.FigureHeadPart:
                    case ItemType.FigureBodyPart:
                    case ItemType.FigureClothesPart:
                    case ItemType.FigureLeftArmPart:
                    case ItemType.FigureLeftLegPart:
                    case ItemType.FigureRightArmPart:
                    case ItemType.FigureRightLegPart:
                    case ItemType.FigureUniversalPart:
                        if (!activeFigure.CanAddPart(item))
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} część, którą próbujesz dodać ma zbyt niską jakość.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (!activeFigure.HasEnoughPointsToAddPart(item))
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} aktywowana część ma zbyt małą ilość punktów konstrukcji, wymagana to {activeFigure.ConstructionPointsToInstall(item)}.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        if (!activeFigure.AddPart(item))
                        {
                            await ReplyAsync("", embed: $"{Context.User.Mention} coś poszło nie tak.".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        embed.Description += $"Dodano część do figurki.";
                        break;

                    default:
                        await ReplyAsync("", embed: $"{Context.User.Mention} tego przedmiotu nie powinno tutaj być!".ToEmbedMessage(EMType.Error).Build());
                        return;
                }

                if (!noCardOperation && card.Character == bUser.GameDeck.Waifu)
                    affectionInc *= 1.15;

                if (!noCardOperation)
                {
                    var response = await _shclient.GetCharacterInfoAsync(card.Character);
                    if (response.IsSuccessStatusCode())
                    {
                        if (response.Body?.Points != null)
                        {
                            var ordered = response.Body.Points.OrderByDescending(x => x.Points);
                            if (ordered.Any(x => x.Name == embed.Author.Name))
                                affectionInc *= 1.1;
                        }
                    }
                }

                var mission = bUser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DUsedItems);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DUsedItems.NewTimeStatus();
                    bUser.TimeStatuses.Add(mission);
                }
                mission.Count(itemCnt);

                if (!noCardOperation && card.Dere == Dere.Tsundere)
                    affectionInc *= 1.2;

                if (item.Type.HasDifferentQualities())
                    affectionInc += affectionInc * bonusFromQ;

                if (consumeItem)
                    item.Count -= itemCnt;

                if (!noCardOperation)
                {
                    if (card.Curse == CardCurse.InvertedItems)
                    {
                        affectionInc = -affectionInc;
                        karmaChange = -karmaChange;
                    }

                    bUser.GameDeck.Karma += karmaChange;
                    card.Affection += affectionInc;

                    _ = card.CalculateCardPower();
                }

                var newTextRelation = noCardOperation ? "" : card.GetAffectionString();
                if (textRelation != newTextRelation)
                    embed.Description += $"\nNowa relacja to *{newTextRelation}*.";

                if (item.Count <= 0)
                    bUser.GameDeck.Items.Remove(item);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: embed.Build());
            }
        }

        [Command("pakiet")]
        [Alias("pakiet kart", "booster", "booster pack", "pack")]
        [Summary("wypisuje dostępne pakiety/otwiera pakiety(maksymalna suma kart z pakietów do otworzenia to 20)")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task OpenPacketAsync([Summary("nr pakietu kart")]int numberOfPack = 0, [Summary("liczba kolejnych pakietów")]int count = 1, [Summary("czy sprawdzić listy życzeń?")]bool checkWishlists = false)
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

                if (bUser.GameDeck.BoosterPacks.Count < (count + numberOfPack - 1) || count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz tylu pakietów.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var packs = bUser.GameDeck.BoosterPacks.ToList().GetRange(numberOfPack - 1, count);
                var cardsCount = packs.Sum(x => x.CardCnt);

                if (cardsCount > 20)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} suma kart z otwieranych pakietów nie może być większa jak dwadzieścia.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Cards.Count + cardsCount > bUser.GameDeck.MaxNumberOfCards)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz już miejsca na kolejną kartę!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var mission = bUser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DPacket);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DPacket.NewTimeStatus();
                    bUser.TimeStatuses.Add(mission);
                }

                var totalCards = new List<Card>();
                var charactersOnWishlist = new List<ulong>();

                foreach (var pack in packs)
                {
                    var cards = await _waifu.OpenBoosterPackAsync(Context.User, pack);
                    if (cards.Count < pack.CardCnt)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się otworzyć pakietu.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    mission.Count();

                    if (pack.CardSourceFromPack == CardSource.Activity || pack.CardSourceFromPack == CardSource.Migration)
                    {
                        bUser.Stats.OpenedBoosterPacksActivity += 1;
                    }
                    else
                    {
                        bUser.Stats.OpenedBoosterPacks += 1;
                    }

                    bUser.GameDeck.BoosterPacks.Remove(pack);

                    foreach (var card in cards)
                    {
                        if (bUser.GameDeck.RemoveCharacterFromWishList(card.Character))
                        {
                            charactersOnWishlist.Add(card.Id);
                        }
                        card.Affection += bUser.GameDeck.AffectionFromKarma();
                        bUser.GameDeck.Cards.Add(card);
                        totalCards.Add(card);
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                string openString = "";
                string packString = $"{count} pakietów";
                if (count == 1) packString = $"pakietu **{packs.First().Name}**";

                foreach (var card in totalCards)
                {
                    if (checkWishlists && count == 1)
                    {
                        var wishlists = db.GameDecks.Include(x => x.Wishes).AsNoTracking().Where(x => !x.WishlistIsPrivate && (x.Wishes.Any(c => c.Type == WishlistObjectType.Card && c.ObjectId == card.Id) || x.Wishes.Any(c => c.Type == WishlistObjectType.Character && c.ObjectId == card.Character))).ToList();
                        openString += charactersOnWishlist.Any(x => x == card.Id) ? "💚 " : ((wishlists.Count > 0) ? "💗 " : "🤍 ");
                    }
                    openString += $"{card.GetString(false, false, true)}\n";
                }

                await ReplyAsync("", embed: $"{Context.User.Mention} z {packString} wypadło:\n\n{openString.TrimToLength(1950)}".ToEmbedMessage(EMType.Success).Build());
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

                if (card.FromFigure)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} tej karty nie można restartować.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (card.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.IsUnusable())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt niską relację, aby dało się ją zrestartować.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                bUser.GameDeck.Karma -= 5;

                card.Defence = _waifu.RandomizeDefence(Rarity.E);
                card.Attack = _waifu.RandomizeAttack(Rarity.E);
                card.Dere = _waifu.RandomizeDere();
                card.Rarity = Rarity.E;
                card.UpgradesCnt = 2;
                card.RestartCnt += 1;
                card.ExpCnt = 0;

                card.Affection = card.RestartCnt * -0.2;

                _ = card.CalculateCardPower();

                if (card.RestartCnt > 1 && card.RestartCnt % 10 == 0 && card.RestartCnt <= 100)
                {
                    var inUserItem = bUser.GameDeck.Items.FirstOrDefault(x => x.Type == ItemType.SetCustomImage);
                    if (inUserItem == null)
                    {
                        inUserItem = ItemType.SetCustomImage.ToItem();
                        bUser.GameDeck.Items.Add(inUserItem);
                    }
                    else inUserItem.Count++;
                }

                await db.SaveChangesAsync();
                _waifu.DeleteCardImageIfExist(card);

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} zrestartował kartę do: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("aktualizuj")]
        [Alias("update")]
        [Summary("pobiera dane na tamat karty z shindena")]
        [Remarks("5412"), RequireWaifuCommandChannel]
        public async Task UpdateCardAsync([Summary("WID")]ulong id, [Summary("czy przywrócić obrazek ze strony")]bool defaultImage = false)
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

                if (card.FromFigure)
                {
                    _waifu.DeleteCardImageIfExist(card);
                    await ReplyAsync("", embed: $"{Context.User.Mention} tej karty nie można zaktualizować.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (defaultImage)
                    card.CustomImage = null;

                try
                {
                    await card.Update(Context.User, _shclient);

                    await db.SaveChangesAsync();
                    _waifu.DeleteCardImageIfExist(card);

                    QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                    await ReplyAsync("", embed: $"{Context.User.Mention} zaktualizował kartę: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception ex)
                {
                    await db.SaveChangesAsync();
                    await ReplyAsync("", embed: $"{Context.User.Mention}: {ex.Message}".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("ulepsz")]
        [Alias("upgrade")]
        [Summary("ulepsza kartę na lepszą jakość")]
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

                if (card.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.UpgradesCnt < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie ma już dostępnych ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (card.ExpCnt < card.ExpToUpgrade())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma niewystarczającą ilość punktów doświadczenia. Wymagane {card.ExpToUpgrade().ToString("F")}.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (card.UpgradesCnt < 5 && card.Rarity == Rarity.SS)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą ilość ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                    return;
                }

                if (!card.CanGiveBloodOrUpgradeToSSS() && card.Rarity == Rarity.SS)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma zbyt małą relację, aby ją ulepszyć.".ToEmbedMessage(EMType.Bot).Build());
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

                _ = card.CalculateCardPower();

                if (card.Rarity == Rarity.SSS)
                {
                    if (bUser.Stats.UpgradedToSSS++ % 10 == 0 && card.RestartCnt < 1)
                    {
                        var inUserItem = bUser.GameDeck.Items.FirstOrDefault(x => x.Type == ItemType.SetCustomImage);
                        if (inUserItem == null)
                        {
                            inUserItem = ItemType.SetCustomImage.ToItem();
                            bUser.GameDeck.Items.Add(inUserItem);
                        }
                        else inUserItem.Count++;
                    }
                }

                await db.SaveChangesAsync();
                _waifu.DeleteCardImageIfExist(card);

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę do: {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("uwolnij")]
        [Alias("release", "puśmje")]
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

                 var chLvl = bUser.GameDeck.ExpContainer.Level;

                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.InCage || card.HasTag("ulubione") || card.FromFigure || card.Expedition != CardExpedition.None)
                    {
                        broken.Add(card);
                        continue;
                    }

                    bUser.StoreExpIfPossible(((card.ExpCnt / 2) > card.GetMaxExpToChest(chLvl))
                        ? card.GetMaxExpToChest(chLvl)
                        : (card.ExpCnt / 2));

                    var incKarma = 1 * card.MarketValue;
                    if (incKarma > 0.001 && incKarma < 1.5)
                        bUser.GameDeck.Karma += incKarma;

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

                var chLvl = bUser.GameDeck.ExpContainer.Level;

                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.InCage || card.HasTag("ulubione") || card.FromFigure || card.Expedition != CardExpedition.None)
                    {
                        broken.Add(card);
                        continue;
                    }

                    bUser.StoreExpIfPossible((card.ExpCnt > card.GetMaxExpToChest(chLvl))
                        ? card.GetMaxExpToChest(chLvl)
                        : card.ExpCnt);

                    var incKarma = 1 * card.MarketValue;
                    if (incKarma > 0.001 && incKarma < 1.5)
                        bUser.GameDeck.Karma -= incKarma;

                    var incCt = card.GetValue() * card.MarketValue;
                    if (incCt > 0 && incCt < 50)
                        bUser.GameDeck.CTCnt += (long) incCt;

                    bUser.Stats.DestroyedCards += 1;

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

        [Command("skrzynia")]
        [Alias("chest")]
        [Summary("przenosi doświadczenie z skrzyni do karty (kosztuje CT)")]
        [Remarks("2154"), RequireWaifuCommandChannel]
        public async Task TransferExpFromChestAsync([Summary("WID")]ulong id, [Summary("liczba doświadczenia")]uint exp)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (bUser.GameDeck.ExpContainer.Level == ExpContainerLevel.Disabled)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz jeszcze skrzyni doświadczenia.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var card = bUser.GameDeck.Cards.FirstOrDefault(x => x.Id == id);
                if (card == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz takiej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.FromFigure)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} na tą kartę nie można przenieść doświadczenia.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var maxExpInOneTime = bUser.GameDeck.ExpContainer.GetMaxExpTransferToCard();
                if (maxExpInOneTime != -1 && exp > maxExpInOneTime)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} na tym poziomie możesz jednorazowo przelać tylko {maxExpInOneTime} doświadczenia.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.ExpContainer.ExpCount < exp)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej ilości doświadczenia.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var cost = bUser.GameDeck.ExpContainer.GetTransferCTCost();
                if (bUser.GameDeck.CTCnt < cost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby CT. ({cost})".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                card.ExpCnt += exp;
                bUser.GameDeck.ExpContainer.ExpCount -= exp;
                bUser.GameDeck.CTCnt -= cost;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} przeniesiono doświadczenie na kartę.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("tworzenie skrzyni")]
        [Alias("make chest")]
        [Summary("tworzy lub ulepsza skrzynię doświadczenia")]
        [Remarks("2154"), RequireWaifuCommandChannel]
        public async Task CreateChestAsync([Summary("WID kart")]params ulong[] ids)
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

                foreach (var card in cardsToSac)
                {
                    if (card.Rarity != Rarity.SSS)
                    {
                        await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie jest kartą SSS.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }
                }

                var cardNeeded = bUser.GameDeck.ExpContainer.GetChestUpgradeCostInCards();
                var bloodNeeded = bUser.GameDeck.ExpContainer.GetChestUpgradeCostInBlood();
                if (cardNeeded == -1 || bloodNeeded == -1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można bardziej ulepszyć skrzyni.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (cardsToSac.Count < cardNeeded)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} podałeś za mało kart SSS. ({cardNeeded})".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var blood = bUser.GameDeck.Items.FirstOrDefault(x => x.Type == ItemType.BetterIncreaseUpgradeCnt);
                if (blood == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz kropel krwi.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (blood.Count < bloodNeeded)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby kropel krwi. ({bloodNeeded})".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                blood.Count -= bloodNeeded;
                if (blood.Count <= 0)
                    bUser.GameDeck.Items.Remove(blood);

                for (int i = 0; i < cardNeeded; i++)
                    bUser.GameDeck.Cards.Remove(cardsToSac[i]);

                ++bUser.GameDeck.ExpContainer.Level;
                bUser.GameDeck.Karma -= 15;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} otrzymałeś skrzynię doświadczenia.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("karta+")]
        [Alias("free card")]
        [Summary("dostajesz jedną darmową kartę")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task GetFreeCardAsync([Summary("czy sprawdzić listy życzeń?")]bool checkWishlists = false)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                var freeCard = botuser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Card);
                if (freeCard == null)
                {
                    freeCard = StatusType.Card.NewTimeStatus();
                    botuser.TimeStatuses.Add(freeCard);
                }

                if (freeCard.IsActive())
                {
                    var timeTo = (int)freeCard.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz otrzymać następną darmową kartę dopiero za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (botuser.GameDeck.Cards.Count + 1 > botuser.GameDeck.MaxNumberOfCards)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz już miejsca na kolejną kartę!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var mission = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.WCardPlus);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.WCardPlus.NewTimeStatus();
                    botuser.TimeStatuses.Add(mission);
                }
                mission.Count();

                freeCard.EndsAt = DateTime.Now.AddHours(22);

                var card = _waifu.GenerateNewCard(Context.User, await _waifu.GetRandomCharacterAsync(),
                    new List<Rarity>() { Rarity.SS, Rarity.S, Rarity.A });

                bool wasOnWishlist = botuser.GameDeck.RemoveCharacterFromWishList(card.Character);
                card.Affection += botuser.GameDeck.AffectionFromKarma();
                card.Source = CardSource.Daily;

                botuser.GameDeck.Cards.Add(card);

                await db.SaveChangesAsync();

                var wishStr = "";
                if (checkWishlists)
                {
                    var wishlists = db.GameDecks.Include(x => x.Wishes).AsNoTracking().Where(x => !x.WishlistIsPrivate && (x.Wishes.Any(c => c.Type == WishlistObjectType.Card && c.ObjectId == card.Id) || x.Wishes.Any(c => c.Type == WishlistObjectType.Character && c.ObjectId == card.Character))).ToList();
                    wishStr = wasOnWishlist ? "💚 " : ((wishlists.Count > 0) ? "💗 " : "🤍 ");
                }

                QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users"});

                await ReplyAsync("", embed: $"{Context.User.Mention} otrzymałeś {wishStr}{card.GetString(false, false, true)}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("rynek")]
        [Alias("market")]
        [Summary("udajesz się na rynek z wybraną przez Ciebie kartą, aby pohandlować")]
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

                if (card.FromFigure)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} z tą kartą nie można iść na rynek.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
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
                    market = StatusType.Market.NewTimeStatus();
                    botuser.TimeStatuses.Add(market);
                }

                if (market.IsActive())
                {
                    var timeTo = (int)market.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz udać się ponownie na rynek za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var mission = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DMarket);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DMarket.NewTimeStatus();
                    botuser.TimeStatuses.Add(mission);
                }
                mission.Count();

                int nextMarket = 20 - (int)(botuser.GameDeck.Karma / 100);
                if (nextMarket > 22) nextMarket = 22;
                if (nextMarket < 4) nextMarket = 4;

                if (botuser.GameDeck.Karma >= 3000)
                {
                    int tK = (int)(botuser.GameDeck.Karma - 2000) / 1000;
                    nextMarket -= tK;

                    if (nextMarket < 1)
                        nextMarket = 1;
                }

                int itemCnt = 1 + (int)(card.Affection / 15);
                itemCnt += (int)(botuser.GameDeck.Karma / 180);
                if (itemCnt > 10) itemCnt = 10;
                if (itemCnt < 1) itemCnt = 1;

                if (card.CanGiveRing()) ++itemCnt;
                if (botuser.GameDeck.CanCreateAngel()) ++itemCnt;

                market.EndsAt = DateTime.Now.AddHours(nextMarket);
                card.Affection += 0.1;

                _ = card.CalculateCardPower();

                string reward = "";
                for (int i = 0; i < itemCnt; i++)
                {
                    var itmType = _waifu.RandomizeItemFromMarket();
                    var itmQu = Quality.Broken;
                    if (itmType.HasDifferentQualities())
                    {
                        itmQu = _waifu.RandomizeItemQualityFromMarket();
                    }

                    var item = itmType.ToItem(1, itmQu);
                    var thisItem = botuser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type && x.Quality == item.Quality);
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

                if (card.FromFigure)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} z tą kartą nie można iść na czarny rynek.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (card.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var market = botuser.TimeStatuses.FirstOrDefault(x => x.Type == StatusType.Market);
                if (market == null)
                {
                    market = StatusType.Market.NewTimeStatus();
                    botuser.TimeStatuses.Add(market);
                }

                if (market.IsActive())
                {
                    var timeTo = (int)market.RemainingMinutes();
                    await ReplyAsync("", embed: $"{Context.User.Mention} możesz udać się ponownie na czarny rynek za {timeTo / 60}h {timeTo % 60}m!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var mission = botuser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DMarket);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DMarket.NewTimeStatus();
                    botuser.TimeStatuses.Add(mission);
                }
                mission.Count();

                int nextMarket = 20 + (int)(botuser.GameDeck.Karma / 100);
                if (nextMarket > 22) nextMarket = 22;
                if (nextMarket < 4) nextMarket = 4;

                if (botuser.GameDeck.Karma <= -3000)
                {
                    int tK = (int)(botuser.GameDeck.Karma + 2000) / 1000;
                    nextMarket += tK;

                    if (nextMarket < 1)
                        nextMarket = 1;
                }

                int itemCnt = 1 + (int)(card.Affection / 15);
                itemCnt -= (int)(botuser.GameDeck.Karma / 180);
                if (itemCnt > 10) itemCnt = 10;
                if (itemCnt < 1) itemCnt = 1;

                if (card.CanGiveBloodOrUpgradeToSSS()) ++itemCnt;
                if (botuser.GameDeck.CanCreateDemon()) ++itemCnt;

                market.EndsAt = DateTime.Now.AddHours(nextMarket);
                card.Affection -= 0.2;

                _ = card.CalculateCardPower();

                string reward = "";
                for (int i = 0; i < itemCnt; i++)
                {
                    var itmType = _waifu.RandomizeItemFromBlackMarket();
                    var itmQu = Quality.Broken;
                    if (itmType.HasDifferentQualities())
                    {
                        itmQu = _waifu.RandomizeItemQualityFromMarket();
                    }

                    var item = itmType.ToItem(1, itmQu);
                    var thisItem = botuser.GameDeck.Items.FirstOrDefault(x => x.Type == item.Type && x.Quality == item.Quality);
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

        [Command("poświęć")]
        [Alias("sacrifice", "poswiec", "poświec", "poświeć", "poswięć", "poswieć")]
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

                if (cardToUp.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                double totalExp = 0;
                var broken = new List<Card>();
                foreach (var card in cardsToSac)
                {
                    if (card.IsBroken() || card.InCage || card.HasTag("ulubione") || card.FromFigure || card.Expedition != CardExpedition.None)
                    {
                        broken.Add(card);
                        continue;
                    }

                    ++bUser.Stats.SacraficeCards;
                    bUser.GameDeck.Karma -= 0.28;

                    var exp = _waifu.GetExpToUpgrade(cardToUp, card);
                    cardToUp.Affection += 0.07;
                    cardToUp.ExpCnt += exp;
                    totalExp += exp;

                    bUser.GameDeck.Cards.Remove(card);
                    _waifu.DeleteCardImageIfExist(card);
                }

                _ = cardToUp.CalculateCardPower();

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                if (cardsToSac.Count > broken.Count)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę: {cardToUp.GetString(false, false, true)} o {totalExp.ToString("F")} exp.".ToEmbedMessage(EMType.Success).Build());
                }

                if (broken.Count > 0)
                {
                     await ReplyAsync("", embed: $"{Context.User.Mention} nie udało się poświęcić {broken.Count} kart.".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("klatka")]
        [Alias("cage")]
        [Summary("otwiera klatkę z kartami (sprecyzowanie wid wyciąga tylko jedną kartę)")]
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

                        _ = card.CalculateCardPower();
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

                    _ = thisCard.CalculateCardPower();

                    foreach (var card in cardsInCage)
                    {
                        if (card.Id != thisCard.Id)
                            card.Affection -= 0.3;

                        _ = card.CalculateCardPower();
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{user.Mention} wyciągnął {cntIn} kart z klatki.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("żusuń")]
        [Alias("wremove", "zusuń", "żusun", "zusun")]
        [Summary("usuwa karty/tytuły/postacie z listy życzeń")]
        [Remarks("karta 4212 21452"), RequireWaifuCommandChannel]
        public async Task RemoveFromWishlistAsync([Summary("typ id (p - postać, t - tytuł, c - karta)")]WishlistObjectType type, [Summary("ids/WIDs")]params ulong[] ids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var objs = bUser.GameDeck.Wishes.Where(x => x.Type == type && ids.Any(c => c == x.ObjectId)).ToList();
                if (objs.Count < 1)
                {
                    await ReplyAsync("", embed: "Nie posiadasz takich pozycji na liście życzeń!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var obj in objs)
                    bUser.GameDeck.Wishes.Remove(obj);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} usunął pozycje z listy życzeń.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("żdodaj")]
        [Alias("wadd", "zdodaj")]
        [Summary("dodaje kartę/tytuł/postać do listy życzeń")]
        [Remarks("karta 4212"), RequireWaifuCommandChannel]
        public async Task AddToWishlistAsync([Summary("typ id (p - postać, t - tytuł, c - karta)")]WishlistObjectType type, [Summary("id/WID")]ulong id)
        {
            using (var db = new Database.UserContext(Config))
            {
                string response = "";
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (bUser.GameDeck.Wishes.Any(x => x.Type == type && x.ObjectId == id))
                {
                    await ReplyAsync("", embed: "Już posiadasz taki wpis w liście życzeń!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var obj = new WishlistObject
                {
                    ObjectId = id,
                    Type = type
                };

                switch (type)
                {
                    case WishlistObjectType.Card:
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
                        obj.ObjectName = $"{card.Id} - {card.Name}";
                        break;

                    case WishlistObjectType.Title:
                        var res1 = await _shclient.Title.GetInfoAsync(id);
                        if (!res1.IsSuccessStatusCode())
                        {
                            await ReplyAsync("", embed: $"Nie odnaleziono serii!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        response = res1.Body.Title;
                        obj.ObjectName = res1.Body.Title;
                        break;

                    case WishlistObjectType.Character:
                        var res2 = await _shclient.GetCharacterInfoAsync(id);
                        if (!res2.IsSuccessStatusCode())
                        {
                            await ReplyAsync("", embed: $"Nie odnaleziono postaci!".ToEmbedMessage(EMType.Error).Build());
                            return;
                        }
                        response = res2.Body.ToString();
                        obj.ObjectName = response;
                        break;
                }

                bUser.GameDeck.Wishes.Add(obj);

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} dodał do listy życzeń: {response}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("życzenia widok")]
        [Alias("wishlist view", "zyczenia widok")]
        [Summary("pozwala ukryć listę życzeń przed innymi graczami")]
        [Remarks("tak"), RequireWaifuCommandChannel]
        public async Task SetWishlistViewAsync([Summary("czy ma być widoczna? (tak/nie)")]bool view)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                bUser.GameDeck.WishlistIsPrivate = !view;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                string response = (!view) ? $"ukrył" : $"udostępnił";
                await ReplyAsync("", embed: $"{Context.User.Mention} {response} swoją listę życzeń!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("na życzeniach", RunMode = RunMode.Async)]
        [Alias("on wishlist", "na zyczeniach")]
        [Summary("wyświetla obiekty dodane do listy życzeń")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task ShowThingsOnWishlistAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser usr = null)
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

                if (Context.User.Id != bUser.Id && bUser.GameDeck.WishlistIsPrivate)
                {
                    await ReplyAsync("", embed: "Lista życzeń tej osoby jest prywatna!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Wishes.Count < 1)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma nic na liście życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var p = bUser.GameDeck.GetCharactersWishList();
                var t = bUser.GameDeck.GetTitlesWishList();
                var c = bUser.GameDeck.GetCardsWishList();

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in await _waifu.GetContentOfWishlist(c, p, t))
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("życzenia", RunMode = RunMode.Async)]
        [Alias("wishlist", "zyczenia")]
        [Summary("wyświetla liste życzeń użytkownika")]
        [Remarks("Dzida tak tak tak"), RequireWaifuCommandChannel]
        public async Task ShowWishlistAsync([Summary("użytkownik (opcjonalne)")]SocketGuildUser usr = null, [Summary("czy pokazać ulubione (true/false) domyślnie ukryte, wymaga podania użytkownika")]bool showFavs = false, [Summary("czy pokazać niewymienialne (true/false) domyślnie pokazane")] bool showBlocked = true, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
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

                if (Context.User.Id != bUser.Id && bUser.GameDeck.WishlistIsPrivate)
                {
                    await ReplyAsync("", embed: "Lista życzeń tej osoby jest prywatna!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Wishes.Count < 1)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma nic na liście życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var p = bUser.GameDeck.GetCharactersWishList();
                var t = bUser.GameDeck.GetTitlesWishList();
                var c = bUser.GameDeck.GetCardsWishList();

                var cards = await _waifu.GetCardsFromWishlist(c, p ,t, db, bUser.GameDeck.Cards);
                cards = cards.Where(x => x.GameDeckId != bUser.Id).ToList();

                if (!showFavs)
                    cards = cards.Where(x => !x.HasTag("ulubione")).ToList();

                if (!showBlocked)
                    cards = cards.Where(x => x.IsTradable).ToList();

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client, !showNames))
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("życzenia filtr", RunMode = RunMode.Async)]
        [Alias("wishlistf", "zyczeniaf")]
        [Summary("wyświetla pozycje z listy życzeń użytkownika zawierające tylko drugiego użytkownika")]
        [Remarks("Dzida Kokos tak tak tak"), RequireWaifuCommandChannel]
        public async Task ShowFilteredWishlistAsync([Summary("użytkownik do którego należy lista życzeń")]SocketGuildUser user, [Summary("użytkownik po którym odbywa się filtracja (opcjonalne)")]SocketGuildUser usrf = null, [Summary("czy pokazać ulubione (true/false) domyślnie ukryte, wymaga podania użytkownika")]bool showFavs = false, [Summary("czy pokazać niewymienialne (true/false) domyślnie pokazane")] bool showBlocked = true, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
        {
            var userf = (usrf ?? Context.User) as SocketGuildUser;
            if (userf == null) return;

            if (user.Id == userf.Id)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} podałeś dwa razy tego samego użytkownika.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetCachedFullUserAsync(user.Id);
                if (bUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (Context.User.Id != bUser.Id && bUser.GameDeck.WishlistIsPrivate)
                {
                    await ReplyAsync("", embed: "Lista życzeń tej osoby jest prywatna!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.Wishes.Count < 1)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma nic na liście życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var p = bUser.GameDeck.GetCharactersWishList();
                var t = bUser.GameDeck.GetTitlesWishList();
                var c = bUser.GameDeck.GetCardsWishList();

                var cards = await _waifu.GetCardsFromWishlist(c, p ,t, db, bUser.GameDeck.Cards);
                cards = cards.Where(x => x.GameDeckId == userf.Id).ToList();

                if (!showFavs)
                    cards = cards.Where(x => !x.HasTag("ulubione")).ToList();

                if (!showBlocked)
                    cards = cards.Where(x => x.IsTradable).ToList();

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client, !showNames))
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("kto chce", RunMode = RunMode.Async)]
        [Alias("who wants", "kc", "ww")]
        [Summary("wyszukuje na listach życzeń użytkowników danej karty, pomija tytuły")]
        [Remarks("51545"), RequireWaifuCommandChannel]
        public async Task WhoWantsCardAsync([Summary("wid karty")]ulong wid, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
        {
            using (var db = new Database.UserContext(Config))
            {
                var thisCards = db.Cards.Include(x => x.TagList).FirstOrDefault(x => x.Id == wid);
                if (thisCards == null)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var wishlists = db.GameDecks.Include(x => x.Wishes).Where(x => !x.WishlistIsPrivate && (x.Wishes.Any(c => c.Type == WishlistObjectType.Card && c.ObjectId == thisCards.Id) || x.Wishes.Any(c => c.Type == WishlistObjectType.Character && c.ObjectId == thisCards.Character))).ToList();
                if (wishlists.Count < 1)
                {
                    await ReplyAsync("", embed: $"Nikt nie chce tej karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                string usersStr = "";
                if (showNames)
                {
                    foreach(var deck in wishlists)
                    {
                        var dUser = Context.Client.GetUser(deck.Id);
                        if (dUser != null) usersStr += $"{dUser.Username}\n";
                    }
                }
                else
                {
                    usersStr = string.Join("\n", wishlists.Select(x => $"<@{x.Id}>"));
                }

                await ReplyAsync("", embed: $"**{thisCards.GetNameWithUrl()} chcą:**\n\n {usersStr}".TrimToLength(2000).ToEmbedMessage(EMType.Info).Build());
            }
        }

        [Command("kto chce anime", RunMode = RunMode.Async)]
        [Alias("who wants anime", "kca", "wwa")]
        [Summary("wyszukuje na wishlistach danego anime")]
        [Remarks("21"), RequireWaifuCommandChannel]
        public async Task WhoWantsCardsFromAnimeAsync([Summary("id anime")]ulong id, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
        {
            var response = await _shclient.Title.GetInfoAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: $"Nie odnaleziono tytułu!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var wishlists = db.GameDecks.Include(x => x.Wishes).Where(x => !x.WishlistIsPrivate && x.Wishes.Any(c => c.Type == WishlistObjectType.Title && c.ObjectId == id)).ToList();
                if (wishlists.Count < 1)
                {
                    await ReplyAsync("", embed: $"Nikt nie ma tego tytułu wpisanego na listę życzeń.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                string usersStr = "";
                if (showNames)
                {
                    foreach(var deck in wishlists)
                    {
                        var dUser = Context.Client.GetUser(deck.Id);
                        if (dUser != null) usersStr += $"{dUser.Username}\n";
                    }
                }
                else
                {
                    usersStr = string.Join("\n", wishlists.Select(x => $"<@{x.Id}>"));
                }

                await ReplyAsync("", embed: $"**Karty z {response.Body.Title} chcą:**\n\n {usersStr}".TrimToLength(2000).ToEmbedMessage(EMType.Info).Build());
            }
        }

        [Command("wyzwól")]
        [Alias("unleash", "wyzwol")]
        [Summary("zmienia karte niewymienialną na wymienialną (250 CT)")]
        [Remarks("8651"), RequireWaifuCommandChannel]
        public async Task UnleashCardAsync([Summary("WID")]ulong wid)
        {
            int cost = 250;
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

                if (thisCard.Expedition != CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta jest na wyprawie!".ToEmbedMessage(EMType.Error).Build());
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

        [Command("limit kart")]
        [Alias("card limit")]
        [Summary("zwiększa limit kart, jakie można posiadać o 100, podanie 0 jako krotności wypisuje obecny limit")]
        [Remarks("10"), RequireWaifuCommandChannel]
        public async Task IncCardLimitAsync([Summary("krotność użycia polecenia")]uint count = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} obecny limit to: {bUser.GameDeck.MaxNumberOfCards}".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (count > 20)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} jednorazowo można zwiększyć limit tylko o 2000.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                long cost = bUser.GameDeck.CalculatePriceOfIncMaxCardCount(count);
                if (bUser.TcCnt < cost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby TC, aby zwiększyć limit o {100 * count} potrzebujesz {cost} TC.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                bUser.TcCnt -= cost;
                bUser.GameDeck.MaxNumberOfCards += 100 * count;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} powiększył swój limit kart do {bUser.GameDeck.MaxNumberOfCards}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("kolor strony")]
        [Alias("site color")]
        [Summary("zmienia kolor przewodni profilu na stronie waifu (500 TC)")]
        [Remarks("#dc5341"), RequireWaifuCommandChannel]
        public async Task ChangeWaifuSiteForegroundColorAsync([Summary("kolor w formacie hex")]string color)
        {
            var tcCost = 500;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.TcCnt < tcCost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby TC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!color.IsAColorInHEX())
                {
                    await ReplyAsync("", embed: "Nie wykryto koloru! Upewnij się, że podałeś poprawny kod HEX!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.TcCnt -= tcCost;
                botuser.GameDeck.ForegroundColor = color;

                await db.SaveChangesAsync();

                await ReplyAsync("", embed: $"Zmieniono kolor na stronie waifu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("szczegół strony")]
        [Alias("szczegoł strony", "szczegol strony", "szczegól strony", "site fg", "site foreground")]
        [Summary("zmienia obrazek nakładany na tło profilu na stronie waifu (500 TC)")]
        [Remarks("https://i.imgur.com/eQoaZid.png"), RequireWaifuCommandChannel]
        public async Task ChangeWaifuSiteForegroundAsync([Summary("bezpośredni adres do obrazka")]string imgUrl)
        {
            var tcCost = 500;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.TcCnt < tcCost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby TC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!imgUrl.IsURLToImage())
                {
                    await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.TcCnt -= tcCost;
                botuser.GameDeck.ForegroundImageUrl = imgUrl;

                await db.SaveChangesAsync();

                await ReplyAsync("", embed: $"Zmieniono szczegół na stronie waifu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("tło strony")]
        [Alias("tlo strony", "site bg", "site background")]
        [Summary("zmienia obrazek tła profilu na stronie waifu (2000 TC)")]
        [Remarks("https://i.imgur.com/wmDhRWd.jpeg"), RequireWaifuCommandChannel]
        public async Task ChangeWaifuSiteBackgroundAsync([Summary("bezpośredni adres do obrazka")]string imgUrl)
        {
            var tcCost = 2000;

            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (botuser.TcCnt < tcCost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz wystarczającej liczby TC!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!imgUrl.IsURLToImage())
                {
                    await ReplyAsync("", embed: "Nie wykryto obrazka! Upewnij się, że podałeś poprawny adres!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.TcCnt -= tcCost;
                botuser.GameDeck.BackgroundImageUrl = imgUrl;

                await db.SaveChangesAsync();

                await ReplyAsync("", embed: $"Zmieniono tło na stronie waifu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("pozycja tła strony")]
        [Alias("pozycja tla strony", "site bgp", "site background position")]
        [Summary("zmienia położenie obrazka tła profilu na stronie waifu")]
        [Remarks("65"), RequireWaifuCommandChannel]
        public async Task ChangeWaifuSiteBackgroundPositionAsync([Summary("pozycja w % od 0 do 100")]uint position)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (position > 100)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} podano niepoprawną wartość!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.GameDeck.BackgroundPosition = (int) position;

                await db.SaveChangesAsync();

                await ReplyAsync("", embed: $"Zmieniono pozycję tła na stronie waifu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("pozycja szczegółu strony")]
        [Alias("pozycja szczególu strony", "pozycja szczegolu strony", "pozycja szczegołu strony", "site fgp", "site foreground position")]
        [Summary("zmienia położenie obrazka szczegółu profilu na stronie waifu")]
        [Remarks("78"), RequireWaifuCommandChannel]
        public async Task ChangeWaifuSiteForegroundPositionAsync([Summary("pozycja w % od 0 do 100")]uint position)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (position > 100)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} podano niepoprawną wartość!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                botuser.GameDeck.ForegroundPosition = (int) position;

                await db.SaveChangesAsync();

                await ReplyAsync("", embed: $"Zmieniono pozycję szczegółu na stronie waifu użytkownika: {Context.User.Mention}!".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("galeria")]
        [Alias("gallery")]
        [Summary("wykupuje dodatkowe 5 pozycji w galerii (koszt 100 TC), podanie 0 jako krotności wypisuje obecny limit")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task IncGalleryLimitAsync([Summary("krotność użycia polecenia")]uint count = 0)
        {
            int cost = 100 * (int)count;
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} obecny limit to: {bUser.GameDeck.CardsInGallery}.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.TcCnt < cost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby TC.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                bUser.TcCnt -= cost;
                bUser.GameDeck.CardsInGallery += 5 * (int)count;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} powiększył swój limit kart w galerii do {bUser.GameDeck.CardsInGallery}.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("wymień na kule")]
        [Alias("wymien na kule", "crystal")]
        [Summary("zmienia naszyjnik i bukiet kwiatów na kryształową kulę (koszt 5 CT)")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task ExchangeToCrystalBallAsync()
        {
            int cost = 5;
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var itemList = bUser.GameDeck.Items.OrderBy(x => x.Type).ToList();

                var item1 = itemList.FirstOrDefault(x => x.Type == ItemType.CardParamsReRoll);
                if (item1 == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby {ItemType.CardParamsReRoll.ToItem().Name}.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var item2 = itemList.FirstOrDefault(x => x.Type == ItemType.DereReRoll);
                if (item2 == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby {ItemType.DereReRoll.ToItem().Name}.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (bUser.GameDeck.CTCnt < cost)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz wystarczającej liczby CT.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (item1.Count == 1)
                {
                    bUser.GameDeck.Items.Remove(item1);
                }
                else item1.Count--;

                if (item2.Count == 1)
                {
                    bUser.GameDeck.Items.Remove(item2);
                }
                else item2.Count--;

                var item3 = itemList.FirstOrDefault(x => x.Type == ItemType.CheckAffection);
                if (item3 == null)
                {
                    item3 = ItemType.CheckAffection.ToItem();
                    bUser.GameDeck.Items.Add(item3);
                }
                else item3.Count++;

                bUser.GameDeck.CTCnt -= cost;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} uzyskał *{item3.Name}*".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz")]
        [Alias("tag")]
        [Summary("dodaje tag do kart")]
        [Remarks("konie 231 12341 22"), RequireWaifuCommandChannel]
        public async Task ChangeCardTagAsync([Summary("tag")]string tag, [Summary("WID kart")]params ulong[] wids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardsSelected = bUser.GameDeck.Cards.Where(x => wids.Any(c => c == x.Id)).ToList();

                if (cardsSelected.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var thisCard in cardsSelected)
                {
                    if (!thisCard.HasTag(tag))
                        thisCard.TagList.Add(new CardTag{ Name = tag });
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} oznaczył {cardsSelected.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz czyść")]
        [Alias("tag clean", "oznacz czysć", "oznacz czyśc", "oznacz czysc")]
        [Summary("czyści tagi z kart")]
        [Remarks("22"), RequireWaifuCommandChannel]
        public async Task CleanCardTagAsync([Summary("WID kart")]params ulong[] wids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardsSelected = bUser.GameDeck.Cards.Where(x => wids.Any(c => c == x.Id)).ToList();

                if (cardsSelected.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var thisCard in cardsSelected)
                    thisCard.TagList.Clear();

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} zdjął tagi z {cardsSelected.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz puste")]
        [Alias("tag empty")]
        [Summary("dodaje tag do kart, które nie są oznaczone")]
        [Remarks("konie"), RequireWaifuCommandChannel]
        public async Task ChangeCardsTagAsync([Summary("tag")]string tag)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var untaggedCards = bUser.GameDeck.Cards.Where(x => x.TagList.Count < 1).ToList();

                if (untaggedCards.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono nieoznaczonych kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var card in untaggedCards)
                    card.TagList.Add(new CardTag{ Name = tag });

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} oznaczył {untaggedCards.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz podmień")]
        [Alias("tag replace", "oznacz podmien")]
        [Summary("podmienia tag na wszystkich kartach, niepodanie nowego tagu usuwa tag z kart")]
        [Remarks("konie wymiana"), RequireWaifuCommandChannel]
        public async Task ReplaceCardsTagAsync([Summary("stary tag")]string oldTag, [Summary("nowy tag")]string newTag = "%$-1")
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cards = bUser.GameDeck.Cards.Where(x => x.HasTag(oldTag)).ToList();

                if (cards.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono nieoznaczonych kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var card in cards)
                {
                    var thisTag = card.TagList.FirstOrDefault(x => x.Name.Equals(oldTag, StringComparison.CurrentCultureIgnoreCase));
                    if (thisTag != null)
                    {
                        card.TagList.Remove(thisTag);

                        if (!card.HasTag(newTag) && newTag != "%$-1")
                            card.TagList.Add(new CardTag{ Name = newTag });
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} oznaczył {cards.Count} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("oznacz usuń")]
        [Alias("tag remove", "oznacz usun")]
        [Summary("kasuje tag z kart")]
        [Remarks("ulubione 2211 2123 33123"), RequireWaifuCommandChannel]
        public async Task RemoveCardTagAsync([Summary("tag")]string tag, [Summary("WID kart")]params ulong[] wids)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var cardsSelected = bUser.GameDeck.Cards.Where(x => wids.Any(c => c == x.Id)).ToList();

                if (cardsSelected.Count < 1)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                int counter = 0;
                foreach (var thisCard in cardsSelected)
                {
                    var tTag = thisCard.TagList.FirstOrDefault(x => x.Name.Equals(tag, StringComparison.CurrentCultureIgnoreCase));
                    if (tTag != null)
                    {
                        ++counter;
                        thisCard.TagList.Remove(tTag);
                    }
                }

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} zdjął tag {tag} z {counter} kart.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("zasady wymiany")]
        [Alias("exchange conditions")]
        [Summary("ustawia tekst będący zasadami wymiany z nami, wywołanie bez podania zasad kasuje tekst")]
        [Remarks("Wymieniam się tylko za karty z mojej listy życzeń."), RequireWaifuCommandChannel]
        public async Task SetExchangeConditionAsync([Summary("zasady wymiany")][Remainder]string condition = null)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);

                bUser.GameDeck.ExchangeConditions = condition;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ustawił nowe zasady wymiany.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("talia")]
        [Alias("deck", "aktywne")]
        [Summary("wyświetla aktywne karty/ustawia kartę jako aktywną")]
        [Remarks("1"), RequireWaifuCommandChannel]
        public async Task ChangeDeckCardStatusAsync([Summary("WID(opcjonalne)")]ulong wid = 0)
        {
            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetCachedFullUserAsync(Context.User.Id);
                var active = botUser.GameDeck.Cards.Where(x => x.Active).ToList();

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

                var tac = active.FirstOrDefault(x => x.Id == thisCard.Id);
                if (tac == null)
                {
                    active.Add(thisCard);
                    thisCard.Active = true;
                }
                else
                {
                    active.Remove(tac);
                    thisCard.Active = false;
                }

                bUser.GameDeck.DeckPower = active.Sum(x => x.CalculateCardPower());

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                var message = thisCard.Active ? "aktywował: " : "dezaktywował: ";
                var power = $"**Moc talii**: {bUser.GameDeck.DeckPower.ToString("F")}";
                await ReplyAsync("", embed: $"{Context.User.Mention} {message}{thisCard.GetString(false, false, true)}\n\n{power}".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("kto", RunMode = RunMode.Async)]
        [Alias("who")]
        [Summary("pozwala wyszukać użytkowników posiadających kartę danej postaci")]
        [Remarks("51 tak"), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsAsync([Summary("id postaci na shinden")]ulong id, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
        {
            var response = await _shclient.GetCharacterInfoAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: $"Nie odnaleziono postaci na shindenie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var cards = await db.Cards.Include(x => x.TagList).Include(x => x.GameDeck).Where(x => x.Character == id).AsNoTracking().FromCacheAsync( new[] {"users"});

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart {response.Body}".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var msgs = _waifu.GetWaifuFromCharacterSearchResult($"[**{response.Body}**]({response.Body.CharacterUrl}) posiadają:", cards, Context.Client, !showNames);
                if (msgs.Count == 1)
                {
                    await ReplyAsync("", embed: msgs.First());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in msgs)
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("ulubione", RunMode = RunMode.Async)]
        [Alias("favs")]
        [Summary("pozwala wyszukać użytkowników posiadających karty z naszej listy ulubionych postaci")]
        [Remarks("tak tak"), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsFromFavListAsync([Summary("czy pokazać ulubione (true/false) domyślnie ukryte")]bool showFavs = false, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
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

                var cards = await db.Cards.Include(x => x.TagList).Include(x => x.GameDeck).Where(x => x.GameDeckId != user.Id && response.Body.Any(r => r.Id == x.Character)).AsNoTracking().ToListAsync();

                if (!showFavs)
                    cards = cards.Where(x => !x.HasTag("ulubione")).ToList();

                if (cards.Count < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client, !showNames))
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
                }
            }
        }

        [Command("jakie", RunMode = RunMode.Async)]
        [Alias("which")]
        [Summary("pozwala wyszukać użytkowników posiadających karty z danego tytułu")]
        [Remarks("1 tak"), RequireWaifuCommandChannel]
        public async Task SearchCharacterCardsFromTitleAsync([Summary("id serii na shinden")]ulong id, [Summary("czy zamienić oznaczenia na nicki?")]bool showNames = false)
        {
            var response = await _shclient.Title.GetCharactersAsync(id);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: $"Nie odnaleziono postaci z serii na shindenie!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var characterIds = response.Body.Select(x => x.CharacterId).Distinct().ToList();
            if (characterIds.Count < 1)
            {
                await ReplyAsync("", embed: $"Nie odnaleziono postaci!".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var cards = await db.Cards.AsQueryable().Include(x => x.TagList).Include(x => x.GameDeck).AsSplitQuery().Where(x => characterIds.Contains(x.Character)).AsNoTracking().FromCacheAsync( new[] {"users"});

                if (cards.Count() < 1)
                {
                    await ReplyAsync("", embed: $"Nie odnaleziono kart.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                try
                {
                    var dm = await Context.User.GetOrCreateDMChannelAsync();
                    foreach (var emb in _waifu.GetWaifuFromCharacterTitleSearchResult(cards, Context.Client, !showNames))
                    {
                        await dm.SendMessageAsync("", embed: emb);
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                    await ReplyAsync("", embed: $"{Context.User.Mention} lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());
                }
                catch (Exception)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie można wysłać do Ciebie PW!".ToEmbedMessage(EMType.Error).Build());
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

        [Command("tworzenie")]
        [Alias("crafting")]
        [Summary("tworzy karte z przedmiotów")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task CraftCardAsync()
        {
            var user1 = Context.User as SocketGuildUser;
            if (user1 == null) return;

            var session = new CraftingSession(user1, _waifu, _config);
            if (_session.SessionExist(session))
            {
                await ReplyAsync("", embed: $"{user1.Mention} już masz otwarte menu tworzenia kart.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var duser1 = await db.GetCachedFullUserAsync(user1.Id);
                if (duser1 == null)
                {
                    await ReplyAsync("", embed: "Jeden z graczy nie posiada profilu!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (duser1.GameDeck.Cards.Count + 1 > duser1.GameDeck.MaxNumberOfCards)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie masz już miejsca na kolejną kartę!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                session.P1 = new PlayerInfo
                {
                    User = user1,
                    Dbuser = duser1,
                    Accepted = false,
                    CustomString = "",
                    Items = new List<Item>()
                };

                session.Name = "⚒ **Tworzenie:**";
                session.Tips = $"Polecenia: `dodaj/usuń [nr przedmiotu] [liczba]`.";
                session.Items = duser1.GameDeck.Items.ToList();

                var msg = await ReplyAsync("", embed: session.BuildEmbed());
                await msg.AddReactionsAsync(session.StartReactions);
                session.Message = msg;

                await _session.TryAddSession(session);
            }
        }

        [Command("wyprawa status", RunMode = RunMode.Async)]
        [Alias("expedition status")]
        [Summary("wypisuje karty znajdujące się na wyprawach")]
        [Remarks(""), RequireWaifuFightChannel]
        public async Task ShowExpeditionStatusAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                 var botUser = await db.GetCachedFullUserAsync(Context.User.Id);
                 var cardsOnExpedition = botUser.GameDeck.Cards.Where(x => x.Expedition != CardExpedition.None).ToList();

                 if (cardsOnExpedition.Count < 1)
                 {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie posiadasz kart znajdujących się na wyprawie.".ToEmbedMessage(EMType.Error).Build());
                    return;
                 }

                 var expStrs = cardsOnExpedition.Select(x => $"{x.GetShortString(true)}:\n Od {x.ExpeditionDate.ToShortDateTime()} na {x.Expedition.GetName("ej")} wyprawie.\nTraci siły po {x.CalculateMaxTimeOnExpeditionInMinutes(botUser.GameDeck.Karma).ToString("F")} min.");
                 await ReplyAsync("", embed: $"**Wyprawy[**{cardsOnExpedition.Count}/{botUser.GameDeck.LimitOfCardsOnExpedition()}**]** {Context.User.Mention}:\n\n{string.Join("\n\n", expStrs)}".ToEmbedMessage(EMType.Bot).WithUser(Context.User).Build());
            }
        }

        [Command("wyprawa koniec")]
        [Alias("expedition end")]
        [Summary("kończy wyprawę karty")]
        [Remarks("11321"), RequireWaifuFightChannel]
        public async Task EndCardExpeditionAsync([Summary("WID")]ulong wid)
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

                if (thisCard.Expedition == CardExpedition.None)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie jest na wyprawie.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var oldName = thisCard.Expedition;
                var message = _waifu.EndExpedition(botUser, thisCard);
                _ = thisCard.CalculateCardPower();

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users"});

                _ = Task.Run(async () =>
                {
                    await ReplyAsync("", embed: $"Karta {thisCard.GetString(false, false, true)} wróciła z {oldName.GetName("ej")} wyprawy!\n\n{message}".ToEmbedMessage(EMType.Success).WithUser(Context.User).Build());
                });
            }
        }

        [Command("wyprawa")]
        [Alias("expedition")]
        [Summary("wysyła kartę na wyprawę")]
        [Remarks("11321 n"), RequireWaifuFightChannel]
        public async Task SendCardToExpeditionAsync([Summary("WID")]ulong wid, [Summary("typ wyprawy")]CardExpedition expedition = CardExpedition.None)
        {
            if (expedition == CardExpedition.None)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} nie podałeś poprawnej nazwy wyprawy.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetUserOrCreateAsync(Context.User.Id);
                var thisCard = botUser.GameDeck.Cards.FirstOrDefault(x => x.Id == wid);
                if (thisCard == null)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie odnaleziono karty.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var cardsOnExp = botUser.GameDeck.Cards.Count(x => x.Expedition != CardExpedition.None);
                if (cardsOnExp >= botUser.GameDeck.LimitOfCardsOnExpedition())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} nie możesz wysłać więcej kart na wyprawę.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!thisCard.ValidExpedition(expedition, botUser.GameDeck.Karma))
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie może się udać na tą wyprawę.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var mission = botUser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DExpeditions);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DExpeditions.NewTimeStatus();
                    botUser.TimeStatuses.Add(mission);
                }
                mission.Count();

                thisCard.Expedition = expedition;
                thisCard.ExpeditionDate = DateTime.Now;

                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{botUser.Id}", "users"});

                _ = Task.Run(async () =>
                {
                    var max = thisCard.CalculateMaxTimeOnExpeditionInMinutes(botUser.GameDeck.Karma, expedition).ToString("F");
                    await ReplyAsync("", embed: $"{thisCard.GetString(false, false, true)} udała się na {expedition.GetName("ą")} wyprawę!\nZmęczy się za {max} min.".ToEmbedMessage(EMType.Success).WithUser(Context.User).Build());
                });
            }
        }

        [Command("pojedynek")]
        [Alias("duel")]
        [Summary("stajesz do walki naprzeciw innemu graczowi")]
        [Remarks(""), RequireWaifuDuelChannel]
        public async Task MakeADuelAsync()
        {
            using (var db = new Database.UserContext(Config))
            {
                var duser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (duser.GameDeck.NeedToSetDeckAgain())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} musisz na nowo ustawić swóją talie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var canFight = duser.GameDeck.CanFightPvP();
                if (canFight != DeckPowerStatus.Ok)
                {
                    var err = (canFight == DeckPowerStatus.TooLow) ? "słabą" : "silną";
                    await ReplyAsync("", embed: $"{Context.User.Mention} masz zbyt {err} talie ({duser.GameDeck.GetDeckPower().ToString("F")}).".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var pvpDailyMax = duser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.Pvp);
                if (pvpDailyMax == null)
                {
                    pvpDailyMax = Database.Models.StatusType.Pvp.NewTimeStatus();
                    duser.TimeStatuses.Add(pvpDailyMax);
                }

                if (!pvpDailyMax.IsActive())
                {
                    pvpDailyMax.EndsAt = DateTime.Now.Date.AddHours(3).AddDays(1);
                    duser.GameDeck.PVPDailyGamesPlayed = 0;
                }

                if (duser.GameDeck.ReachedDailyMaxPVPCount())
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} dziś już nie możesz rozegrać pojedynku.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if ((DateTime.Now - duser.GameDeck.PVPSeasonBeginDate.AddMonths(1)).TotalSeconds > 1)
                {
                    duser.GameDeck.PVPSeasonBeginDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    duser.GameDeck.SeasonalPVPRank = 0;
                }

                var allPvpPlayers = await db.GetCachedPlayersForPVP(duser.Id);
                if (allPvpPlayers.Count < 10)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} zbyt mała liczba graczy ma utworzoną poprawną talię!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                double toLong = 1;
                var pvpPlayersInRange = allPvpPlayers.Where(x => x.IsNearMMR(duser.GameDeck)).ToList();
                for (double mrr = 0.5; pvpPlayersInRange.Count < 10; mrr += (0.5 * toLong))
                {
                    pvpPlayersInRange = allPvpPlayers.Where(x => x.IsNearMMR(duser.GameDeck, mrr)).ToList();
                    toLong += 0.5;
                }

                var randEnemy = Services.Fun.GetOneRandomFrom(pvpPlayersInRange).UserId;
                var denemy = await db.GetUserOrCreateAsync(randEnemy);
                var euser = Context.Client.GetUser(denemy.Id);
                while (euser == null)
                {
                    randEnemy = Services.Fun.GetOneRandomFrom(pvpPlayersInRange).UserId;
                    denemy = await db.GetUserOrCreateAsync(randEnemy);
                    euser = Context.Client.GetUser(denemy.Id);
                }

                var players = new List<PlayerInfo>
                {
                    new PlayerInfo
                    {
                        Cards = duser.GameDeck.Cards.Where(x => x.Active).ToList(),
                        User = Context.User,
                        Dbuser = duser
                    },
                    new PlayerInfo
                    {
                        Cards = denemy.GameDeck.Cards.Where(x => x.Active).ToList(),
                        Dbuser = denemy,
                        User = euser
                    }
                };

                var fight = _waifu.MakeFightAsync(players);
                string deathLog = _waifu.GetDeathLog(fight, players);

                var res = FightResult.Lose;
                if (fight.Winner == null)
                    res = FightResult.Draw;
                else if (fight.Winner.User.Id == duser.Id)
                    res = FightResult.Win;

                duser.GameDeck.PvPStats.Add(new CardPvPStats
                {
                    Type = FightType.NewVersus,
                    Result = res
                });

                var mission = duser.TimeStatuses.FirstOrDefault(x => x.Type == Database.Models.StatusType.DPvp);
                if (mission == null)
                {
                    mission = Database.Models.StatusType.DPvp.NewTimeStatus();
                    duser.TimeStatuses.Add(mission);
                }
                mission.Count();

                var info = duser.GameDeck.CalculatePVPParams(denemy.GameDeck, res);
                await db.SaveChangesAsync();

                _ = Task.Run(async () =>
                {
                    string wStr = fight.Winner == null ? "Remis!" : $"Zwycięża {fight.Winner.User.Mention}!";
                    await ReplyAsync("", embed: $"⚔️ **Pojedynek**:\n{Context.User.Mention} vs. {euser.Mention}\n\n{deathLog.TrimToLength(2000)}\n{wStr}\n{info}".ToEmbedMessage(EMType.Bot).Build());
                });
            }
        }

        [Command("waifu")]
        [Alias("husbando")]
        [Summary("pozwala ustawić sobie ulubioną postać na profilu (musisz posiadać jej kartę)")]
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
                        var prevWaifus = bUser.GameDeck.Cards.Where(x => x.Character == bUser.GameDeck.Waifu);
                        foreach (var card in prevWaifus)
                        {
                            card.Affection -= 5;
                            _ = card.CalculateCardPower();
                        }

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

                if (bUser.GameDeck.Waifu == thisCard.Character)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} masz już ustawioną tą postać!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var allPrevWaifus = bUser.GameDeck.Cards.Where(x => x.Character == bUser.GameDeck.Waifu);
                foreach (var card in allPrevWaifus)
                {
                    card.Affection -= 5;
                    _ = card.CalculateCardPower();
                }

                bUser.GameDeck.Waifu = thisCard.Character;
                await db.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}", "users" });

                await ReplyAsync("", embed: $"{Context.User.Mention} ustawił {thisCard.Name} jako ulubioną postać.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("ofiaruj")]
        [Alias("doante")]
        [Summary("ofiaruj trzy krople swojej krwi, aby przeistoczyć kartę w anioła lub demona (wymagany odpowiedni poziom karmy)")]
        [Remarks("451"), RequireWaifuCommandChannel]
        public async Task ChangeCardAsync([Summary("WID")]ulong wid)
        {
            using (var db = new Database.UserContext(Config))
            {
                var bUser = await db.GetUserOrCreateAsync(Context.User.Id);
                if (!bUser.GameDeck.CanCreateDemon() && !bUser.GameDeck.CanCreateAngel())
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
                    await ReplyAsync("", embed: $"{Context.User.Mention} o dziwo nie posiadasz kropli swojej krwi.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (blood.Count < 3)
                {
                    await ReplyAsync("", embed: $"{Context.User.Mention} o dziwo posiadasz za mało kropli swojej krwi.".ToEmbedMessage(EMType.Error).Build());
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
        public async Task ShowProfileAsync([Summary("użytkownik (opcjonalne)")]SocketGuildUser usr = null)
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

                var aPvp = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.NewVersus);
                var wPvp = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.NewVersus);

                var seasonString = "----";
                if (bUser.GameDeck.IsPVPSeasonalRankActive())
                    seasonString = $"{bUser.GameDeck.GetRankName()} ({bUser.GameDeck.SeasonalPVPRank})";

                var globalString = $"{bUser.GameDeck.GetRankName(bUser.GameDeck.GlobalPVPRank)} ({bUser.GameDeck.GlobalPVPRank})";

                var sssString = "";
                if (sssCnt > 0)
                    sssString = $"**SSS**: {sssCnt} ";

                var embed = new EmbedBuilder()
                {
                    Color = EMType.Bot.Color(),
                    Author = new EmbedAuthorBuilder().WithUser(user),
                    Description = $"*{bUser.GameDeck.GetUserNameStatus()}*\n\n"
                                + $"**Skrzynia({(int)bUser.GameDeck.ExpContainer.Level})**: {bUser.GameDeck.ExpContainer.ExpCount.ToString("F")}\n"
                                + $"**Uwolnione**: {bUser.Stats.ReleasedCards}\n**Zniszczone**: {bUser.Stats.DestroyedCards}\n**Poświęcone**: {bUser.Stats.SacraficeCards}\n**Ulepszone**: {bUser.Stats.UpgaredCards}\n**Wyzwolone**: {bUser.Stats.UnleashedCards}\n\n"
                                + $"**CT**: {bUser.GameDeck.CTCnt}\n**Karma**: {bUser.GameDeck.Karma.ToString("F")}\n\n**Posiadane karty**: {bUser.GameDeck.Cards.Count}\n"
                                + $"{sssString}**SS**: {ssCnt} **S**: {sCnt} **A**: {aCnt} **B**: {bCnt} **C**: {cCnt} **D**: {dCnt} **E**:{eCnt}\n\n"
                                + $"**PVP** Rozegrane: {aPvp} Wygrane: {wPvp}\n**GR**: {globalString}\n**SR**: {seasonString}"
                };

                if (bUser.GameDeck?.Waifu != 0)
                {
                    var tChar = bUser.GameDeck.Cards.OrderBy(x => x.Rarity).FirstOrDefault(x => x.Character == bUser.GameDeck.Waifu);
                    if (tChar != null)
                    {
                        using (var cdb = new Database.GuildConfigContext(Config))
                        {
                            var config = await cdb.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                            var channel = Context.Guild.GetTextChannel(config.WaifuConfig.TrashCommandsChannel);

                            embed.WithImageUrl(await _waifu.GetWaifuProfileImageAsync(tChar, channel));
                            embed.WithFooter(new EmbedFooterBuilder().WithText($"{tChar.Name}"));
                        }
                    }
                }

                await ReplyAsync("", embed: embed.Build());
            }
        }
    }
}
