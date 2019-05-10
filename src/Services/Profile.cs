#pragma warning disable 1591

using Discord.WebSocket;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Services
{
    public enum TopType
    {
        Level, ScCnt, TcCnt, Posts, PostsMonthly, PostsMonthlyCharacter, Commands, Cards
    }

    public class Profile
    {
        public List<User> GetTopUsers(List<User> list, TopType type)
            => GetRangeMax(OrderUsersByTop(list, type), 50);

        private List<T> GetRangeMax<T>(List<T> list, int range)
            => list.GetRange(0, list.Count > range ? range : list.Count);

        private List<User> OrderUsersByTop(List<User> list, TopType type)
        {
            switch (type)
            {
                default:
                case TopType.Level:
                    return list.OrderByDescending(x => x.Level).ToList();

                case TopType.ScCnt:
                    return list.OrderByDescending(x => x.ScCnt).ToList();

                case TopType.TcCnt:
                    return list.OrderByDescending(x => x.TcCnt).ToList();

                case TopType.Posts:
                    return list.OrderByDescending(x => x.MessagesCnt).ToList();

                case TopType.PostsMonthly:
                    return list.Where(x => x.IsCharCounterActive()).OrderByDescending(x => x.MessagesCntAtDate - x.MessagesCnt).ToList();

                case TopType.PostsMonthlyCharacter:
                    return list.Where(x => x.IsCharCounterActive()).OrderByDescending(x => x.CharacterCntFromDate / (x.MessagesCntAtDate - x.MessagesCnt)).ToList();

                case TopType.Commands:
                    return list.OrderByDescending(x => x.CommandsCnt).ToList();

                case TopType.Cards:
                    return list.OrderByDescending(x => x.GameDeck.Cards.Count).ToList();
            }
        }

        public List<string> BuildListView(List<User> list, TopType type, SocketGuild guild)
        {
            var view = new List<string>();

            foreach (var user in list)
            {
                var bUsr = guild.GetUser(user.Id);
                if (bUsr == null) continue;

                view.Add($"{bUsr.Mention}: {user.GetViewValueForTop(type)}");
            }

            return view;
        }
    }
}