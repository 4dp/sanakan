#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Sanakan.Config;
using Sanakan.Database.Models.Analytics;

namespace Sanakan.Database
{
    public class AnalyticsContext : DbContext
    {
        private IConfig _config;

        public AnalyticsContext(IConfig config) : base()
        {
            _config = config;
        }

        public DbSet<UserAnalytics> UsersData { get; set; }
        public DbSet<SystemAnalytics> SystemData { get; set; }
        public DbSet<TransferAnalytics> TransferData { get; set; }
        public DbSet<CommandsAnalytics> CommandsData { get; set; }
        public DbSet<WishlistCount> WishlistCountData { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_config.Get().ConnectionString,
                new MySqlServerVersion(new System.Version(5, 7)),
                mySqlOptions => mySqlOptions.CharSetBehavior(CharSetBehavior.NeverAppend));
        }
    }
}