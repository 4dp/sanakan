using System.Collections.Generic;

namespace Sanakan.Api.Models
{
    /// <summary>
    /// Moduł bota
    /// </summary>
    public class Module
    {
        /// <summary>
        /// Nazwa modułu
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Podmoduły
        /// </summary>
        public List<SubModule> SubModules { get; set; }
    }
}