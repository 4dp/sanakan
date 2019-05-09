#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Services;
using Sanakan.Extensions;
using Sanakan.Services.Commands;
using System.Threading.Tasks;
using Sanakan.Preconditions;
using Discord.WebSocket;
using System.Linq;

namespace Sanakan.Modules
{
    [Name("Kraina"), RequireUserRole]
    public class Lands : SanakanModuleBase<SocketCommandContext>
    {
        private Database.GuildConfigContext _dbGuildConfigContext;
        private LandManager _manager;

        public Lands(Database.GuildConfigContext dbGuildConfigContext, LandManager manager)
        {
            _dbGuildConfigContext = dbGuildConfigContext;
            _manager = manager;
        }

        [Command("ludność", RunMode = RunMode.Async)]
        [Alias("ludnosc", "ludnośc", "ludnosć", "people")]
        [Summary("wyświetla użytkowników należących do krainy")]
        [Remarks("Kotleciki")]
        public async Task ShowPeopleAsync([Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
            var land = _manager.DetermineLand(config.Lands, Context.User as SocketGuildUser, name);
            if (land == null)
            {
                await ReplyAsync("", embed: "Nie zarządzasz żadną krainą.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            await ReplyAsync("", embed: _manager.GetMembersList(land, Context.Guild).Build());
        }

        [Command("kraina dodaj", RunMode = RunMode.Async)]
        [Alias("land add")]
        [Summary("dodaje użytkownika do krainy")]
        [Remarks("Karna Kotleciki")]
        public async Task AddPersonAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
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

        [Command("kraina usuń", RunMode = RunMode.Async)]
        [Alias("land remove", "kraina usun")]
        [Summary("usuwa użytkownika z krainy")]
        [Remarks("Karna")]
        public async Task RemovePersonAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("nazwa krainy(opcjonalne)")][Remainder]string name = null)
        {
            var config = await _dbGuildConfigContext.GetCachedGuildFullConfigAsync(Context.Guild.Id);
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