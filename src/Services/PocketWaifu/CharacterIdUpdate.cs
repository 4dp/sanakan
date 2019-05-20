#pragma warning disable 1591

using System;
using System.Collections.Generic;

namespace Sanakan.Services.PocketWaifu
{
    public class CharacterIdUpdate
    {
        public CharacterIdUpdate()
        {
            Ids = new List<ulong>();
            LastUpdate = DateTime.MinValue;
        }

        public void Update(List<ulong> ids)
        {
            LastUpdate = DateTime.Now;
            Ids = ids;
        }

        public bool IsNeedForUpdate()
            => (DateTime.Now - LastUpdate).TotalDays >= 1;

        public List<ulong> Ids { get; private set; }
        public DateTime LastUpdate { get; private set; }
    }
}