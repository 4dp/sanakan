#pragma warning disable 1591

using Sanakan.Database.Models;
using Shinden.Models;

namespace Sanakan.Services.PocketWaifu
{
    public class CardInfo
    {
        public Card Card { get; set; }
        public ICharacterInfo Info { get; set; }
    }
}