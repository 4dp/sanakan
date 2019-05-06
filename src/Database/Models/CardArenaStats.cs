namespace Sanakan.Database.Models
{
    public class CardArenaStats
    {
        public ulong Id { get; set; }
        public long Wins { get; set; }
        public long Loses { get; set; }
        public long Draws { get; set; }

        public ulong CardId { get; set; }
        public virtual Card Card { get; set; }
    }
}
