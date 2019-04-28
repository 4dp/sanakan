#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models.Configuration;

namespace Sanakan.Database
{
    public class GuildConfigContext : DbContext
    {
        private IConfig _config;

        public GuildConfigContext(IConfig config) : base()
        {
            _config = config;
        }

        public DbSet<GuildOptions> Guilds { get; set; }
        public DbSet<LevelRole> LevelRoles { get; set; }
        public DbSet<CommandChannel> CommandChannels { get; set; }
        public DbSet<ModeratorRoles> ModeratorRoles { get; set; }
        public DbSet<WithoutExpChannel> WithoutExpChannels { get; set; }
        public DbSet<WithoutSupervisionChannel> WithoutSupervisionChannels { get; set; }
        public DbSet<MyLand> MyLands { get; set; }
        public DbSet<Waifu> Waifus { get; set; }
        public DbSet<WaifuCommandChannel> WaifuCommandChannels { get; set; }
        public DbSet<WaifuFightChannel> WaifuFightChannels { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies().UseMySql(_config.Get().ConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<CommandChannel>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.GuildOptions)
                    .WithMany(g => g.CommandChannels);
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
        }
    }
}
