#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Analytics;
using Sanakan.Database.Models.Configuration;
using Sanakan.Database.Models.Management;
using System;
using Z.EntityFramework.Plus;

namespace Sanakan.Database
{
    public class BuildDatabaseContext : DbContext
    {
        private IConfig _config;

        public BuildDatabaseContext(IConfig config) : base()
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
        public DbSet<Figure> Figures { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<RarityExcluded> RaritysExcludedFromPacks { get; set; }
        public DbSet<SelfRole> SelfRoles { get; set; }
        public DbSet<GuildOptions> Guilds { get; set; }
        public DbSet<LevelRole> LevelRoles { get; set; }
        public DbSet<CommandChannel> CommandChannels { get; set; }
        public DbSet<ModeratorRoles> ModeratorRoles { get; set; }
        public DbSet<WithoutExpChannel> WithoutExpChannels { get; set; }
        public DbSet<WithoutMsgCntChannel> IgnoredChannels { get; set; }
        public DbSet<WithoutSupervisionChannel> WithoutSupervisionChannels { get; set; }
        public DbSet<MyLand> MyLands { get; set; }
        public DbSet<Waifu> Waifus { get; set; }
        public DbSet<Raport> Raports { get; set; }
        public DbSet<WaifuCommandChannel> WaifuCommandChannels { get; set; }
        public DbSet<WaifuFightChannel> WaifuFightChannels { get; set; }
        public DbSet<PenaltyInfo> Penalties { get; set; }
        public DbSet<OwnedRole> OwnedRoles { get; set; }
        public DbSet<UserAnalytics> UsersData { get; set; }
        public DbSet<SystemAnalytics> SystemData { get; set; }
        public DbSet<TransferAnalytics> TransferData { get; set; }
        public DbSet<CommandsAnalytics> CommandsData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            QueryCacheManager.DefaultMemoryCacheEntryOptions = new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromHours(4),
                AbsoluteExpirationRelativeToNow  = TimeSpan.FromHours(24)
            };
            optionsBuilder.UseMySql(_config.Get().ConnectionString,
                new MySqlServerVersion(new System.Version(5, 7)),
                mySqlOptions => mySqlOptions.CharSetBehavior(CharSetBehavior.NeverAppend));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Answer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Question)
                    .WithMany(u => u.Answers);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<UserStats>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithOne(u => u.Stats);
            });

            modelBuilder.Entity<SlotMachineConfig>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithOne(u => u.SMConfig);
            });

            modelBuilder.Entity<TimeStatus>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.TimeStatuses);
            });

            modelBuilder.Entity<GameDeck>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                    .WithOne(u => u.GameDeck);
            });

            modelBuilder.Entity<ExpContainer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithOne(u => u.ExpContainer);
            });

            modelBuilder.Entity<Figure>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(u => u.Figures);
            });

            modelBuilder.Entity<Card>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(d => d.Cards);
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(d => d.Items);
            });

            modelBuilder.Entity<WishlistObject>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(d => d.Wishes);
            });

            modelBuilder.Entity<BoosterPack>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(d => d.BoosterPacks);
            });

            modelBuilder.Entity<CardPvPStats>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GameDeck)
                    .WithMany(d => d.PvPStats);
            });

            modelBuilder.Entity<CardTag>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Card)
                    .WithMany(d => d.TagList);
            });

            modelBuilder.Entity<CardArenaStats>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Card)
                    .WithOne(c => c.ArenaStats);
            });

            modelBuilder.Entity<BoosterPackCharacter>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.BoosterPack)
                    .WithMany(p => p.Characters);
            });

            modelBuilder.Entity<RarityExcluded>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.BoosterPack)
                    .WithMany(p => p.RarityExcludedFromPack);
            });

            // GuildConfig
            modelBuilder.Entity<GuildOptions>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Waifu>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasMany(e => e.CommandChannels)
                    .WithOne(w => w.Waifu);
                entity.HasMany(e => e.FightChannels)
                    .WithOne(w => w.Waifu);
                entity.HasOne(e => e.GuildOptions)
                    .WithOne(g => g.WaifuConfig);
            });

            modelBuilder.Entity<Raport>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.Raports);
            });

            modelBuilder.Entity<SelfRole>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.SelfRoles);
            });

            modelBuilder.Entity<CommandChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.CommandChannels);
            });

            modelBuilder.Entity<WithoutMsgCntChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.IgnoredChannels);
            });

            modelBuilder.Entity<LevelRole>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.RolesPerLevel);
            });

            modelBuilder.Entity<ModeratorRoles>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.ModeratorRoles);
            });

            modelBuilder.Entity<WithoutExpChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.ChannelsWithoutExp);
            });

            modelBuilder.Entity<WithoutSupervisionChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.ChannelsWithoutSupervision);
            });

            modelBuilder.Entity<MyLand>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                     .WithMany(g => g.Lands);
            });

            modelBuilder.Entity<WaifuCommandChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Waifu)
                    .WithMany(w => w.CommandChannels);
            });

            modelBuilder.Entity<WaifuFightChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Waifu)
                    .WithMany(w => w.FightChannels);
            });

            // Managment
            modelBuilder.Entity<PenaltyInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<OwnedRole>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.PenaltyInfo)
                    .WithMany(p => p.Roles);
            });

            // Analytics
            modelBuilder.Entity<UserAnalytics>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<SystemAnalytics>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<TransferAnalytics>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<CommandsAnalytics>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }
}
