#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sanakan.Extensions;

namespace Sanakan.Database.Models
{
    public enum Quality
    {
        Broken = 0,
        Alpha  = 1,
        Beta   = 2,
        Gamma  = 3,
        Delta  = 4,
        Zeta   = 6,
        Lambda = 11,
        Sigma  = 18,
        Omega  = 24
    }

    public class Figure
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public ulong Character { get; set; }
        public bool IsComplete { get; set; }
        public Quality SkeletonQuality { get; set; }
        public DateTime CompletionDate { get; set; }

        public ulong GameDeckId { get; set; }
        [JsonIgnore]
        public virtual GameDeck GameDeck { get; set; }
    }
}
