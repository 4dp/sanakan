using Sanakan.Database.Models;
using System;

namespace Sanakan.Extensions
{
    public static class UserExtension
    {
        public static User Default(this User u, ulong id)
        {
            return new User
            {
                Id = id,
                Level = 1,
                TcCnt = 0,
                ScCnt = 100,
                ExpCnt = 10,
                Shinden = 0,
                MessagesCnt = 0,
                MessagesCntAtDate = 0,
                IsBlacklisted = false,
                CharacterCntFromDate = 0,
                MeasureDate = DateTime.Now,
                ProfileType = ProfileType.Stats,
                StatsReplacementProfileUri = "none",
                BackgroundProfileUri = $"defBg.png",
                GameDeck = new GameDeck { Waifu = 0 },
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
        }
    }
}
