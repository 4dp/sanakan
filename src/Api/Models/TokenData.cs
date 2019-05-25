#pragma warning disable 1591

using System;

namespace Sanakan.Api.Models
{
    public class TokenData
    {
        public string Token { get; set; }
        public DateTime Expire { get; set; }
    }
}
