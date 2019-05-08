#pragma warning disable 1591

using System.Collections.Generic;

namespace Sanakan.Database.Models.Configuration
{
    public class GuildOptions
    {
        public ulong Id { get; set; }
        public ulong MuteRole { get; set; }
        public ulong ModMuteRole { get; set; }
        public ulong UserRole { get; set; }
        public ulong AdminRole { get; set; }
        public ulong GlobalEmotesRole { get; set; }
        public ulong NotificationChannel { get; set; }
        public ulong RaportChannel { get; set; }
        public ulong QuizChannel { get; set; }
        public ulong ToDoChannel { get; set; }
        public ulong NsfwChannel { get; set; }
        public ulong LogChannel { get; set; }
        public ulong GreetingsChannel { get; set; }
        public string WelcomeMessage { get; set; }
        public string GoodbyeMessage { get; set; }

        public virtual Waifu WaifuConfig { get; set; }

        public virtual ICollection<WithoutSupervisionChannel> ChannelsWithoutSupervision { get; set; }
        public virtual ICollection<WithoutExpChannel> ChannelsWithoutExp { get; set; }
        public virtual ICollection<CommandChannel> CommandChannels { get; set; }
        public virtual ICollection<ModeratorRoles> ModeratorRoles { get; set; }
        public virtual ICollection<LevelRole> RolesPerLevel { get; set; }
        public virtual ICollection<SelfRole> SelfRoles { get; set; }
        public virtual ICollection<MyLand> Lands { get; set; }
    }
}
