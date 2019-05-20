#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Services;
using System;
using System.Collections.Generic;

namespace Sanakan.Extensions
{
    public static class UserExtension
    {
        public static bool IsCharCounterActive(this User u)
            => DateTime.Now.Month == u.MeasureDate.Month && DateTime.Now.Year == u.MeasureDate.Year;

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
                ProfileType = ProfileType.Stats,
                StatsReplacementProfileUri = "none",
                GameDeck = new GameDeck 
                { 
                    Waifu = 0,
                    Cards = new List<Card>(),
                    BoosterPacks = new List<BoosterPack>(),
                },
                BackgroundProfileUri = $"./Pictures/defBg.png",
                MeasureDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                Stats = new UserStats
                {
                    Hit = 0,
                    Head = 0,
                    Misd = 0,
                    Tail = 0,
                    ScLost = 0,
                    IncomeInSc = 0,
                    SlotMachineGames = 0,
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
                CardCnt = 3,
                MinRarity = Rarity.C,
                Name = "Startowy pakiet",
                IsCardFromPackTradable = true
            });

            return user;
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
                    return $"{u.MessagesCntAtDate - u.MessagesCnt}";

                case TopType.PostsMonthlyCharacter:
                    return $"{u.CharacterCntFromDate / (u.MessagesCntAtDate - u.MessagesCnt)} znaki";

                case TopType.Commands:
                    return $"{u.CommandsCnt}";

                case TopType.Cards:
                    return $"{u.GameDeck.Cards.Count}";
            }
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
    }
}
