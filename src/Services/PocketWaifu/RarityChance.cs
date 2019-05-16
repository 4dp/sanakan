#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Services.PocketWaifu
{
    public class RarityChance
    {
        public RarityChance(int chance, Rarity rarity)
        {
            Chance = chance;
            Rarity = rarity;
        }

        public Rarity Rarity { get; set; }
        public int Chance { get; set; }
    }
}