using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Polecenie
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Przykład użycia
        /// </summary>
        public string Example { get; set; }
        /// <summary>
        /// Opis
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Głowna nazwa
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Alternatywne nazwy
        /// </summary>
        public List<string> Aliases { get; set; }
        /// <summary>
        /// Atrybuty polecenia
        /// </summary>
        public List<CommandAttribute> Attributes { get; set; }
    }
}