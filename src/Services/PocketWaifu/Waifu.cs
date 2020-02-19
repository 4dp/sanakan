#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.Executor;
using Sanakan.Services.PocketWaifu.Fight;
using Shinden;
using Shinden.Models;
using Z.EntityFramework.Plus;

namespace Sanakan.Services.PocketWaifu
{
    public enum FightWinner
    {
        Card1, Card2, Draw
    }

    public enum HaremType
    {
        Rarity, Cage, Affection, Attack, Defence, Health, Tag, NoTag, Blocked, Broken, Picture, NoPicture, CustomPicture, Unique
    }

    public class Waifu
    {
        private static CharacterIdUpdate CharId = new CharacterIdUpdate();

        private IConfig _config;
        private ImageProcessing _img;
        private ShindenClient _shClient;

        public Waifu(ImageProcessing img, ShindenClient client, IConfig config)
        {
            _img = img;
            _config = config;
            _shClient = client;
        }

        public bool GetEventSate() => CharId.EventEnabled;

        public void SetEventState(bool state) => CharId.EventEnabled = state;

        public void SetEventIds(List<ulong> ids) => CharId.SetEventIds(ids);

        public List<Card> GetListInRightOrder(IEnumerable<Card> list, HaremType type, string tag)
        {
            var nList = new List<Card>();
            var tagList = tag.Split(" ").ToList();

            switch (type)
            {
                case HaremType.Health:
                    return list.OrderByDescending(x => x.GetHealthWithPenalty()).ToList();

                case HaremType.Affection:
                    return list.OrderByDescending(x => x.Affection).ToList();

                case HaremType.Attack:
                    return list.OrderByDescending(x => x.GetAttackWithBonus()).ToList();

                case HaremType.Defence:
                    return list.OrderByDescending(x => x.GetDefenceWithBonus()).ToList();

                case HaremType.Unique:
                    return list.Where(x => x.Unique).ToList();

                case HaremType.Cage:
                    return list.Where(x => x.InCage).ToList();

                case HaremType.Blocked:
                    return list.Where(x => !x.IsTradable).ToList();

                case HaremType.Broken:
                    return list.Where(x => x.IsBroken()).ToList();

                case HaremType.Tag:
                {
                    foreach (var t in tagList)
                    {
                        if (t.Length < 1)
                            continue;

                        nList = list.Where(x => x.TagList.Any(c => c.Name.Equals(t, StringComparison.CurrentCultureIgnoreCase))).ToList();
                    }
                    return nList;
                }

                case HaremType.NoTag:
                {
                    foreach (var t in tagList)
                    {
                        if (t.Length < 1)
                            continue;

                        nList = list.Where(x => !x.TagList.Any(c => c.Name.Equals(t, StringComparison.CurrentCultureIgnoreCase))).ToList();
                    }
                    return nList;
                }

                case HaremType.Picture:
                    return list.Where(x => x.HasImage()).ToList();

                case HaremType.NoPicture:
                    return list.Where(x => x.Image == null).ToList();

                case HaremType.CustomPicture:
                    return list.Where(x => x.CustomImage != null).ToList();

                default:
                case HaremType.Rarity:
                    return list.OrderBy(x => x.Rarity).ToList();
            }
        }

        public Embed GetGMwKView(IEmote emote, Rarity max)
        {
            var time = DateTime.Now.AddMinutes(3);
            return new EmbedBuilder
            {
                Color = EMType.Error.Color(),
                Description = $"**Grupowa Masakra w Kisielu**\n\nRozpoczęcie: `{time.ToShortTimeString()}:{time.Second.ToString("00")}`\n"
                    + $"Wymagana minimalna liczba graczy: `5`\nMaksymalna jakość karty: `{max}`\n\nAby dołączyć kliknij na reakcje {emote}"
            }.Build();
        }

