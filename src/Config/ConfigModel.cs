#pragma warning disable 1591

namespace Sanakan.Config
{
    public class ConfigModel
    {
        public string Prefix { get; set; }
        public string BotToken { get; set; }
        public ConfigShinden Shinden { get; set; }
    }

    public class ConfigShinden
    {
        public string Token { get; set; }
        public string UserAgent { get; set; }
        public string Marmolade { get; set; }
    }
}
