#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Services.Supervisor
{
    public class SupervisorEntity
    {
        public List<SupervisorMessage> Messages { get; private set; }
        public DateTime LastMessage { get; private set; }
        public int TotalMessages { get; private set; }

        public SupervisorEntity(string contentOfFirstMessage) : this()
        {
            TotalMessages = 1;
            Messages.Add(new SupervisorMessage(contentOfFirstMessage));
        }
        
        public SupervisorEntity()
        {
            Messages = new List<SupervisorMessage>();
            LastMessage = DateTime.Now;
            TotalMessages = 0;
        }

        public SupervisorMessage Get(string content) => Messages.FirstOrDefault(x => x.Content == content);
        public bool IsValid() => (DateTime.Now - LastMessage).TotalMinutes <= 2;
        public void Add(SupervisorMessage message) => Messages.Add(message);
        public int Inc()
        {
            if ((DateTime.Now - LastMessage).TotalSeconds > 5)
                TotalMessages = 0;

            LastMessage = DateTime.Now;

            return ++TotalMessages;
        }
    }
}
