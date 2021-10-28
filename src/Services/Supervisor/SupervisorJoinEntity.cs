#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Services.Supervisor
{
    public class SupervisorJoinEntity
    {
        public int TotalUsers { get; private set; }
        public List<ulong> IDs { get; private set; }
        public DateTime LastJoinTime { get; private set; }

        public SupervisorJoinEntity(ulong id) : this()
        {
            TotalUsers = 1;
            IDs.Add(id);
        }

        public SupervisorJoinEntity()
        {
            IDs = new List<ulong>();
            LastJoinTime = DateTime.Now;
            TotalUsers = 0;
        }

        public bool IsBannable() => IsValid() && TotalUsers > 3;
        public bool IsValid() => (DateTime.Now - LastJoinTime).TotalMinutes <= 2;
        public void Add(ulong id)
        {
            if (!IDs.Any(x => x == id))
            {
                IDs.Add(id);
                ++TotalUsers;
            }
        }

        public List<ulong> GetUsersToBan()
        {
            var copy = IDs.ToList();
            IDs.Clear();
            return copy;
        }
    }
}
