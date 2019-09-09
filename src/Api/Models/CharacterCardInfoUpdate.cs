#pragma warning disable 1591

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Informacje o karcie
    /// </summary>
    public class CharacterCardInfoUpdate
    {
        /// <summary>
        /// Url do nowego obrazka karty
        /// </summary>
        public string ImageUrl { get; set; }
        /// <summary>
        /// Imię i nazwisko na karcie
        /// </summary>
        public string CharacterName { get; set; }
        /// <summary>
        /// Nazwa serii z której pochodzi karta
        /// </summary>
        public string CardSeriesTitle { get; set; }
    }
}