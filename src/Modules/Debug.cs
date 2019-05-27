#pragma warning disable 1591

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Config;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.PocketWaifu;
using Shinden;
using Shinden.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Debug"), Group("dev"), DontAutoLoad, RequireDev]
    public class Debug : SanakanModuleBase<SocketCommandContext>
    {
        private Waifu _waifu;
        private IConfig _config;
        private Services.Helper _helper;
        private ShindenClient _shClient;
        private Services.ImageProcessing _img;
        private Database.UserContext _dbUserContext;

        public Debug(Waifu waifu, ShindenClient shClient, Database.UserContext userContext, Services.Helper helper, Services.ImageProcessing img, IConfig config)
        {
            _dbUserContext = userContext;
            _shClient = shClient;
            _helper = helper;
            _config = config;
            _waifu = waifu;
            _img = img;
        }

        [Command("poke", RunMode = RunMode.Async)]
        [Summary("generuje obrazek safari")]
        [Remarks("1")]
        public async Task GeneratPokeImageAsync([Summary("nr grafiki")]int index)
        {
            try
            {
                var reader = new JsonFileReader($"./Pictures/Poke/List.json");
                var images = reader.Load<List<SafariImage>>();

                var character = (await _shClient.GetCharacterInfoAsync(2)).Body;
                var channel = Context.Channel as ITextChannel;

                _ = await _waifu.GetSafariViewAsync(images[index], character, _waifu.GenerateNewCard(character), channel);
            }
            catch (Exception ex)
            {
                await ReplyAsync("", embed: $"Coś poszło nie tak: {ex.Message}".ToEmbedMessage(EMType.Error).Build());
            }
        }

        [Command("utitle")]
        [Summary("updatuje tytuł karty")]
        [Remarks("ssało")]
        public async Task ChangeTitleCardAsync([Summary("WID")]ulong wid, [Summary("tytuł")][Remainder]string title = null)
        {
            var thisCard = _dbUserContext.Cards.FirstOrDefault(x => x.Id == wid);
            if (thisCard == null)
            {
                await ReplyAsync("", embed: $"Taka karta nie istnieje.".ToEmbedMessage(EMType.Error).Build());
                return;
            }

            if (title != null)
            {
                thisCard.Title = title;
            }
            else
            {
                var res = await _shClient.GetCharacterInfoAsync(thisCard.Character);
                if (res.IsSuccessStatusCode())
                {
                    thisCard.Title = res.Body?.Relations?.OrderBy(x => x.Id)?.FirstOrDefault()?.Title ?? "????";
                }
            }

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { "users" });

            await ReplyAsync("", embed: $"Nowy tytuł to: `{thisCard.Title}`".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tsafari")]
        [Summary("wyłącza/załącza safari")]
        [Remarks("true")]
        public async Task ToggleSafariAsync([Summary("true/false - czy zapisać")]bool save = false)
        {
            var config = _config.Get();
            config.SafariEnabled = !config.SafariEnabled;
            if (save) _config.Save();

            await ReplyAsync("", embed: $"Safari: {config.SafariEnabled} `Zapisano: {save.GetYesNo()}`".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("lvlbadge", RunMode = RunMode.Async)]
        [Summary("generuje przykładowy obrazek otrzymania poziomu")]
        [Remarks("")]
        public async Task GenerateLevelUpBadgeAsync([Summary("użytkownik(opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            using (var badge = await _img.GetLevelUpBadgeAsync("Very very long nickname of trolly user", 
                2154, usr.GetAvatarUrl(), usr.Roles.OrderByDescending(x => x.Position).First().Color))
            {
                using (var badgeStream = badge.ToPngStream())
                {
                    await Context.Channel.SendFileAsync(badgeStream, $"{usr.Id}.png");
                }
            }
        }

        [Command("devr")]
        [Summary("przyznaje lub odbiera role developera")]
        [Remarks("")]
        public async Task ToggleDeveloperRoleAsync()
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return;

            var devr = Context.Guild.Roles.FirstOrDefault(x => x.Name == "Developer");
            if (devr == null) return;

            if (user.Roles.Contains(devr))
            {
                await user.RemoveRoleAsync(devr);
                await ReplyAsync("", embed: $"{user.Mention} stracił role deva.".ToEmbedMessage(EMType.Success).Build());
            }
            else
            {
                await user.AddRoleAsync(devr);
                await ReplyAsync("", embed: $"{user.Mention} otrzymał role deva.".ToEmbedMessage(EMType.Success).Build());
            }
        }

        [Command("gcard")]
        [Summary("generuje kartę i dodaje ją użytkownikowi")]
        [Remarks("Sniku 54861")]
        public async Task GenerateCardAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("id postaci na shinden(nie podanie - losowo)")]ulong id = 0,
            [Summary("jakość karty(nie podanie - losowo)")] Rarity rarity = Rarity.E)
        {
            var character = (id == 0) ? await _waifu.GetRandomCharacterAsync() : (await _shClient.GetCharacterInfoAsync(id)).Body;
            var card = (rarity == Rarity.E) ? _waifu.GenerateNewCard(character) : _waifu.GenerateNewCard(character, rarity);

            card.Source = CardSource.GodIntervention;
            var botuser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            botuser.GameDeck.Cards.Add(card);

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{user.Mention} otrzymał {card.GetString(false, false, true)}.".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("sc")]
        [Summary("zmienia SC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserScAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba SC")]long amount)
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            botuser.ScCnt += amount;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.ScCnt} SC".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("tc")]
        [Summary("zmienia TC użytkownika o podaną wartość")]
        [Remarks("Sniku 10000")]
        public async Task ChangeUserTcAsync([Summary("użytkownik")]SocketGuildUser user, [Summary("liczba TC")]long amount)
        {
            var botuser = await _dbUserContext.GetUserOrCreateAsync(user.Id);
            botuser.TcCnt += amount;

            await _dbUserContext.SaveChangesAsync();

            QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}", "users" });

            await ReplyAsync("", embed: $"{user.Mention} ma teraz {botuser.TcCnt} TC".ToEmbedMessage(EMType.Success).Build());
        }

        [Command("kill", RunMode = RunMode.Async)]
        [Summary("wyłącza bota")]
        [Remarks("")]
        public async Task TurnOffAsync()
        {
            await ReplyAsync("", embed: "To dobry czas by umrzeć.".ToEmbedMessage(EMType.Bot).Build());
            await Context.Client.LogoutAsync();
            await Task.Delay(1500);
            Environment.Exit(0);
        }

        [Command("update", RunMode = RunMode.Async)]
        [Summary("wyłącza bota z kodem 255")]
        [Remarks("")]
        public async Task TurnOffWithUpdateAsync()
        {
            await ReplyAsync("", embed: "To już czas?".ToEmbedMessage(EMType.Bot).Build());
            await Context.Client.LogoutAsync();
            await Task.Delay(1500);
            Environment.Exit(255);
        }

        [Command("pomoc", RunMode = RunMode.Async)]
        [Alias("help", "h")]
        [Summary("wypisuje polecenia")]
        [Remarks("kasuj"), RequireAdminOrModRole]
        public async Task SendHelpAsync([Summary("nazwa polecenia(opcjonalne)")][Remainder]string command = null)
        {
            if (command != null)
            {
                try
                {
                    await ReplyAsync(_helper.GiveHelpAboutPrivateCmd("Debug", command));
                }
                catch (Exception ex)
                {
                    await ReplyAsync("", embed: ex.Message.ToEmbedMessage(EMType.Error).Build());
                }

                return;
            }

            await ReplyAsync(_helper.GivePrivateHelp("Debug"));
        }
    }
}
