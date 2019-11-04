#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services;
using Sanakan.Extensions;
using Sanakan.Services.Commands;
using System.Threading.Tasks;
using Sanakan.Preconditions;
using Discord.WebSocket;
using System.Linq;
using System;

namespace Sanakan.Modules
{
    [Name("Kraina"), RequireUserRole]
    public class Lands : SanakanModuleBase<SocketCommandContext>
    {
        private LandManager _manager;

        public Lands(LandManager manager)
        {
            _manager = manager;
        }

        [Command("ludność", RunMode = RunMode.Async)]
        [Alias("ludnosc", "ludnośc", "ludnosć", "people")]
        [Summary("wyświetla użytkowników należących do krainy")]
        [Remarks("Kotleciki")]
        public async Task ShowPeopleAsync([Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                var land = _manager.DetermineLand(config.Lands, Context.User as SocketGuildUser, name);
                if (land == null)
                {
                    await ReplyAsync("", embed: "Nie zarządzasz żadną krainą.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                foreach (var emb in _manager.GetMembersList(land, Context.Guild))
                {
                    await ReplyAsync("", embed: emb);
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }

        [Command("kraina dodaj", RunMode = RunMode.Async)]
        [Alias("land add")]
        [Summary("dodaje użytkownika do krainy")]
        [Remarks("Karna Kotleciki")]
        public async Task AddPersonAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                var land = _manager.DetermineLand(config.Lands, Context.User as SocketGuildUser, name);
                if (land == null)
                {
                    await ReplyAsync("", embed: "Nie zarządzasz żadną krainą.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var role = Context.Guild.GetRole(land.Underling);
                if (role == null)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono roli członka!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (!user.Roles.Contains(role))
                    await user.AddRoleAsync(role);

                await ReplyAsync("", embed: $"{user.Mention} dołącza do `{land.Name}`.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("kraina usuń", RunMode = RunMode.Async)]
        [Alias("land remove", "kraina usun")]
        [Summary("usuwa użytkownika z krainy")]
        [Remarks("Karna")]
        public async Task RemovePersonAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            using (var db = new Database.GuildConfigContext(Config))
            {
                var config = await db.GetCachedGuildFullConfigAsync(Context.Guild.Id);
                var land = _manager.DetermineLand(config.Lands, Context.User as SocketGuildUser, name);
                if (land == null)
                {
                    await ReplyAsync("", embed: "Nie zarządzasz żadną krainą.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var role = Context.Guild.GetRole(land.Underling);
                if (role == null)
                {
                    await ReplyAsync("", embed: "Nie odnaleziono roli członka!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (user.Roles.Contains(role))
                    await user.RemoveRoleAsync(role);

                await ReplyAsync("", embed: $"{user.Mention} odchodzi z `{land.Name}`.".ToEmbedMessage(EMType.Success).Build());
            }
        }
    }
}