#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Configuration;
using Sanakan.Database.Models.Tower;

namespace Sanakan.Database
{
    public class UserContext : DbContext
    {
        private IConfig _config;

        public UserContext(IConfig config) : base()
        {
            _config = config;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserStats> UsersStats { get; set; }
        public DbSet<TimeStatus> TimeStatuses { get; set; }
        public DbSet<SlotMachineConfig> SlotMachineConfigs { get; set; }
        public DbSet<GameDeck> GameDecks { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<BoosterPack> BoosterPacks { get; set; }
        public DbSet<CardPvPStats> CardPvPStats { get; set; }
        public DbSet<CardArenaStats> CardArenaStats { get; set; }
        public DbSet<BoosterPackCharacter> BoosterPackCharacters { get; set; }
        public DbSet<RarityExcluded> RaritysExcludedFromPacks { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        public DbSet<TowerProfile> TProfiles { get; set; }
        public DbSet<TowerItem> TItems { get; set; }
        public DbSet<Effect> TEffects { get; set; }
        public DbSet<Enemy> TEnemies { get; set; }
        public DbSet<Spell> TSpells { get; set; }
        public DbSet<Floor> TFloors { get; set; }
        public DbSet<Room> TRooms { get; set; }

        public DbSet<EffectInProfile> TProfileEffects { get; set; }
        public DbSet<SpellInProfile> TProfileSpells { get; set; }
        public DbSet<ItemInProfile> TProfileItems { get; set; }
        public DbSet<RoomConnection> TConnections { get; set; }
        public DbSet<SpellInEnemy> TEnemySpells { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_config.Get().ConnectionString);
        }
    }
}
