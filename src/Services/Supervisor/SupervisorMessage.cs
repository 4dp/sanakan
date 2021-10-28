#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Services.Supervisor
{
    public class SupervisorMessage
    {
        private static List<string> BannableStrings = new List<string>()
        {
            "dliscord.com/", ".gift/", "discorl.com/"
        };

        public SupervisorMessage(string content, int count = 1)
        {
            PreviousOccurrence = DateTime.Now;
            Content = content;
            Count = count;
        }

        public DateTime PreviousOccurrence { get; private set; }
        public string Content { get; private set; }
        public int Count { get; private set; }

        public bool IsBannable() => BannableStrings.Any(x => Content.Contains(x));
        public bool IsValid() => (DateTime.Now - PreviousOccurrence).TotalMinutes <= 1;
        public int Inc() => ++Count;
    }
}
