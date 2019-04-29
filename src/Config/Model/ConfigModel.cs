#pragma warning disable 1591

namespace Sanakan.Config.Model
{
    public class ConfigModel
    {
        public string Prefix { get; set; }
        public string BotToken { get; set; }
        public bool Supervision { get; set; }
        public bool Demonization { get; set; }
        public string ConnectionString { get; set; }
        public ConfigShinden Shinden { get; set; }
    }
}