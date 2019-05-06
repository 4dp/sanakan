namespace Sanakan.Database.Models
{
    public class BoosterPackCharacter
    {
        public ulong Id { get; set; }
        public ulong Character { get; set; }

        public ulong BoosterPackId { get; set; }
        public virtual BoosterPack BoosterPack { get; set; }
    }
}
