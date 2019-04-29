using Microsoft.EntityFrameworkCore;
using Sanakan.Database.Models.Configuration;
using System.Threading.Tasks;

namespace Sanakan.Extensions
{
    public static class GuildConfigContextExtension
    {
        public static async Task<GuildOptions> GetGuildConfigOrCreate(this Database.GuildConfigContext context, ulong guildId)
        {
            var config = await context.Guilds.FirstOrDefaultAsync(x => x.Id == guildId);
            if (config == null)
            {
                config = new GuildOptions
                {
                    Id = guildId
                };
                await context.Guilds.AddAsync(config);
            }
            return config;
        }
    }
}
