#pragma warning disable 1591

using Discord;
using Sanakan.Database.Models;
using Sanakan.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Extensions
{
    public static class UserExtension
    {
        public static bool SendAnyMsgInMonth(this User u)
            => (u.MessagesCnt - u.MessagesCntAtDate) > 0;

        public static bool IsCharCounterActive(this User u)
            => DateTime.Now.Month == u.MeasureDate.Month && DateTime.Now.Year == u.MeasureDate.Year;

        public static bool IsPVPSeasonalRankActive(this GameDeck d)
            => DateTime.Now.Month == d.PVPSeasonBeginDate.Month && DateTime.Now.Year == d.PVPSeasonBeginDate.Year;

        public static bool IsPVPSeasonalRankActive(this User u)
            => u.GameDeck.IsPVPSeasonalRankActive();

        public static User Default(this User u, ulong id)
        {
            var user = new User
            {
                Id = id,
                Level = 1,
                TcCnt = 0,
                ScCnt = 100,
                ExpCnt = 10,
                Shinden = 0,
                MessagesCnt = 0,
                CommandsCnt = 0,
                MessagesCntAtDate = 0,
                IsBlacklisted = false,
                CharacterCntFromDate = 0,
                ShowWaifuInProfile = false,
                ProfileType = ProfileType.Stats,
                StatsReplacementProfileUri = "none",
                TimeStatuses = new List<TimeStatus>(),
                BackgroundProfileUri = $"./Pictures/defBg.png",
                MeasureDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                GameDeck = new GameDeck
                {
                    Id = id,
                    Waifu = 0,
                    CTCnt = 0,
                    Karma = 0,
                    PVPCoins = 0,
                    PVPWinStreak = 0,
                    ItemsDropped = 0,
                    GlobalPVPRank = 0,
                    SeasonalPVPRank = 0,
                    CardsInGallery = 10,
                    MatachMakingRatio = 0,
                    PVPDailyGamesPlayed = 0,
                    MaxNumberOfCards = 1000,
                    Items = new List<Item>(),
                    Cards = new List<Card>(),
                    ExchangeConditions = null,
                    BackgroundImageUrl = null,
                    WishlistIsPrivate = false,
                    Figures = new List<Figure>(),
                    Wishes = new List<WishlistObject>(),
                    PvPStats = new List<CardPvPStats>(),
                    BoosterPacks = new List<BoosterPack>(),
                    PVPSeasonBeginDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    ExpContainer = new ExpContainer
                    {
                        Id = id,
                        ExpCount = 0,
                        Level = ExpContainerLevel.Disabled
                    }
                },
                Stats = new UserStats
                {
                    Hit = 0,
                    Head = 0,
                    Misd = 0,
                    Tail = 0,
                    ScLost = 0,
                    IncomeInSc = 0,
                    RightAnswers = 0,
                    TotalAnswers = 0,
                    UpgaredCards = 0,
                    YamiUpgrades = 0,
                    YatoUpgrades = 0,
                    RaitoUpgrades = 0,
                    ReleasedCards = 0,
                    TurnamentsWon = 0,
                    UpgradedToSSS = 0,
                    UnleashedCards = 0,
                    SacraficeCards = 0,
                    DestroyedCards = 0,
                    WastedTcOnCards = 0,
                    SlotMachineGames = 0,
                    WastedTcOnCookies = 0,
                    OpenedBoosterPacks = 0,
                    WastedPuzzlesOnCards = 0,
                    WastedPuzzlesOnCookies = 0,
                    OpenedBoosterPacksActivity = 0,
                },
                SMConfig = new SlotMachineConfig
                {
                    PsayMode = 0,
                    Beat = SlotMachineBeat.b1,
                    Rows = SlotMachineSelectedRows.r1,
                    Multiplier = SlotMachineBeatMultiplier.x1,
                }
            };

            user.GameDeck.BoosterPacks.Add(new BoosterPack
            {
                CardCnt = 5,
                MinRarity = Rarity.A,
                Name = "Startowy pakiet",
                IsCardFromPackTradable = true
            });

            return user;
        }

        public static long CalculatePriceOfIncMaxCardCount(this GameDeck deck, long count)
        {
            long price = 0;
            var basePrice = 120;
            var f = deck.MaxNumberOfCards % 1000;
            var b = deck.MaxNumberOfCards / 1000;
            var maxOldPriceCnt = 10 - (f / 100);
            var bExp = (b - 1) * 0.2;
            var oldPriceCnt = count;

            if (count >= maxOldPriceCnt)
            {
                oldPriceCnt = maxOldPriceCnt;
                count -= maxOldPriceCnt;
            }
            else count = 0;

            price = (long)((oldPriceCnt * basePrice) * ((b + bExp) * b));
            var rmCnt = count / 10;
            for (var i = 1; i < rmCnt + 1; i++)
            {
                bExp = (++b - 1) * 0.2;
                price += (long)((10 * basePrice) * ((b + bExp) * b));
            }

            if (count > 0)
            {
                bExp = (++b - 1) * 0.2;
                price += (long)(((count - (rmCnt * 10)) * basePrice) * ((b + rmCnt + bExp) * b));
            }

            return price;
        }

        private const double PVPRankMultiplier = 0.45;

        public static string GetRankName(this GameDeck deck, long? rank = null)
        {
            switch ((ExperienceManager.CalculateLevel(rank ?? deck.SeasonalPVPRank, PVPRankMultiplier) / 10))
            {
                case var n when (n >= 17):
                    return "Konsul";

                case 16: return "Praetor";
                case 15: return "Legatus";
                case 14: return "Preafectus classis";
                case 13: return "Praefectus praetoria";
                case 12: return "Tribunus laticavius";
                case 11: return "Prefectus";
                case 10: return "Tribunus angusticlavius";
                case 9: return "Praefectus castorium";
                case 8: return "Primus pilus";
                case 7: return "Primi ordines";
                case 6: return "Centurio";
                case 5: return "Decurio";
                case 4: return "Tesserarius";
                case 3: return "Optio";
                case 2: return "Aquilifier";
                case 1: return "Signifer";

                default:
                    return "Miles gregarius";
            }
        }

        public static bool ReachedDailyMaxPVPCount(this GameDeck deck)
            => deck.PVPDailyGamesPlayed >= 10;

        public static int CanFightPvP(this GameDeck deck)
        {
            var power = deck.GetDeckPower();

            if (power > deck.GetMaxDeckPower()) return 1;
            if (power < deck.GetMinDeckPower()) return -1;
            return 0;
        }

        public static bool CanFightPvPs(this GameDeck deck)
            => CanFightPvP(deck) == 0;

        public static bool IsNearMMR(this GameDeck d1, GameDeck d2, double margin = 0.3)
        {
            var d1MMR = d1.MatachMakingRatio;
            var mDown = d2.MatachMakingRatio - margin;
            var mUp = d2.MatachMakingRatio + (margin * 1.2);

            return d1MMR >= mDown && d1MMR <= mUp;
        }

        public static long GetPVPCoinsFromDuel(this GameDeck deck, FightResult res)
        {
            var step = (ExperienceManager.CalculateLevel(deck.SeasonalPVPRank, PVPRankMultiplier) / 10);
            if (step > 5) step = 5;

            var coinCnt = 40 + (20 * step);
            return (res == FightResult.Win) ? coinCnt : coinCnt / 2;
        }

        public static string CalculatePVPParams(this GameDeck d1, GameDeck d2, FightResult res)
        {
            ++d1.PVPDailyGamesPlayed;

            var mmrDif = d1.MatachMakingRatio - d2.MatachMakingRatio;
            var chanceD1 = 1 / (1 + Math.Pow(10, -mmrDif / 40f));
            var chanceD2 = 1 / (1 + Math.Pow(10, mmrDif / 40f));

            var sDif = d1.SeasonalPVPRank - d2.SeasonalPVPRank;
            var sChan = 1 / (1 + Math.Pow(10, -sDif / 400f));

            var gDif = d1.GlobalPVPRank - d2.GlobalPVPRank;
            var gChan = 1 / (1 + Math.Pow(10, -gDif / 400f));

            long gRank = 0;
            long sRank = 0;

            double mmrChange = 0;
            double mmreChange = 0;

            switch (res)
            {
                case FightResult.Win:
                    ++d1.PVPWinStreak;

                    var wsb = 20 * (1 + (d1.PVPWinStreak / 10));
                    if (wsb < 20) wsb = 20;
                    if (wsb > 40) wsb = 40;

                    sRank = (long) (80 * (1 - sChan)) + wsb;
                    gRank = (long) (40 * (1 - gChan)) + wsb;

                    mmrChange = 2 * (1 - chanceD1);
                    mmreChange = 2 * (0 - chanceD2);
                break;

                case FightResult.Lose:
                    d1.PVPWinStreak = 0;
                    sRank = (long) (80 * (0 - sChan));
                    gRank = (long) (40 * (0 - gChan));

                    mmrChange = 2 * (0 - chanceD1);
                    mmreChange = 2 * (1 - chanceD2);
                break;

                case FightResult.Draw:
                    sRank = (long) (40 * (1 - sChan));
                    gRank = (long) (20 * (1 - gChan));

                    mmrChange = 1 * (1 - chanceD1);
                    mmreChange = 1 * (1 - chanceD2);
                break;
            }

            d1.MatachMakingRatio += mmrChange;
            d2.MatachMakingRatio += mmreChange;

            d1.GlobalPVPRank += gRank;
            d1.SeasonalPVPRank += sRank;

            if (d1.GlobalPVPRank < 0)
                d1.GlobalPVPRank = 0;

            if (d1.SeasonalPVPRank < 0)
                d1.SeasonalPVPRank = 0;

            var coins = d1.GetPVPCoinsFromDuel(res);
            d1.PVPCoins += coins;

            return $"**{coins.ToString("+0;-#")}** PC **{gRank.ToString("+0;-#")}** GR  **{sRank.ToString("+0;-#")}** SR";
        }

        public static double GetDeckPower(this GameDeck deck)
            => deck.Cards.Where(x => x.Active).Sum(x => x.GetCardPower());

        public static double GetMaxDeckPower(this GameDeck _) => 800;

        public static double GetMinDeckPower(this GameDeck _) => 200;

        public static int LimitOfCardsOnExpedition(this GameDeck _) => 10;

        public static string GetUserNameStatus(this GameDeck deck)
        {
            if (deck.Karma >= 2000) return $"Papaj";
            if (deck.Karma >= 1600) return $"Miłościwy kumpel";
            if (deck.Karma >= 1200) return $"Oślepiony bugiem";
            if (deck.Karma >= 800) return $"Pan pokoiku";
            if (deck.Karma >= 400) return $"Błogosławiony rycerz";
            if (deck.Karma >= 200) return $"Pionek buga";
            if (deck.Karma >= 100) return $"Sługa buga";
            if (deck.Karma >= 50) return $"Biały koleś";
            if (deck.Karma >= 10) return $"Pantofel";
            if (deck.Karma >= 5) return $"Lizus";
            if (deck.Karma <= -2000) return $"Mroczny panocek";
            if (deck.Karma <= -1600) return $"Nienawistny koleżka";
            if (deck.Karma <= -1200) return $"Mściwy ślepiec";
            if (deck.Karma <= -800) return $"Pan wojenki";
            if (deck.Karma <= -400) return $"Przeklęty rycerz";
            if (deck.Karma <= -200) return $"Ciemny pionek";
            if (deck.Karma <= -100) return $"Sługa mroku";
            if (deck.Karma <= -50) return $"Murzynek";
            if (deck.Karma <= -10) return $"Rzezimieszek";
            if (deck.Karma <= -5) return $"Buntownik";
            return "Wieśniak";
        }

        public static bool CanCreateDemon(this GameDeck deck) => deck.Karma <= -2000;

        public static bool CanCreateAngel(this GameDeck deck) => deck.Karma >= 2000;

        public static bool IsMarketDisabled(this GameDeck deck) => deck.Karma <= -400;

        public static bool IsBlackMarketDisabled(this GameDeck deck) => deck.Karma > -400;

        public static bool IsEvil(this GameDeck deck) => deck.Karma <= -10;

        public static bool IsGood(this GameDeck deck) => deck.Karma >= 10;

        public static bool IsNeutral(this GameDeck deck) => IsKarmaNeutral(deck.Karma);

        public static bool IsKarmaNeutral(this double karma) => karma > -10 && karma < 10;

        public static double AffectionFromKarma(this GameDeck deck)
        {
            var karmaDif = deck.Karma / 150d;
            if (karmaDif < -6) karmaDif = -6;
            if (karmaDif > 6) karmaDif = 6;
            return karmaDif;
        }

        public static double GetStrongestCardPower(this GameDeck deck)
        {
            return deck.Cards.OrderByDescending(x => x.GetCardPower()).FirstOrDefault()?.GetCardPower() ?? 0;
        }

        public static List<ulong> GetTitlesWishList(this GameDeck deck)
        {
            return deck.Wishes.Where(x => x.Type == WishlistObjectType.Title).Select(x => x.ObjectId).ToList();
        }

        public static List<ulong> GetCardsWishList(this GameDeck deck)
        {
            return deck.Wishes.Where(x => x.Type == WishlistObjectType.Card).Select(x => x.ObjectId).ToList();
        }

        public static List<ulong> GetCharactersWishList(this GameDeck deck)
        {
            return deck.Wishes.Where(x => x.Type == WishlistObjectType.Character).Select(x => x.ObjectId).ToList();
        }

        public static void RemoveCharacterFromWishList(this GameDeck deck, ulong id)
        {
            var en = deck.Wishes.FirstOrDefault(x => x.Type == WishlistObjectType.Character && x.ObjectId == id);
            if (en != null) deck.Wishes.Remove(en);
        }

        public static void RemoveFromWaifu(this GameDeck deck, Card card)
        {
            if (deck.Waifu == card.Character)
            {
                card.Affection -= 25;
                deck.Waifu = 0;
            }
        }

        public static void RemoveCardFromWishList(this GameDeck deck, ulong id)
        {
            var en = deck.Wishes.FirstOrDefault(x => x.Type == WishlistObjectType.Card && x.ObjectId == id);
            if (en != null) deck.Wishes.Remove(en);
        }

        public static EmbedBuilder GetStatsView(this User u, IUser user)
        {
            string stats = $"**Wiadomości**: {u.MessagesCnt}\n**Polecenia:** {u.CommandsCnt}";

            return new EmbedBuilder
            {
                Color = EMType.Info.Color(),
                Description = $"**Statystyki** {user.Mention}:\n\n{stats}\n{u.Stats.ToView()}".TrimToLength(1950)
            };
        }

        public static long GetRemainingExp(this User u)
        {
            var nextLvlExp = ExperienceManager.CalculateExpForLevel(u.Level + 1);
            var exp = nextLvlExp - u.ExpCnt;
            if (exp < 1) exp = 1;

            return exp;
        }

        public static string GetViewValueForTop(this User u, TopType type)
        {
            switch (type)
            {
                default:
                case TopType.Level:
                    return $"{u.Level} **LVL** ({u.ExpCnt} **EXP**)";

                case TopType.ScCnt:
                    return $"{u.ScCnt} **SC**";

                case TopType.TcCnt:
                    return $"{u.TcCnt} **TC**";

                case TopType.Posts:
                    return $"{u.MessagesCnt}";

                case TopType.PostsMonthly:
                    return $"{u.MessagesCnt - u.MessagesCntAtDate}";

                case TopType.PostsMonthlyCharacter:
                    return $"{u.CharacterCntFromDate / (u.MessagesCnt - u.MessagesCntAtDate)}";

                case TopType.Commands:
                    return $"{u.CommandsCnt}";

                case TopType.Card:
                    return u.GameDeck.Cards.OrderByDescending(x => x.GetCardPower())?.FirstOrDefault()?.GetString(false, false, true) ?? "---";

                case TopType.Cards:
                    return $"{u.GameDeck.Cards.Count}";

                case TopType.CardsPower:
                    return u.GameDeck.GetCardCountStats();

                case TopType.Karma:
                case TopType.KarmaNegative:
                    return $"{u.GameDeck.Karma.ToString("F")}";

                case TopType.Pvp:
                    return $"{u.GameDeck.GlobalPVPRank}";

                case TopType.PvpSeason:
                    return $"{u.GameDeck.SeasonalPVPRank}";
            }
        }

        public static string GetCardCountStats(this GameDeck deck)
        {
            string stats = "";

            foreach (Rarity rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
            {
                var count = deck.Cards.Count(x => x.Rarity == rarity);
                if (count > 0) stats += $"**{rarity.ToString().ToUpper()}**: {count} ";
            }

            return stats;
        }

        public static bool ApplySlotMachineSetting(this User user, SlotMachineSetting type, string value)
        {
            try
            {
                switch (type)
                {
                    case SlotMachineSetting.Beat:
                            var bt = (SlotMachineBeat)Enum.Parse(typeof(SlotMachineBeat), $"b{value}");
                            user.SMConfig.Beat = bt;
                        break;
                    case SlotMachineSetting.Rows:
                            var rw = (SlotMachineSelectedRows)Enum.Parse(typeof(SlotMachineSelectedRows), $"r{value}");
                            user.SMConfig.Rows = rw;
                        break;
                    case SlotMachineSetting.Multiplier:
                            var mt = (SlotMachineBeatMultiplier)Enum.Parse(typeof(SlotMachineBeatMultiplier), $"x{value}");
                            user.SMConfig.Multiplier = mt;
                        break;

                    default:
                        return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void StoreExpIfPossible(this User user, double exp)
        {
            var maxToTransfer = user.GameDeck.ExpContainer.GetMaxExpTransferToChest();
            if (maxToTransfer != -1)
            {
                exp = Math.Floor(exp);
                var diff = maxToTransfer - user.GameDeck.ExpContainer.ExpCount;
                if (diff <= exp) exp = Math.Floor(diff);
                if (exp < 0) exp = 0;
            }
            user.GameDeck.ExpContainer.ExpCount += exp;
        }
    }
}
