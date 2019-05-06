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
        [Remarks("Gintama")]
        public async Task SearchAnimeAsync([Summary("tytuł")][Remainder]string title)
        {
            if (title.Equals("fate/loli")) title = "Fate/kaleid Liner Prisma Illya";

            var session = new SearchSession(Context.User, _shclient);
            if (_session.SessionExist(session)) return;
            
            var res = await _shclient.Search.QuickSearchAsync(title, QuickSearchType.Anime);
            if (res.IsSuccessStatusCode())
            {
                var list = res.Body;
                var toSend = _shinden.GetSearchAnimeOrMangaResponse(list);

                if (list.Count == 1)
                {
                    var info = (await _shclient.Title.GetInfoAsync(list.First())).Body;
                    await ReplyAsync("", false, info.ToEmbed());
                }
                else
                {
                    Discord.Rest.RestUserMessage[] msg = new Discord.Rest.RestUserMessage[10];
                    for (int index = 0; index < toSend.Length; index++)
                    {
                        if(toSend[index] != null) msg[index] = await Context.Channel.SendMessageAsync(toSend[index]);
                    }

                    session.SList = list;
                    session.Messages = msg;
                    await _session.TryAddSession(session);
                }
            }
            else
            {
                string toSend = "";
                switch (res.Code)
                {
                    case System.Net.HttpStatusCode.NotFound: toSend = "Brak wyników!";
                        break;

                    default: toSend = $"Brak połączenia z Shindenem! ({res.Code})";
                        break;
                }
                await ReplyAsync("", false, toSend.ToEmbedMessage(EMType.Error).Build());
            }
        }
    }
}