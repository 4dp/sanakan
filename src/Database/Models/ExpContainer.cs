#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public enum ExpContainerLevel
    {
        Disabled,
        Max100Exp,
        Max500Exp,
        Unlimited
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
