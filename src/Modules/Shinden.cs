#pragma warning disable 1591

using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden;
using System;
using System.Linq;
using System.Threading.Tasks;
using Z.EntityFramework.Plus;

namespace Sanakan.Modules
{
    [Name("Shinden"), RequireCommandChannel, RequireUserRole]
    public class Shinden : SanakanModuleBase<SocketCommandContext>
    {
        private ShindenClient _shclient;
        private SessionManager _session;
        private Services.Shinden _shinden;
        private Database.UserContext _dbUserContext;

        public Shinden(ShindenClient client, SessionManager session, Services.Shinden shinden, Database.UserContext userContext)
        {
            _shclient = client;
            _session = session;
            _shinden = shinden;
            _dbUserContext = userContext;
        }

        [Command("odcinki", RunMode = RunMode.Async)]
        [Alias("episodes")]
        [Summary("wyświetla nowo dodane epizody")]
        [Remarks("")]
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
                        var dm = await Context.User.GetOrCreateDMChannelAsync();
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
            await _shinden.SendSearchInfoAsync(Context, title, QuickSearchType.Anime);
        }

        [Command("manga", RunMode = RunMode.Async)]
        [Alias("komiks")]
        [Summary("wyświetla informacje o mandze")]
        [Remarks("Gintama")]
        public async Task SearchMangaAsync([Summary("tytuł")][Remainder]string title)
        {
            await _shinden.SendSearchInfoAsync(Context, title, QuickSearchType.Manga);
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
            var toSend = _shinden.GetSearchResponse(list, "Wybierz postać którą chcesz wyświetlić poprzez wpisanie numeru odpowadającemu jej na liście.");

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

                if (_dbUserContext.Users.Any(x => x.Shinden == shindenId))
                {
                    await ReplyAsync("", embed: "Wygląda na to, że ktoś już połączył się z tym kontem.".ToEmbedMessage(EMType.Error).Build());
                    return;
                }

                var botuser = await _dbUserContext.GetUserOrCreateAsync(Context.User.Id);
                botuser.Shinden = shindenId;

                await _dbUserContext.SaveChangesAsync();

                QueryCacheManager.ExpireTag(new string[] { $"users" });

                await ReplyAsync("", embed: "Konta zostały połączone.".ToEmbedMessage(EMType.Success).Build());
                return;
            }

            await ReplyAsync("", embed: $"Brak połączenia z Shindenem! ({response.Code})".ToEmbedMessage(EMType.Error).Build());
        }
    }
}