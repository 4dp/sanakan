#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services;
using Sanakan.Services.Commands;
using Sanakan.Services.PocketWaifu;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using System;
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
        private Waifu _waifu;

        public PocketWaifu(Waifu waifu, Sden.ShindenClient client, Database.UserContext userContext, 
            Database.GuildConfigContext dbGuildConfigContext, SessionManager session)
        {
            _waifu = waifu;
            _shclient = client;
            _session = session;
            _dbUserContext = userContext;
            _dbGuildConfigContext = dbGuildConfigContext;
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

            if (card.Rarity == Rarity.SS)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma już najwyższy poziom.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.UpgradesCnt < 1)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta nie ma już dostępnych ulepszeń.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            if (card.ExpCnt < 30)
            {
                await ReplyAsync("", embed: $"{Context.User.Mention} ta karta ma niewystarczającą ilość punktów doświadczenia.".ToEmbedMessage(EMType.Bot).Build());
                return;
            }

            ++bUser.Stats.UpgaredCards;

            card.Defence = Waifu.GetDefenceAfterLevelUp(card.Rarity, card.Defence);
            card.Attack = Waifu.GetAttactAfterLevelUp(card.Rarity, card.Attack);
            card.Rarity = --card.Rarity;
            card.UpgradesCnt -= 1;
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

            var exp = Waifu.GetExpToUpgrade(cardToUp, cardToSac);
            cardToUp.Affection += 0.01;
            cardToUp.ExpCnt += exp;

            bUser.GameDeck.Cards.Remove(cardToSac);

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{bUser.Id}" });

            await ReplyAsync("", embed: $"{Context.User.Mention} ulepszył kartę: {cardToUp.GetString(false, false, true)} o {exp.ToString("F")} exp.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("klatka")]
        [Alias("cage")]
        [Summary("otwiera klatkę z kartami")]
        [Remarks(""), RequireWaifuCommandChannel]
        public async Task OpenCageAsync()
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
                    if (dm != null)
                    {
                        await dm.SendMessageAsync("", embed: Waifu.GetActiveList(active));
                        await dm.CloseAsync();
                    }
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

            var a1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.vs1);
            var a1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Type == FightType.vs3);
            
            var w1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.vs1);
            var d1vs1 = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Draw && x.Type == FightType.vs1);
            var w1vs1ac = bUser.GameDeck?.PvPStats?.Count(x => x.Result == FightResult.Win && x.Type == FightType.vs3);

            var embed = new EmbedBuilder()
            {
                Color = EMType.Bot.Color(),
                Author = new EmbedAuthorBuilder().WithUser(user),
                Description = $"**Posiadane karty**: {bUser.GameDeck.Cards.Count}\n"
                            + $"**SS**: {ssCnt} **S**: {sCnt} **A**: {aCnt} **B**: {bCnt} **C**: {cCnt} **D**: {dCnt} **E**:{eCnt}\n\n"
                            + $"**1vs1** Rozegrane: {a1vs1} Wygrane: {w1vs1} Remis: {d1vs1}\n"
                            + $"**1vs1 AC** Rozegrane: {a1vs1ac} Wygrane: {w1vs1ac}\n"
                            + $"**GMwK** Rozegrane: 0 Wygrane: 0"
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