namespace Sanakan.Api.Models
{
    /// <summary>
    /// Stopka wiadomości - ostatnia linia
    /// </summary>
    public partial class RichMessageFooter
    {
        /// <summary>
        /// Tesks ostatniej lini
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Obrazek ostatniej lini
        /// </summary>
        public string ImageUrl { get; set; }
    }
}