        public Rarity RandomizeRarity()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 5)   return Rarity.SS;
            if (num < 25)  return Rarity.S;
            if (num < 75)  return Rarity.A;
            if (num < 175) return Rarity.B;
            if (num < 370) return Rarity.C;
            if (num < 620) return Rarity.D;
            return Rarity.E;
        }

        public List<Rarity> GetExcludedArenaRarity(Rarity cardRarity)
        {
            var excudled = new List<Rarity>();

            switch (cardRarity)
            {
                case Rarity.SSS:
                    excudled.Add(Rarity.A);
                    excudled.Add(Rarity.B);
                    excudled.Add(Rarity.C);
                    excudled.Add(Rarity.D);
                    excudled.Add(Rarity.E);
                    break;

                case Rarity.SS:
                    excudled.Add(Rarity.B);
                    excudled.Add(Rarity.C);
                    excudled.Add(Rarity.D);
                    excudled.Add(Rarity.E);
                    break;

                case Rarity.S:
                    excudled.Add(Rarity.C);
                    excudled.Add(Rarity.D);
                    excudled.Add(Rarity.E);
                    break;

                case Rarity.A:
                    excudled.Add(Rarity.D);
                    excudled.Add(Rarity.E);
                    break;

                case Rarity.B:
                    excudled.Add(Rarity.E);
                    break;

                case Rarity.C:
                    excudled.Add(Rarity.SS);
                    break;

                case Rarity.D:
                    excudled.Add(Rarity.SS);
                    excudled.Add(Rarity.S);
                    break;

                default:
                case Rarity.E:
                    excudled.Add(Rarity.SS);
                    excudled.Add(Rarity.S);
                    excudled.Add(Rarity.A);
                    break;
            }

            return excudled;
        }

        public Rarity RandomizeRarity(List<Rarity> rarityExcluded)
        {
            if (rarityExcluded == null) return RandomizeRarity();
            if (rarityExcluded.Count < 1) return RandomizeRarity();

            var list = new List<RarityChance>()
            {
                new RarityChance(5,    Rarity.SS),
                new RarityChance(25,   Rarity.S ),
                new RarityChance(75,   Rarity.A ),
                new RarityChance(175,  Rarity.B ),
                new RarityChance(370,  Rarity.C ),
                new RarityChance(650,  Rarity.D ),
                new RarityChance(1000, Rarity.E ),
            };

            var ex = list.Where(x => rarityExcluded.Any(c => c == x.Rarity)).ToList();
            foreach(var e in ex) list.Remove(e);

            var num = Fun.GetRandomValue(1000);
            foreach(var rar in list)
            {
                if (num < rar.Chance)
                    return rar.Rarity;
            }
            return list.Last().Rarity;
        }

        public ItemType RandomizeItemFromFight()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 2) return ItemType.IncreaseExpSmall;
            if (num < 8) return ItemType.BetterIncreaseUpgradeCnt;
            if (num < 15) return ItemType.IncreaseUpgradeCnt;
            if (num < 40) return ItemType.AffectionRecoveryGreat;
            if (num < 95) return ItemType.AffectionRecoveryBig;
            if (num < 150) return ItemType.CardParamsReRoll;
            if (num < 225) return ItemType.DereReRoll;
            if (num < 475) return ItemType.AffectionRecoveryNormal;
            return ItemType.AffectionRecoverySmall;
        }

        public ItemType RandomizeItemFromMFight()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 2) return ItemType.IncreaseExpBig;
            if (num < 10) return ItemType.BetterIncreaseUpgradeCnt;
            if (num < 18) return ItemType.IncreaseUpgradeCnt;
            if (num < 45) return ItemType.AffectionRecoveryGreat;
            if (num < 100) return ItemType.AffectionRecoveryBig;
            if (num < 160) return ItemType.CardParamsReRoll;
            if (num < 230) return ItemType.DereReRoll;
            if (num < 500) return ItemType.AffectionRecoveryNormal;
            if (num < 510) return ItemType.IncreaseExpSmall;
            return ItemType.AffectionRecoverySmall;
        }

        public ItemType RandomizeItemFromBlackMarket()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 2) return ItemType.IncreaseExpSmall;
            if (num < 12) return ItemType.BetterIncreaseUpgradeCnt;
            if (num < 25) return ItemType.IncreaseUpgradeCnt;
            if (num < 70) return ItemType.AffectionRecoveryGreat;
            if (num < 120) return ItemType.AffectionRecoveryBig;
            if (num < 180) return ItemType.CardParamsReRoll;
            if (num < 250) return ItemType.DereReRoll;
            if (num < 780) return ItemType.AffectionRecoveryNormal;
            return ItemType.AffectionRecoverySmall;
        }

        public ItemType RandomizeItemFromMarket()
        {
            var num = Fun.GetRandomValue(1000);
            if (num < 2) return ItemType.IncreaseExpSmall;
            if (num < 15) return ItemType.IncreaseUpgradeCnt;
            if (num < 80) return ItemType.AffectionRecoveryBig;
            if (num < 145) return ItemType.CardParamsReRoll;
            if (num < 230) return ItemType.DereReRoll;
            if (num < 480) return ItemType.AffectionRecoveryNormal;
            return ItemType.AffectionRecoverySmall;
        }

        public ItemWithCost[] GetItemsWithCost()
        {
            return new ItemWithCost[]
            {
                new ItemWithCost(3,     ItemType.AffectionRecoverySmall.ToItem()),
                new ItemWithCost(14,    ItemType.AffectionRecoveryNormal.ToItem()),
                new ItemWithCost(109,   ItemType.AffectionRecoveryBig.ToItem()),
                new ItemWithCost(29,    ItemType.DereReRoll.ToItem()),
                new ItemWithCost(79,    ItemType.CardParamsReRoll.ToItem()),
                new ItemWithCost(1099,  ItemType.IncreaseUpgradeCnt.ToItem()),
                new ItemWithCost(999,   ItemType.SetCustomImage.ToItem()),
                new ItemWithCost(659,   ItemType.SetCustomBorder.ToItem()),
                new ItemWithCost(149,   ItemType.ChangeStarType.ToItem()),
                new ItemWithCost(99,    ItemType.RandomBoosterPackSingleE.ToItem()),
                new ItemWithCost(1199,  ItemType.RandomTitleBoosterPackSingleE.ToItem()),
                new ItemWithCost(199,   ItemType.RandomNormalBoosterPackB.ToItem()),
                new ItemWithCost(499,   ItemType.RandomNormalBoosterPackA.ToItem()),
                new ItemWithCost(899,   ItemType.RandomNormalBoosterPackS.ToItem()),
                new ItemWithCost(1299,  ItemType.RandomNormalBoosterPackSS.ToItem()),
            };
        }

        public double GetExpToUpgrade(Card toUp, Card toSac, bool wild = false)
        {
            double rExp = 30f / (wild ? 100f : 10f);

            if (toUp.Character == toSac.Character && !wild)
                rExp = 30f;

            var sacVal = (int) toSac.Rarity;
            var upVal = (int) toUp.Rarity;
            var diff = upVal - sacVal;

            if (diff < 0)
            {
                diff = -diff;
                for (int i = 0; i < diff; i++) rExp /= 2;
            }
            else if (diff > 0)
            {
                for (int i = 0; i < diff; i++) rExp *= 1.5;
            }

            return rExp;
        }

        public FightWinner GetFightWinner(Card card1, Card card2)
        {
            var FAcard1 = GetFA(card1, card2);
            var FAcard2 = GetFA(card2, card1);

            var c1Health = card1.GetHealthWithPenalty();
            var c2Health = card2.GetHealthWithPenalty();
            var atkTk1 = c1Health / FAcard2;
            var atkTk2 = c2Health / FAcard1;

            var winner = FightWinner.Draw;
            if (atkTk1 > atkTk2 + 0.3) winner = FightWinner.Card1;
            if (atkTk2 > atkTk1 + 0.3) winner = FightWinner.Card2;

            return winner;
        }

        public double GetFA(Card target, Card enemy)
        {
            double atk1 = target.GetAttackWithBonus();
            if (!target.HasImage()) atk1 -= atk1 * 20 / 100;
            if (atk1 < 1) atk1 = 1;

            double def2 = enemy.GetDefenceWithBonus();
            if (!enemy.HasImage()) def2 -= def2 * 20 / 100;
            if (def2 < 1) def2 = 1;
            if (def2 > 99) def2 = 99;

            var realAtk1 = atk1 * (100 - def2) / 100;
            if (enemy.IsWeakTo(target.Dere)) realAtk1 *= 2;
            if (enemy.IsResistTo(target.Dere)) realAtk1 /= 2;
            if (realAtk1 < 1) realAtk1 = 1;

            return realAtk1;
        }

        public int RandomizeAttack(Rarity rarity)
            => Fun.GetRandomValue(rarity.GetAttackMin(), rarity.GetAttackMax() + 1);

        public int RandomizeDefence(Rarity rarity)
            => Fun.GetRandomValue(rarity.GetDefenceMin(), rarity.GetDefenceMax() + 1);

        public int RandomizeHealth(Card card)
            => Fun.GetRandomValue(card.Rarity.GetHealthMin(), card.GetHealthMax() + 1);

        public Dere RandomizeDere()
        {
            return Fun.GetOneRandomFrom(new List<Dere>()
            {
                Dere.Tsundere,
                Dere.Kamidere,
                Dere.Deredere,
                Dere.Yandere,
                Dere.Dandere,
                Dere.Kuudere,
                Dere.Mayadere,
                Dere.Bodere
            });
        }

        public Card GenerateNewCard(IUser user, ICharacterInfo character, Rarity rarity)
        {
            var card = new Card
            {
                Title = character?.Relations?.OrderBy(x => x.Id)?.FirstOrDefault()?.Title ?? "????",
                Defence = RandomizeDefence(rarity),
                ArenaStats = new CardArenaStats(),
                Attack = RandomizeAttack(rarity),
                TagList = new List<CardTag>(),
                CreationDate = DateTime.Now,
                Name = character.ToString(),
                StarStyle = StarStyle.Full,
                Source = CardSource.Other,
                Character = character.Id,
                Dere = RandomizeDere(),
                RarityOnStart = rarity,
                CustomBorder = null,
                CustomImage = null,
                IsTradable = true,
                FirstIdOwner = 1,
                UpgradesCnt = 2,
                LastIdOwner = 0,
                Rarity = rarity,
                Unique = false,
                InCage = false,
                RestartCnt = 0,
                Active = false,
                Affection = 0,
                Image = null,
                Health = 0,
                ExpCnt = 0,
            };

            if (user != null)
                card.FirstIdOwner = user.Id;

            if (character.HasImage)
                card.Image = character.PictureUrl;

            card.Health = RandomizeHealth(card);
            return card;
        }

        public Card GenerateNewCard(IUser user, ICharacterInfo character)
            => GenerateNewCard(user, character, RandomizeRarity());

        public Card GenerateNewCard(IUser user, ICharacterInfo character, List<Rarity> rarityExcluded)
            => GenerateNewCard(user, character, RandomizeRarity(rarityExcluded));

        private int ScaleNumber(int oMin, int oMax, int nMin, int nMax, int value)
        {
            var m = (double)(nMax - nMin)/(double)(oMax - oMin);
            var c = (oMin * m) - nMin;

            return (int)((m * value) - c);
        }

        public int GetAttactAfterLevelUp(Rarity oldRarity, int oldAtk)
        {
            var newRarity = oldRarity - 1;
            var newMax = newRarity.GetAttackMax();
            var newMin = newRarity.GetAttackMin();
            var range = newMax - newMin;

            var oldMax = oldRarity.GetAttackMax();
            var oldMin = oldRarity.GetAttackMin();

            var relNew = ScaleNumber(oldMin, oldMax, newMin, newMax, oldAtk);
            var relMin = relNew - (range * 6 / 100);
            var relMax = relNew + (range * 8 / 100);

            var nAtk = Fun.GetRandomValue(relMin, relMax + 1);
            if (nAtk > newMax) nAtk = newMax;
            if (nAtk < newMin) nAtk = newMin;

            return nAtk;
        }

        public int GetDefenceAfterLevelUp(Rarity oldRarity, int oldDef)
        {
            var newRarity = oldRarity - 1;
            var newMax = newRarity.GetDefenceMax();
            var newMin = newRarity.GetDefenceMin();
            var range = newMax - newMin;

            var oldMax = oldRarity.GetDefenceMax();
            var oldMin = oldRarity.GetDefenceMin();

            var relNew = ScaleNumber(oldMin, oldMax, newMin, newMax, oldDef);
            var relMin = relNew - (range * 6 / 100);
            var relMax = relNew + (range * 8 / 100);

            var nDef = Fun.GetRandomValue(relMin, relMax + 1);
            if (nDef > newMax) nDef = newMax;
            if (nDef < newMin) nDef = newMin;

            return nDef;
        }

        private int GetDmgDeal(Card c1, Card c2)
        {
            var dmg = GetFA(c1, c2);
            if (dmg < 1) dmg = 1;

            return (int)dmg;
        }

        public string GetDeathLog(FightHistory fight, List<PlayerInfo> players)
        {
            string deathLog = "";
            for (int i = 0; i < fight.Rounds.Count; i++)
            {
                var dead = fight.Rounds[i].Cards.Where(x => x.Hp <= 0);
                if (dead.Count() > 0)
                {
                    deathLog += $"**Runda {i + 1}**:\n";
                    foreach (var d in dead)
                    {
                        var thisCard = players.First(x => x.Cards.Any(c => c.Id == d.CardId)).Cards.First(x => x.Id == d.CardId);
                        deathLog += $"❌ {thisCard.GetString(true, false, true, true)}\n";
                    }
                    deathLog += "\n";
                }
            }
            return deathLog;
        }

        public IExecutable GetExecutableGMwK(FightHistory history, List<PlayerInfo> players)
        {
            return new Executable("GMwK", new Task(() =>
            {
                using (var db = new Database.UserContext(_config))
                {
                    bool isWinner = history.Winner != null;
                    foreach (var p in players)
                    {
                        var u = db.GetUserOrCreateAsync(p.User.Id).Result;
                        var stat = new CardPvPStats
                        {
                            Type = FightType.BattleRoyale,
                            Result = isWinner ? FightResult.Lose : FightResult.Draw
                        };

                        if (isWinner)
                        {
                            if (u.Id == history.Winner.User.Id)
                                stat.Result = FightResult.Win;
                        }

                        u.GameDeck.PvPStats.Add(stat);
                    }

                    db.SaveChanges();
                }
            }));
        }

        public async Task<FightHistory> MakeFightAsync(List<PlayerInfo> players, bool oneCard = false)
        {
            var totalCards = new List<Card>();
            await Task.CompletedTask;

            foreach (var player in players)
            {
                foreach (var card in player.Cards)
                {
                    card.Health = card.GetHealthWithPenalty();
                    totalCards.Add(card);
                }
            }

            var rounds = new List<RoundInfo>();
            bool fight = true;

            while (fight)
            {
                var round = new RoundInfo();
                totalCards = totalCards.Shuffle().ToList();

                foreach (var card in totalCards)
                {
                    if (card.Health <= 0)
                        continue;

                    var enemies = totalCards.Where(x => x.Health > 0 && x.GameDeckId != card.GameDeckId);
                    if (enemies.Count() > 0)
                    {
                        var target = Fun.GetOneRandomFrom(enemies);
                        var dmg = GetDmgDeal(card, target);
                        target.Health -= dmg;

                        var hpSnap = round.Cards.FirstOrDefault(x => x.CardId == target.Id);
                        if (hpSnap == null)
                        {
                            round.Cards.Add(new HpSnapshot
                            {
                                CardId = target.Id,
                                Hp = target.Health
                            });
                        }
                        else hpSnap.Hp = target.Health;

                        round.Fights.Add(new AttackInfo
                        {
                            Dmg = dmg,
                            AtkCardId = card.Id,
                            DefCardId = target.Id
                        });
                    }
                }

                rounds.Add(round);

                if (oneCard)
                {
                    fight = totalCards.Count(x => x.Health > 0) > 1;
                }
                else
                {
                    var alive = totalCards.Where(x => x.Health > 0);
                    var one = alive.FirstOrDefault();
                    if (one == null) break;

                    fight = alive.Any(x => x.GameDeckId != one.GameDeckId);
                }
            }

            PlayerInfo winner = null;
            var win = totalCards.Where(x => x.Health > 0).FirstOrDefault();

            if (win != null)
                winner = players.FirstOrDefault(x => x.Cards.Any(c => c.GameDeckId == win.GameDeckId));

            return new FightHistory(winner) { Rounds = rounds };
        }

        public Embed GetActiveList(IEnumerable<Card> list)
        {
            var embed = new EmbedBuilder()
            {
                Color = EMType.Info.Color(),
                Footer = new EmbedFooterBuilder().WithText($"MOC {list.Sum(x => x.GetCardPower()).ToString("F")}"),
                Description = "**Twoje aktywne karty to**:\n\n",
            };

            foreach(var card in list)
                embed.Description += $"**P:** {card.GetCardPower().ToString("F")} {card.GetString(false, false, true)}\n";

            return embed.Build();
        }

        public async Task<ICharacterInfo> GetRandomCharacterAsync()
        {
            int check = 2;
            if (CharId.IsNeedForUpdate())
            {
                var characters = await _shClient.Ex.GetAllCharactersFromAnimeAsync();
                if (!characters.IsSuccessStatusCode()) return null;

                CharId.Update(characters.Body);
            }

            ulong id = Fun.GetOneRandomFrom(CharId.GetIds());
            var response = await _shClient.GetCharacterInfoAsync(id);

            while (!response.IsSuccessStatusCode())
            {
                id = Fun.GetOneRandomFrom(CharId.GetIds());
                response = await _shClient.GetCharacterInfoAsync(id);

                await Task.Delay(TimeSpan.FromSeconds(2));

                if (check-- == 0)
                    return null;
            }
            return response.Body;
        }

        public async Task<string> GetWaifuProfileImageAsync(Card card, ITextChannel trashCh)
        {
            using (var cardImage = await _img.GetWaifuInProfileCardAsync(card))
            {
                cardImage.SaveToPath($"./GOut/Profile/P{card.Id}.png");

                using (var stream = cardImage.ToPngStream())
                {
                    var fs = await trashCh.SendFileAsync(stream, $"P{card.Id}.png");
                    var im = fs.Attachments.FirstOrDefault();
                    return im.Url;
                }
            }
        }

        public Embed GetWaifuFromCharacterSearchResult(string title, IEnumerable<Card> cards, DiscordSocketClient client)
        {
            string contentString = "";
            foreach (var card in cards)
            {
                var thU = client.GetUser(card.GameDeck.UserId);
                contentString += $"{thU?.Mention ?? "????"} **[{card.Id}]** **{card.Rarity}** {card.GetStatusIcons()}\n";
            }

            return new EmbedBuilder()
            {
                Color = EMType.Info.Color(),
                Description = $"{title}\n\n{contentString.TrimToLength(1850)}"
            }.Build();
        }

        public List<Embed> GetWaifuFromCharacterTitleSearchResult(IEnumerable<Card> cards, DiscordSocketClient client)
        {
            var list = new List<Embed>();
            var characters = cards.GroupBy(x => x.Character);

            string contentString = "";
            foreach (var cardsG in characters)
            {
                string tempContentString = $"\n**{cardsG.First().GetNameWithUrl()}**\n";
                foreach (var card in cardsG)
                {
                    var user = client.GetUser(card.GameDeckId);
                    var uString = user?.Mention ?? "????";

                    tempContentString += $"{uString}: **[{card.Id}]** **{card.Rarity}** {card.GetStatusIcons()}\n";
                }

                if ((contentString.Length + tempContentString.Length) <= 2000)
                {
                    contentString += tempContentString;
                }
                else
                {
                    list.Add(new EmbedBuilder()
                    {
                        Color = EMType.Info.Color(),
                        Description = contentString.TrimToLength(2000)
                    }.Build());

                    contentString = tempContentString;
                }
                tempContentString = "";
            }

            list.Add(new EmbedBuilder()
            {
                Color = EMType.Info.Color(),
                Description = contentString.TrimToLength(2000)
            }.Build());

            return list;
        }

        public Embed GetBoosterPackList(SocketUser user, List<BoosterPack> packs)
        {
            int groupCnt = 0;
            int startGroup = 1;
            string groupName = "";
            string packString = "";
            for (int i = 0; i < packs.Count + 1; i++)
            {
                if (i == packs.Count || groupName != packs[i].Name)
                {
                    if (groupName != "")
                    {
                        string count = groupCnt > 0 ? $"{startGroup}-{startGroup+groupCnt}" : $"{startGroup}";
                        packString += $"**[{count}]** {groupName}\n";
                    }
                    if (i != packs.Count)
                    {
                        groupName = packs[i].Name;
                        startGroup = i + 1;
                        groupCnt = 0;
                    }
                }
                else ++groupCnt;
            }

            return new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Description = $"{user.Mention} twoje pakiety:\n\n{packString.TrimToLength(1900)}"
            }.Build();
        }

        public Embed GetItemList(SocketUser user, List<Item> items)
        {
            return new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Description = $"{user.Mention} twoje przedmioty:\n\n{items.ToItemList().TrimToLength(1900)}"
            }.Build();
        }

        public async Task<List<Card>> OpenBoosterPackAsync(IUser user, BoosterPack pack)
        {
            var cardsFromPack = new List<Card>();

            for (int i = 0; i < pack.CardCnt; i++)
            {
                ICharacterInfo chara = null;
                if (pack.Characters.Count > 0)
                {
                    var id = pack.Characters.First();
                    if (pack.Characters.Count > 1)
                        id = Fun.GetOneRandomFrom(pack.Characters);

                    var res = await _shClient.GetCharacterInfoAsync(id.Character);
                    if (res.IsSuccessStatusCode()) chara = res.Body;
                }
                else if (pack.Title != 0)
                {
                    var res = await _shClient.Title.GetCharactersAsync(pack.Title);
                    if (res.IsSuccessStatusCode())
                    {
                        if (res.Body.Count > 0)
                        {
                            var id = Fun.GetOneRandomFrom(res.Body).CharacterId;
                            if (id.HasValue)
                            {
                                var response = await _shClient.GetCharacterInfoAsync(id.Value);
                                if (response.IsSuccessStatusCode()) chara = response.Body;
                            }
                        }
                    }
                }
                else
                {
                    chara = await GetRandomCharacterAsync();
                }

                if (chara != null)
                {
                    var newCard = GenerateNewCard(user, chara, pack.RarityExcludedFromPack.Select(x => x.Rarity).ToList());
                    if (pack.MinRarity != Rarity.E && i == pack.CardCnt - 1)
                        newCard = GenerateNewCard(user, chara, pack.MinRarity);

                    newCard.IsTradable = pack.IsCardFromPackTradable;
                    newCard.Source = pack.CardSourceFromPack;

                    cardsFromPack.Add(newCard);
                }
            }

            return cardsFromPack;
        }

        public async Task<string> GenerateAndSaveCardAsync(Card card, bool small = false)
        {
            string imageLocation = $"./GOut/Cards/{card.Id}.png";
            string sImageLocation = $"./GOut/Cards/Small/{card.Id}.png";

            using (var image = await _img.GetWaifuCardAsync(card))
            {
                image.SaveToPath(imageLocation);
                image.SaveToPath(sImageLocation, 133, 0);
            }

            return small ? sImageLocation : imageLocation;
        }

        public void DeleteCardImageIfExist(Card card)
        {
            string imageLocation = $"./GOut/Cards/{card.Id}.png";
            string sImageLocation = $"./GOut/Cards/Small/{card.Id}.png";

            try
            {
                if (File.Exists(imageLocation))
                    File.Delete(imageLocation);

                if (File.Exists(sImageLocation))
                    File.Delete(sImageLocation);
            }
            catch (Exception) {}
        }

        private async Task<string> GetCardUrlIfExistAsync(Card card, bool defaultStr = false, bool force = false)
        {
            string imageUrl = null;
            string imageLocation = $"./GOut/Cards/{card.Id}.png";
            string sImageLocation = $"./GOut/Cards/Small/{card.Id}.png";

            if (!File.Exists(imageLocation) || !File.Exists(sImageLocation) || force)
            {
                if (card.Id != 0)
                    imageUrl = await GenerateAndSaveCardAsync(card);
            }
            else
            {
                imageUrl = imageLocation;
                if ((DateTime.Now - File.GetCreationTime(imageLocation)).TotalHours > 4)
                    imageUrl = await GenerateAndSaveCardAsync(card);
            }

            return defaultStr ? (imageUrl ?? imageLocation) : imageUrl;
        }

        public SafariImage GetRandomSarafiImage()
        {
            SafariImage dImg = null;
            var reader = new Config.JsonFileReader($"./Pictures/Poke/List.json");
            try
            {
                var images = reader.Load<List<SafariImage>>();
                dImg = Fun.GetOneRandomFrom(images);
            }
            catch (Exception) { }

            return dImg;
        }

        public async Task<string> GetSafariViewAsync(SafariImage info, Card card, ITextChannel trashChannel)
        {
            string uri = info != null ? info.Uri(SafariImage.Type.Truth) : SafariImage.DefaultUri(SafariImage.Type.Truth);
            var cardUri = await GetCardUrlIfExistAsync(card);

            using (var cardImage = await _img.GetWaifuCardAsync(cardUri, card))
            {
                int posX = info != null ? info.GetX() : SafariImage.DefaultX();
                int posY = info != null ? info.GetY() : SafariImage.DefaultY();
                using (var pokeImage = _img.GetCatchThatWaifuImage(cardImage, uri, posX, posY))
                {
                    using (var stream = pokeImage.ToJpgStream())
                    {
                        var msg = await trashChannel.SendFileAsync(stream, $"poke.jpg");
                        return msg.Attachments.First().Url;
                    }
                }
            }
        }

        public async Task<string> GetSafariViewAsync(SafariImage info, ITextChannel trashChannel)
        {
            string uri = info != null ? info.Uri(SafariImage.Type.Mystery) : SafariImage.DefaultUri(SafariImage.Type.Mystery);
            var msg = await trashChannel.SendFileAsync(uri);
            return msg.Attachments.First().Url;
        }

        public async Task<string> GetArenaViewAsync(DuelInfo info, ITextChannel trashChannel)
        {
            string url = null;
            string imageUrlWinner = await GetCardUrlIfExistAsync(info.Winner, force: true);
            string imageUrlLooser = await GetCardUrlIfExistAsync(info.Loser, force: true);

            DuelImage dImg = null;
            var reader = new Config.JsonFileReader($"./Pictures/Duel/List.json");
            try
            {
                var images = reader.Load<List<DuelImage>>();
                dImg = Fun.GetOneRandomFrom(images);
            }
            catch (Exception) { }

            using (var winner = await _img.GetWaifuCardAsync(imageUrlWinner, info.Winner))
            {
                using (var looser = await _img.GetWaifuCardAsync(imageUrlLooser, info.Loser))
                {
                    using (var img = _img.GetDuelCardImage(info, dImg, winner, looser))
                    {
                        using (var stream = img.ToPngStream())
                        {
                            var msg = await trashChannel.SendFileAsync(stream, $"duel.png");
                            url = msg.Attachments.First().Url;
                        }
                    }
                }
            }

            return url;
        }

        public async Task<Embed> BuildCardViewAsync(Card card, ITextChannel trashChannel, SocketUser owner)
        {
            string imageUrl = await GetCardUrlIfExistAsync(card, true);
            if (imageUrl != null)
            {
                var msg = await trashChannel.SendFileAsync(imageUrl);
                imageUrl = msg.Attachments.First().Url;
            }

            string imgUrls = $"[_obrazek_]({imageUrl})\n[_możesz zmienić obrazek tutaj_]({card.GetCharacterUrl()}/edit_crossroad)";
            string ownerString = ((owner as SocketGuildUser)?.Nickname ?? owner?.Username) ?? "????";

            return new EmbedBuilder
            {
                ImageUrl = imageUrl,
                Color = EMType.Info.Color(),
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Należy do: {ownerString}"
                },
                Description = $"{card.GetDesc()}{imgUrls}".TrimToLength(1800)
            }.Build();
        }

        public Embed GetShopView(ItemWithCost[] items)
        {
            string embedString = "";
            for (int i = 0; i < items.Length; i++)
                embedString+= $"**[{i + 1}]** _{items[i].Item.Name}_ - {items[i].Cost} TC\n";

            return new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Description = $"**Sklepik:**\n\n{embedString}".TrimToLength(2000)
            }.Build();
        }

        public Embed GetItemShopInfo(ItemWithCost item)
        {
            return new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Description =$"**{item.Item.Name}**\n_{item.Item.Type.Desc()}_",
            }.Build();
        }

        public async Task<IEnumerable<Embed>> GetContentOfWishlist(List<ulong> cardsId, List<ulong> charactersId, List<ulong> titlesId)
        {
            var contentTable = new List<string>();
            if (cardsId.Count > 0) contentTable.Add($"**Karty:** {string.Join(", ", cardsId)}");

            foreach (var character in charactersId)
            {
                var res = await _shClient.GetCharacterInfoAsync(character);
                if (!res.IsSuccessStatusCode()) continue;

                contentTable.Add($"**P[{res.Body.Id}]** [{res.Body}]({res.Body.CharacterUrl})");
            }

            foreach (var title in titlesId)
            {
                var res = await _shClient.Title.GetInfoAsync(title);
                if (!res.IsSuccessStatusCode()) continue;

                var url = "https://shinden.pl/";
                if (res.Body is IAnimeTitleInfo ai) url = ai.AnimeUrl;
                else if (res.Body is IMangaTitleInfo mi) url = mi.MangaUrl;

                contentTable.Add($"**T[{res.Body.Id}]** [{res.Body}]({url})");
            }

            string temp = "";
            var content = new List<Embed>();
            for (int i = 0; i < contentTable.Count; i++)
            {
                if (temp.Length + contentTable[i].Length > 2000)
                {
                    content.Add(new EmbedBuilder()
                    {
                        Color = EMType.Info.Color(),
                        Description = temp
                    }.Build());
                    temp = contentTable[i];
                }
                else temp += $"\n{contentTable[i]}";
            }

            content.Add(new EmbedBuilder()
            {
                Color = EMType.Info.Color(),
                Description = temp
            }.Build());

            return content;
        }

        public async Task<IEnumerable<Card>> GetCardsFromWishlist(List<ulong> cardsId, List<ulong> charactersId, List<ulong> titlesId, Database.UserContext db, IEnumerable<Card> userCards)
        {
            var cards = new List<Card>();
            if (cardsId != null)
            {
                var cds = await db.Cards.Include(x => x.TagList).Where(x => cardsId.Any(c => c == x.Id)).AsNoTracking().ToListAsync();
                cards.AddRange(cds);
            }

            var characters = new List<ulong>();
            if (charactersId != null)
                characters.AddRange(charactersId);

            if (titlesId != null)
            {
                foreach (var id in titlesId)
                {
                    var response = await _shClient.Title.GetCharactersAsync(id);
                    if (response.IsSuccessStatusCode())
                        characters.AddRange(response.Body.Where(x => x.CharacterId.HasValue).Select(x => x.CharacterId.Value));
                }
            }

            if (characters.Count > 0)
            {
                characters = characters.Distinct().Where(c => !userCards.Any(x => x.Character == c)).ToList();
                var cads = await db.Cards.Include(x => x.TagList).Where(x => characters.Any(c => c == x.Character)).AsNoTracking().ToListAsync();
                cards.AddRange(cads);
            }

            return cards.Distinct().ToList();
        }
    }
}