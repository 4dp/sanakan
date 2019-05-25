using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Wszystkie publiczne polecenia bota
    /// </summary>
    public class Commands
    {
        /// <summary>
        /// Prefix bota
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// Moduły
        /// </summary>
        public List<Module> Modules { get; set; }
    }
}