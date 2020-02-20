#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public enum ExpContainerLevel
    {
        Disabled,
        Level1,
        Level2,
        Level3
    }

    public class ExpContainer
    {
        public ulong Id { get; set; }
        public double ExpCount { get; set; }
        public ExpContainerLevel Level { get; set; }

        public ulong GameDeckId { get; set; }
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }
    }
}
