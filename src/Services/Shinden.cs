#pragma warning disable 1591

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Sanakan.Extensions;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden;

namespace Sanakan.Services
{
    public enum UrlParsingError
    {
        None, InvalidUrl, InvalidUrlForum
    }

    public class Shinden
    {
        private ShindenClient _shClient;
        private SessionManager _session;
        private ImageProcessing _img;

        public Shinden(ShindenClient client, SessionManager session, ImageProcessing img)
        {
            _shClient = client;
            _session = session;
            _img = img;
        }

        public UrlParsingError ParseUrlToShindenId(string url, out ulong shindenId)
        {
            shindenId = 0;
            var splited = url.Split('/');
            bool http = splited[0].Equals("https:") || splited[0].Equals("http:");
            int toChek = http ? 2 : 0;

            if (splited.Length < (toChek == 2 ? 5 : 3))
                return UrlParsingError.InvalidUrl;

            if (splited[toChek].Equals("shinden.pl") || splited[toChek].Equals("www.shinden.pl"))
            {
                if(splited[++toChek].Equals("user") || splited[toChek].Equals("animelist") || splited[toChek].Equals("mangalist"))
                {
                    var data = splited[++toChek].Split('-');
                    if (ulong.TryParse(data[0], out shindenId))
                        return UrlParsingError.None;
                }
            }

            if (splited[toChek].Equals("forum.shinden.pl") || splited[toChek].Equals("www.forum.shinden.pl"))
                return UrlParsingError.InvalidUrlForum;

            return UrlParsingError.InvalidUrl;
        }

        public string[] GetSearchResponse(IEnumerable<object> list, string title)
        {
            string temp = "";
            int messageNr = 0;
            var toSend = new string[10];
            toSend[0] = $"{title}\n```ini\n";
            int i = 0;

            foreach (var item in list)
            {
                temp += $"[{++i}] {item.ToString()}\n";
                if (temp.Length > 1800)
                {
                    toSend[messageNr] += "\n```";
                    toSend[++messageNr] += $"```ini\n[{i}] {item.ToString()}\n";
                    temp = "";
                }
                else toSend[messageNr] += $"[{i}] {item.ToString()}\n";
            }
            toSend[messageNr] += "```\nNapisz `koniec` aby zamknąć menu.";

            return toSend;
        }

        public async Task SendSearchInfoAsync(SocketCommandContext context, string title, QuickSearchType type)
        {
            if (title.Equals("fate/loli")) title = "Fate/kaleid Liner Prisma Illya";

            var session = new SearchSession(context.User, _shClient);
            if (_session.SessionExist(session)) return;

            var res = await _shClient.Search.QuickSearchAsync(title, type);
            if (!res.IsSuccessStatusCode())
            {
                await context.Channel.SendMessageAsync("", false, GetResponseFromSearchCode(res).ToEmbedMessage(EMType.Error).Build());
                return;
            }

            var list = res.Body;
            var toSend = GetSearchResponse(list, "Wybierz tytuł który chcesz wyświetlić poprzez wpisanie numeru odpowadającemu mu na liście.");

            if (list.Count == 1)
            {
                var info = (await _shClient.Title.GetInfoAsync(list.First())).Body;
                await context.Channel.SendMessageAsync("", false, info.ToEmbed());
            }
            else
            {
                session.SList = list;
                await SendSearchResponseAsync(context, toSend, session);
            }
        }

        public async Task SendSearchResponseAsync(SocketCommandContext context, string[] toSend, SearchSession session)
        {
            var msg = new Discord.Rest.RestUserMessage[10];
            for (int index = 0; index < toSend.Length; index++)
                if (toSend[index] != null) msg[index] = await context.Channel.SendMessageAsync(toSend[index]);

            session.Messages = msg;
            await _session.TryAddSession(session);
        }

        public string GetResponseFromSearchCode(HttpStatusCode code)
        {
            switch (code)
            {
                case HttpStatusCode.NotFound:
                    return "Brak wyników!";

                default:
                    return $"Brak połączenia z Shindenem! ({code})";
            }
        }

        public async Task<Stream> GetSiteStatisticAsync(ulong shindenId, SocketGuildUser user)
        {
            var response = await _shClient.User.GetAsync(shindenId);
            if (!response.IsSuccessStatusCode()) return null;

            var resLR = await _shClient.User.GetLastReadedAsync(shindenId);
            var resLW = await _shClient.User.GetLastWatchedAsync(shindenId);

            using (var image = await _img.GetSiteStatisticAsync(response.Body,
                user.Roles.OrderByDescending(x => x.Position).FirstOrDefault()?.Color ?? Discord.Color.DarkerGrey,
                resLR.IsSuccessStatusCode() ? resLR.Body : null,
                resLW.IsSuccessStatusCode() ? resLW.Body : null))
            {
                return image.ToPngStream();
            }
        }
    }
}