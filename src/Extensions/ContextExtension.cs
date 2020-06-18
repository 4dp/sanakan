#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Configuration;
using Sanakan.Database.Models.Management;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Extensions
{
    public static class ContextExtension
    {
        public static async Task<GuildOptions> GetGuildConfigOrCreateAsync(this Database.GuildConfigContext context, ulong guildId)
        {
            var config = await context.Guilds.Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels).Include(x => x.Raports)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).FirstOrDefaultAsync(x => x.Id == guildId);

            if (config == null)
            {
                config = new GuildOptions
                {
                    Id = guildId,
                    SafariLimit = 50
                };
                await context.Guilds.AddAsync(config);
            }
            return config;
        }

        public static async Task<GuildOptions> GetCachedGuildFullConfigAsync(this Database.GuildConfigContext context, ulong guildId)
        {
            return (await context.Guilds.Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels).Include(x => x.Raports)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).AsNoTracking().FromCacheAsync(new string[] { $"config-{guildId}" })).FirstOrDefault(x => x.Id == guildId);
        }

        public static async Task<IEnumerable<PenaltyInfo>> GetCachedFullPenalties(this Database.ManagmentContext context)
        {
            return (await context.Penalties.Include(x => x.Roles).AsNoTracking().FromCacheAsync(new string[] { $"mute" })).ToList();
        }

        public static async Task<User> GetCachedFullUserAsync(this Database.UserContext context, ulong userId)
        {
            return (await context.Users.Where(x => x.Id == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Wishes).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .AsNoTracking().FromCacheAsync(new string[] { $"user-{userId}", "users" })).FirstOrDefault();
        }

        public static async Task<User> GetCachedFullUserByShindenIdAsync(this Database.UserContext context, ulong userId)
        {
            return (await context.Users.Where(x => x.Shinden == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Wishes).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .AsNoTracking().FromCacheAsync(new string[] { $"user-{userId}", "users" })).FirstOrDefault();
        }

        public static async Task<List<User>> GetCachedAllUsersAsync(this Database.UserContext context)
        {
            return (await context.Users.Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .AsNoTracking().FromCacheAsync(new string[] { $"users" })).ToList();
        }

        public static async Task<List<GameDeck>> GetCachedPlayersWithActiveCards(this Database.UserContext context)
        {
            return (await context.Cards.Where(x => x.Active).Include(x => x.GameDeck).Select(x => x.GameDeck).Include(x => x.Cards).AsNoTracking().FromCacheAsync(new string[] { $"users" })).ToList();
        }

        public static async Task<User> GetUserOrCreateAsync(this Database.UserContext context, ulong userId)
        {
            var user = await context.Users.Where(x => x.Id == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                user = user.Default(userId);
                await context.Users.AddAsync(user);
            }

            return user;
        }

        public static async Task<User> GetUserAndDontTrackAsync(this Database.UserContext context, ulong userId)
        {
            return await context.Users.Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        }

        public static async Task<List<Question>> GetCachedAllQuestionsAsync(this Database.UserContext context)
        {
            return (await context.Questions.Include(x => x.Answers).AsNoTracking().FromCacheAsync(new string[] { $"quiz" })).ToList();
        }

        public static async Task<Question> GetCachedQuestionAsync(this Database.UserContext context, ulong id)
        {
            return (await context.Questions.Include(x => x.Answers).AsNoTracking().FromCacheAsync(new string[] { $"quiz" })).FirstOrDefault(x => x.Id == id);
        }
    }
}
