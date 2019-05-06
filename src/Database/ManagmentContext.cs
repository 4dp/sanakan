#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Sanakan.Config;
using Sanakan.Database.Models.Management;

namespace Sanakan.Database
{
    public class ManagmentContext : DbContext
    {
        private IConfig _config;

        public ManagmentContext(IConfig config) : base()
        {
            _config = config;
        }

        public DbSet<PenaltyInfo> Penalties { get; set; }
        public DbSet<OwnedRole> OwnedRoles { get; set; }
        public DbSet<LeaverInfo> Leavers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_config.Get().ConnectionString);
        }
    }
}