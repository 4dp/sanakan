using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Podmoduł bota
    /// </summary>
    public class SubModule
    {
        /// <summary>
        /// Prefix poprzedzajacy polecenie
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// Aliasy prefixu
        /// </summary>
        public List<string> PrefixAliases { get; set; }
        /// <summary>
        /// Polecenia modułu
        /// </summary>
        public List<Command> Commands { get; set; }
    }
}