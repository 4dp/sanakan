#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shden = Shinden;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Shinden"), RequireUserRole]
    public class Shinden : SanakanModuleBase<SocketCommandContext>
    {
        private Shden.ShindenClient _shclient;
        private SessionManager _session;
        private Services.Shinden _shinden;

        public Shinden(Shden.ShindenClient client, SessionManager session, Services.Shinden shinden)
        {
            _shclient = client;
            _session = session;
            _shinden = shinden;
        }

        [Command("odcinki", RunMode = RunMode.Async)]
        [Alias("episodes")]
        [Summary("wyświetla nowo dodane epizody")]
        [Remarks(""), RequireCommandChannel]
        public async Task ShowNewEpisodesAsync()
        {
            var response = await _shclient.GetNewEpisodesAsync();
            if (response.IsSuccessStatusCode())
            {
                var episodes = response.Body;
                if (episodes?.Count > 0)
                {
                    var msg = await ReplyAsync("", embed: "Lista poszła na PW!".ToEmbedMessage(EMType.Success).Build());

                    try
                    {
                        var dm = await Context.User.CreateDMChannelAsync();
                        foreach (var ep in episodes)
                        {
                            await dm.SendMessageAsync("", false, ep.ToEmbed());
                            await Task.Delay(500);
                        }
                        await dm.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        await msg.ModifyAsync(x => x.Embed = $"{Context.User.Mention} nie udało się wyłać PW! ({ex.Message})".ToEmbedMessage(EMType.Error).Build());
                    }

                    return;
                }
            }

            await ReplyAsync("", embed: "Nie udało się pobrać listy odcinków.".ToEmbedMessage(EMType.Error).Build());
        }

        [Command("anime", RunMode = RunMode.Async)]
        [Alias("bajka")]
        [Summary("wyświetla informacje o anime")]
        [Remarks("Soul Eater")]
        public async Task SearchAnimeAsync([Summary("tytuł")][Remainder]string title)
        {
            await _shinden.SendSearchInfoAsync(Context, title, Shden.QuickSearchType.Anime);
        }

        [Command("manga", RunMode = RunMode.Async)]
        [Alias("komiks")]
        [Summary("wyświetla informacje o mandze")]
        [Remarks("Gintama")]
        public async Task SearchMangaAsync([Summary("tytuł")][Remainder]string title)
        {
            await _shinden.SendSearchInfoAsync(Context, title, Shden.QuickSearchType.Manga);
        }

        [Command("postać", RunMode = RunMode.Async)]
        [Alias("postac", "character")]
        [Summary("wyświetla informacje o postaci")]
        [Remarks("Gintoki")]
        public async Task SearchCharacterAsync([Summary("imie")][Remainder]string name)
        {
            var session = new SearchSession(Context.User, _shclient);
            if (_session.SessionExist(session)) return;

            var response = await _shclient.Search.CharacterAsync(name);
            if (!response.IsSuccessStatusCode())
            {
                await ReplyAsync("", embed: _shinden.GetResponseFromSearchCode(response).ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var list = response.Body;
            var toSend = _shinden.GetSearchResponse(list, "Wybierz postać, którą chcesz wyświetlić poprzez wpisanie numeru odpowiadającemu jej na liście.");

            if (list.Count == 1)
            {
                var info = (await _shclient.GetCharacterInfoAsync(list.First())).Body;
                await ReplyAsync("", false, info.ToEmbed());
            }
            else
            {
                session.PList = list;
                await _shinden.SendSearchResponseAsync(Context, toSend, session);
            }
        }

        [Command("strona", RunMode = RunMode.Async)]
        [Alias("ile", "otaku", "site", "mangozjeb")]
        [Summary("wyświetla statystyki użytkownika z strony")]
        [Remarks("karna")]
        public async Task ShowSiteStatisticAsync([Summary("użytkownik (opcjonalne)")]SocketGuildUser user = null)
        {
            var usr = user ?? Context.User as SocketGuildUser;
            if (usr == null) return;

            using (var db = new Database.UserContext(Config))
            {
                var botUser = await db.GetCachedFullUserAsync(usr.Id);
                if (botUser == null)
                {
                    await ReplyAsync("", embed: "Ta osoba nie ma profilu bota.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                if (botUser?.Shinden == 0)
                {
                    await ReplyAsync("", embed: "Ta osoba nie połączyła konta bota z kontem na stronie.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var stream = await _shinden.GetSiteStatisticAsync(botUser.Shinden, usr))
                {
                    if (stream == null)
                    {
                        await ReplyAsync("", embed: $"Brak połączenia z Shindenem!".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    await Context.Channel.SendFileAsync(stream, $"{usr.Id}.png", $"{Shden.API.Url.GetProfileURL(botUser.Shinden)}");
                }
            }
        }

        [Command("połącz")]
        [Alias("connect", "polacz", "połacz", "polącz")]
        [Summary("łączy funkcje bota, z kontem na stronie")]
        [Remarks("https://shinden.pl/user/136-mo0nisi44")]
        public async Task ConnectAsync([Summary("adres do profilu")]string url)
        {
            switch (_shinden.ParseUrlToShindenId(url, out var shindenId))
            {
                case Services.UrlParsingError.InvalidUrl:
                    await ReplyAsync("", embed: "Wygląda na to, że podałeś niepoprawny link.".ToEmbedMessage(EMType.Error).Build());
                    return;

                case Services.UrlParsingError.InvalidUrlForum:
                await ReplyAsync("", embed: "Wygląda na to, że podałeś link do forum zamiast strony.".ToEmbedMessage(EMType.Error).Build());
                    return;

                default:
                case Services.UrlParsingError.None:
                    break;
            }

            var response = await _shclient.User.GetAsync(shindenId);
            if (response.IsSuccessStatusCode())
            {
                var user = response.Body;
                var userNameInDiscord = (Context.User as SocketGuildUser).Nickname ?? Context.User.Username;

                if (!user.Name.Equals(userNameInDiscord))
                {
                    await ReplyAsync("", embed: "Wykryto próbę podszycia się. Nieładnie!".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                using (var db = new Database.UserContext(Config))
                {
                    if (db.Users.Any(x => x.Shinden == shindenId))
                    {
                        await ReplyAsync("", embed: "Wygląda na to, że ktoś już połączył się z tym kontem.".ToEmbedMessage(EMType.Error).Build());
                        return;
                    }

                    var botuser = await db.GetUserOrCreateAsync(Context.User.Id);
                    botuser.Shinden = shindenId;

                    await db.SaveChangesAsync();

                    QueryCacheManager.ExpireTag(new string[] { $"user-{botuser.Id}" });
                }

                await ReplyAsync("", embed: "Konta zostały połączone.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            await ReplyAsync("", embed: $"Brak połączenia z Shindenem! ({response.Code})".ToEmbedMessage(EMType.Error).Build());
        }
    }
}