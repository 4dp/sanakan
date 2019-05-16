#pragma warning disable 1591

namespace Sanakan.Database.Models
{
    public enum Rarity
    {
        SS, S, A, B, C, D, E
    }

    public enum Dere
    {
        Tsundere, Kamidere, Deredere, Yandere, Dandere, Kuudere, Mayadere, Bodere
    }

    public class Card
    {
        public ulong Id { get; set; }
        public bool Active { get; set; }
        public bool InCage { get; set; }
        public bool IsTradable { get; set; }
        public double ExpCnt { get; set; }
        public double Affection { get; set; }
        public int UpgradesCnt { get; set; }
        public Rarity Rarity { get; set; }
        public Dere Dere { get; set; }
        public int Defence { get; set; }
        public int Attack { get; set; }
        public string Name { get; set; }
        public ulong Character { get; set; }

        public virtual CardArenaStats ArenaStats { get; set; }

        public ulong GameDeckId { get; set; }
        public virtual GameDeck GameDeck { get; set; }
    }
}
