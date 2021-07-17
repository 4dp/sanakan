#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class QuestionExtension
    {
        public static bool CheckAnswer(this Question q, int ans) => q.Answer == ans;

        public static string GetRightAnswer(this Question q)
            =>  $"Prawidłowa odpowiedź to: **{q.Answer}** - {q.Answers.First(x => x.Number == q.Answer).Content}";

        private static Discord.IEmote GetEmote(int i)
        {
            if (i == 0) return new Discord.Emoji("\u0030\u20E3");
            if (i == 1) return new Discord.Emoji("\u0031\u20E3");
            if (i == 2) return new Discord.Emoji("\u0032\u20E3");
            if (i == 3) return new Discord.Emoji("\u0033\u20E3");
            if (i == 4) return new Discord.Emoji("\u0034\u20E3");
            if (i == 5) return new Discord.Emoji("\u0035\u20E3");
            if (i == 6) return new Discord.Emoji("\u0036\u20E3");
            if (i == 7) return new Discord.Emoji("\u0037\u20E3");
            if (i == 8) return new Discord.Emoji("\u0038\u20E3");
            return new Discord.Emoji("\u0039\u20E3");
        }

        public static void RandomizeAnswers(this Question q)
        {
            var numbersColeration = new List<Tuple<int, int>>();
            var possibleAnswers = q.Answers.Select(x => x.Number).ToList();

            foreach (var answer in q.Answers)
            {
                var num = Services.Fun.GetOneRandomFrom(possibleAnswers);
                possibleAnswers.Remove(num);

                numbersColeration.Add(new Tuple<int, int>(num, answer.Number));
                answer.Number = num;
            }

            q.Answer = numbersColeration.First(x => x.Item2 == q.Answer).Item1;
            q.Answers = q.Answers.OrderBy(x => x.Number).ToList();
        }

        public static string Get(this Question q)
        {
            string str = $"**{q.Content}**\n\n";
            foreach(var item in q.Answers)
            {
                str += $"**{item.Number}**: {item.Content}\n";
            }
            return str;
        }

        public static Discord.IEmote[] GetEmotes(this Question q)
        {
            List<Discord.IEmote> emo = new List<Discord.IEmote>();
            foreach (var qu in q.Answers) emo.Add(GetEmote(qu.Number));
            return emo.ToArray();
        }

        public static Discord.IEmote GetRightEmote(this Question q) => GetEmote(q.Answer);
    }
}
