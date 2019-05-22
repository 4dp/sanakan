#pragma warning disable 1591

namespace Sanakan.Database.Models
{
    public enum FightType
    {
        Versus, BattleRoyale
    }

    public enum FightResult
    {
        Win, Lose, Draw
    }

    public class CardPvPStats
    {
        public ulong Id { get; set; }
        public FightType Type { get; set; }
        public FightResult Result { get; set; }

        public ulong GameDeckId { get; set; }
        public virtual GameDeck GameDeck { get; set; }
    }
}
