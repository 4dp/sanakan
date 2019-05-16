#pragma warning disable 1591

using Sanakan.Database.Models;

namespace Sanakan.Services.PocketWaifu
{
    public class ItemWithCost
    {
        public ItemWithCost(int cost, Item item)
        {
            Cost = cost;
            Item = item;
        }

        public Item Item { get; set; }
        public int Cost { get; set; }
    }
}