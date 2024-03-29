﻿#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Configuration;

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
        public DbSet<ExpContainer> ExpContainers { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<CardTag> CardTags { get; set; }
        public DbSet<BoosterPack> BoosterPacks { get; set; }
        public DbSet<CardPvPStats> CardPvPStats { get; set; }
        public DbSet<CardArenaStats> CardArenaStats { get; set; }
        public DbSet<BoosterPackCharacter> BoosterPackCharacters { get; set; }
        public DbSet<WishlistObject> Wishes { get; set; }
        public DbSet<RarityExcluded> RaritysExcludedFromPacks { get; set; }
        public DbSet<Figure> Figures { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Models.Analytics.WishlistCount> WishlistCountData { get; set; }

        public DbSet<GuildOptions> Guilds { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_config.Get().ConnectionString,
                new MySqlServerVersion(new System.Version(5, 7)),
                mySqlOptions => mySqlOptions.CharSetBehavior(CharSetBehavior.NeverAppend));
        }
    }
}
