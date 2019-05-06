#pragma warning disable 1591

using Discord.Commands;
using Sanakan.Extensions;
using Sanakan.Preconditions;
using Sanakan.Services.Commands;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sanakan.Modules
{
    [Name("Shinden"), RequireCommandChannel, RequireUserRole]
    public class Shinden : SanakanModuleBase<SocketCommandContext>
    {
        private ShindenClient _shclient;
        private SessionManager _session;
        private Services.Shinden _shinden;

        public Shinden(ShindenClient client, SessionManager session, Services.Shinden shinden)
        {
            _shclient = client;
            _session = session;
            _shinden = shinden;
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
    }
}