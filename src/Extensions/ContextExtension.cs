#pragma warning disable 1591

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Configuration;
using Sanakan.Database.Models.Management;
using System;
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
            var config = await context.Guilds.AsQueryable().Include(x => x.IgnoredChannels).Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels).Include(x => x.Raports)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == guildId);

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
            return (await context.Guilds.AsQueryable().Include(x => x.IgnoredChannels).Include(x => x.ChannelsWithoutExp).Include(x => x.ChannelsWithoutSupervision).Include(x => x.CommandChannels).Include(x => x.SelfRoles)
                .Include(x => x.Lands).Include(x => x.ModeratorRoles).Include(x => x.RolesPerLevel).Include(x => x.WaifuConfig).ThenInclude(x => x.CommandChannels).Include(x => x.Raports)
                .Include(x => x.WaifuConfig).ThenInclude(x => x.FightChannels).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"config-{guildId}" })).FirstOrDefault(x => x.Id == guildId);
        }

        public static async Task<IEnumerable<PenaltyInfo>> GetCachedFullPenalties(this Database.ManagmentContext context)
        {
            return (await context.Penalties.AsQueryable().Include(x => x.Roles).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"mute" })).ToList();
        }

        public static async Task<User> GetCachedFullUserAsync(this Database.UserContext context, ulong userId)
        {
            return (await context.Users.AsQueryable().Where(x => x.Id == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Wishes).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .Include(x => x.GameDeck).ThenInclude(x => x.Figures).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"user-{userId}", "users" })).FirstOrDefault();
        }

        public static async Task<User> GetCachedFullUserByShindenIdAsync(this Database.UserContext context, ulong userId)
        {
            return (await context.Users.AsQueryable().Where(x => x.Shinden == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Wishes).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .Include(x => x.GameDeck).ThenInclude(x => x.Figures).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"user-{userId}", "users" })).FirstOrDefault();
        }

        public static async Task<List<User>> GetCachedAllUsersLiteAsync(this Database.UserContext context)
        {
            return (await context.Users.AsQueryable().AsNoTracking().AsSplitQuery().FromCacheAsync(new MemoryCacheEntryOptions{ AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) })).ToList();
        }

        public static async Task<GameDeck> GetCachedUserGameDeckAsync(this Database.UserContext context, ulong userId)
        {
            return (await context.GameDecks.AsQueryable().Where(x => x.UserId == userId).Include(x => x.Cards).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"user-{userId}", "users" })).FirstOrDefault();
        }

        public static async Task<List<User>> GetCachedAllUsersAsync(this Database.UserContext context)
        {
            return (await context.Users.AsQueryable().Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats)
                .Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack)
                .Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList).Include(x => x.GameDeck).ThenInclude(x => x.Figures).AsNoTracking().AsSplitQuery()
                .FromCacheAsync(new MemoryCacheEntryOptions{ AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6) })).ToList();
        }

        public static async Task<List<GameDeck>> GetCachedPlayersForPVP(this Database.UserContext context, ulong ignore = 1)
        {
            return (await context.GameDecks.AsQueryable().Where(x => x.DeckPower > UserExtension.MIN_DECK_POWER && x.DeckPower < UserExtension.MAX_DECK_POWER && x.UserId != ignore).AsNoTracking().AsSplitQuery().FromCacheAsync(new MemoryCacheEntryOptions{ AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) })).ToList();
        }

        public static async Task<User> GetUserOrCreateAsync(this Database.UserContext context, ulong userId)
        {
            var user = await context.Users.AsQueryable().Where(x => x.Id == userId).Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .Include(x => x.GameDeck).ThenInclude(x => x.Figures).AsSplitQuery().FirstOrDefaultAsync();

            if (user == null)
            {
                user = user.Default(userId);
                await context.Users.AddAsync(user);
            }

            return user;
        }

        public static async Task<User> GetUserAndDontTrackAsync(this Database.UserContext context, ulong userId)
        {
            return await context.Users.AsQueryable().Include(x => x.Stats).Include(x => x.SMConfig).Include(x => x.TimeStatuses).Include(x => x.GameDeck).ThenInclude(x => x.PvPStats).Include(x => x.GameDeck).ThenInclude(x => x.Wishes)
                .Include(x => x.GameDeck).ThenInclude(x => x.Items).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.ArenaStats).Include(x => x.GameDeck)
                .ThenInclude(x => x.ExpContainer).Include(x => x.GameDeck).ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.Characters).Include(x => x.GameDeck)
                .ThenInclude(x => x.BoosterPacks).ThenInclude(x => x.RarityExcludedFromPack).Include(x => x.GameDeck).ThenInclude(x => x.Cards).ThenInclude(x => x.TagList)
                .Include(x => x.GameDeck).ThenInclude(x => x.Figures).AsNoTracking().AsSplitQuery().FirstOrDefaultAsync(x => x.Id == userId);
        }

        public static async Task<List<Question>> GetCachedAllQuestionsAsync(this Database.UserContext context)
        {
            return (await context.Questions.AsQueryable().Include(x => x.Answers).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"quiz" })).ToList();
        }

        public static async Task<Question> GetCachedQuestionAsync(this Database.UserContext context, ulong id)
        {
            return (await context.Questions.AsQueryable().Include(x => x.Answers).AsNoTracking().AsSplitQuery().FromCacheAsync(new string[] { $"quiz" })).FirstOrDefault(x => x.Id == id);
        }
    }
}
