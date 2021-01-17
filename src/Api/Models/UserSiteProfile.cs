using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Profil użytkownika na stronie
    /// </summary>
    public class UserSiteProfile
    {
        /// <summary>
        /// Liczba posiadanych sss
        /// </summary>
        public int SSSCount { get; set; }
        /// <summary>
        /// Liczba posiadanych ss
        /// </summary>
        public int SSCount { get; set; }
        /// <summary>
        /// Liczba posiadanych s
        /// </summary>
        public int SCount { get; set; }
        /// <summary>
        /// Liczba posiadanych a
        /// </summary>
        public int ACount { get; set; }
        /// <summary>
        /// Liczba posiadanych b
        /// </summary>
        public int BCount { get; set; }
        /// <summary>
        /// Liczba posiadanych c
        /// </summary>
        public int CCount { get; set; }
        /// <summary>
        /// Liczba posiadanych d
        /// </summary>
        public int DCount { get; set; }
        /// <summary>
        /// Liczba posiadanych e
        /// </summary>
        public int ECount { get; set; }
        /// <summary>
        /// Waifu
        /// </summary>
        public CardFinalView Waifu { get; set; }
        /// <summary>
        /// Galeria
        /// </summary>
        public List<CardFinalView> Gallery { get; set; }
        /// <summary>
        /// Lista tagów jakie ma użytkownik na kartach
        /// </summary>
        public List<string> TagList { get; set; }
    }
}
