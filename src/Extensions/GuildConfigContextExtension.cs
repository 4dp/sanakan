using Microsoft.EntityFrameworkCore;
using Sanakan.Database.Models.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Extensions
{
    public static class GuildConfigContextExtension
    {
        public static async Task<GuildOptions> GetGuildConfigOrCreateAsync(this Database.GuildConfigContext context, ulong guildId)
        {
            var config = await context.Guilds.Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).FirstOrDefaultAsync(x => x.Id == guildId);

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

        public static async Task<GuildOptions> GetCachedGuildFullConfigAsync(this Database.GuildConfigContext context, ulong guildId)
        {
            return (await context.Guilds.Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).FromCacheAsync(new string[] { $"config-{guildId}" })).FirstOrDefault(x => x.Id == guildId);
        }
    }
}
