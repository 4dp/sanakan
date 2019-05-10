#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using Sanakan.Database.Models.Configuration;
using Sanakan.Extensions;

namespace Sanakan.Services
{
    public class LandManager
    {
        public MyLand DetermineLand(IEnumerable<MyLand> lands, SocketGuildUser user, string name)
        {
            if (user == null) return null;
            if (name != null)
            {
                var land = lands.FirstOrDefault(x => x.Name == name);
                if (land == null) return null;

                return user.Roles.Any(x => x.Id == land.Manager) ? land : null;
            }

            var all = lands.Where(x => user.Roles.Any(c => c.Id == x.Manager));
            if (all.Count() < 1) return null;
            return all.First();
        }

        public EmbedBuilder GetMembersList(MyLand land, SocketGuild guild)
        {
            string valueString = "";
            foreach (var user in guild.Users.Where(x => x.Roles.Any(r => r.Id == land.Underling)))
                valueString += $"{user.Mention}\n";

            return new EmbedBuilder
            {
                Title = $"Członkowie: {land.Name}".TrimToLength(EmbedBuilder.MaxTitleLength),
                Description = valueString.TrimToLength(1700)
            };
        }
    }
}