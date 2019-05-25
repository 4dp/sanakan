#pragma warning disable 1591

using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Enum z grupami forum
    /// </summary>
    public enum ForumUserGroup
    {
        Unregistered = 1,
        User = 2,
        Administrator = 3,
        Moderator = 4,
        Banned = 8,
        SiteModerator = 19,
        GroupModerator = 20,
        DiscordAdministrator = 21,
        DiscordEmotes = 22,
        Developer = 23,
        DiscordPig = 24,
        Dev = 25
    }

    /// <summary>
    /// Struktura rejestracji użytkownika
    /// </summary>
    public class UserRegistration
    {
        /// <summary>
        /// Czy posiada uprawnienia su
        /// </summary>
        public bool IsSuperAdmin { get; set; }
        /// <summary>
        /// Id użytkownika forum
        /// </summary>
        public ulong ForumUserId { get; set; }
        /// <summary>
        /// Id użytkownika discord
        /// </summary>
        public ulong DiscordUserId { get; set; }
        /// <summary>
        /// Nazwa użytkownika na stronie
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Lista rang użytkownika na forum
        /// </summary>
        public List<ForumUserGroup> ForumGroupsId { get; set; }
    }
}
