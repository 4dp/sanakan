#pragma warning disable 1591

using Sanakan.Config.Model;

namespace Sanakan.Config
{
    public class ConfigManager : IConfig
    {
        private ConfigModel _config;
        private JsonFileReader _reader;

        public ConfigManager(string path)
        {
            _reader = new JsonFileReader(path);
            _config = Load();
        }

        public ConfigModel Get() => _config;

        public void Save() => _reader.Save(_config);

        private ConfigModel Load()  => _reader.Load<ConfigModel>();
    }
}
