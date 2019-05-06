using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord.Commands;
using Sanakan.Extensions;
using Sanakan.Services.Session;
using Sanakan.Services.Session.Models;
using Shinden;

namespace Sanakan.Services
{
    public class Shinden
    {
        private ShindenClient _shClient;
        private SessionManager _session;

        public Shinden(ShindenClient client, SessionManager session)
        {
            _shClient = client;
            _session = session;
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
    }
}