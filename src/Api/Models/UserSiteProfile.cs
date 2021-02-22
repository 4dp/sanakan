using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Profil użytkownika na stronie
    /// </summary>
    public class UserSiteProfile
    {
        /// <summary>
        /// Liczba posaidanych kart z podziałem na jakość
        /// </summary>
        public Dictionary<string, int> CardsCount { get; set; }
        /// <summary>
        /// Liczba kart które może posiadać użytkownik
        /// </summary>
        public long MaxCardCount { get; set; }
        /// <summary>
        /// Waifu
        /// </summary>
        public CardFinalView Waifu { get; set; }
        /// <summary>
        /// Galeria
        /// </summary>
        public List<CardFinalView> Gallery { get; set; }
        /// <summary>
        /// Lista wypraw
        /// </summary>
        public List<ExpeditionCard> Expeditions { get; set; }
        /// <summary>
        /// Lista tagów jakie ma użytkownik na kartach
        /// </summary>
        public List<string> TagList { get; set; }
        /// <summary>
        /// warunki wymiany z użytkownikiem
        /// </summary>
        public string ExchangeConditions { get; set; }
    }
}
