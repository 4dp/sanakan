#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class UserStatsExtension
    {
        public static string ToView(this UserStats stats)
        {
            if (stats == null) return "";

            return $"**Stracone SC**: {stats.ScLost}\n**Dochód SC**: {stats.IncomeInSc}\n**Gier na automacie**: {stats.SlotMachineGames}\n"
                + $"**Rzutów monetą**: {stats.Tail + stats.Head}\n**-Trafień**: {stats.Hit}\n**-Pudeł**: {stats.Misd}";
        }
    }
}
