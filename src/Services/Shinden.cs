using System.Collections.Generic;
using Shinden.Models;

namespace Sanakan.Services
{
    public class Shinden
    {
        public string[] GetSearchAnimeOrMangaResponse(List<IQuickSearch> list)
        {
            string temp = "";
            int messageNr = 0;
            string[] toSend = new string[10];
            toSend[0] = "Wybierz tytuł który chcesz wyświetlić poprzez wpisanie numeru odpowadającemu mu na liście.\n```ini\n";
            int i = 1;

            foreach (var item in list)
            {
                temp += "[" + i + "] " + item.Title + "\n";
                if (temp.Length > 1800)
                {
                    toSend[messageNr] += "\n```";
                    toSend[++messageNr] += "```ini\n[" + i + "] " + item.Title + "\n";
                    temp = "";
                }
                else toSend[messageNr] += "[" + i + "] " + item.Title + "\n";

                ++i;
            }
            toSend[messageNr] += "```\nNapisz `koniec` aby zamknąć menu.";

            return toSend;
        }
    }
}