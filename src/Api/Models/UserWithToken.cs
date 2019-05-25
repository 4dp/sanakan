using System;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Użytkownik wraz z tokenem
    /// </summary>
    public class UserWithToken
    {
        /// <summary>
        /// Token JWT
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// Data wygaśnięcia
        /// </summary>
        public DateTime? Expire { get; set; }
        /// <summary>
        /// Uzytkownik
        /// </summary>
        public Database.Models.User User { get; set; }
    }
}
