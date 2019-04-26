#pragma warning disable 1591

using System;

namespace Sanakan.Services.Supervisor
{
    public class SupervisorMessage
    {
        public SupervisorMessage(string content, int count = 1)
        {
            PreviousOccurrence = DateTime.Now;
            Content = content;
            Count = count;
        }

        public DateTime PreviousOccurrence { get; private set; }
        public string Content { get; private set; }
        public int Count { get; private set; }

        public bool IsValid() => (DateTime.Now - PreviousOccurrence).TotalMinutes <= 1;
        public int Inc() => ++Count;
    }
}
