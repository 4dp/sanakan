using Newtonsoft.Json;
using System;
using System.IO;

namespace Sanakan.Config
{
    public class ConfigManager : IConfig
    {
        private ConfigModel _config;
        private string _path;

        public ConfigManager(string path)
        {
            _path = path;
            _config = Load();
        }

        public ConfigModel Get() => _config;

        public void Save()
        {
            using (StreamWriter sw = File.CreateText(_path))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    new JsonSerializer().Serialize(writer, _config);
                }
            }
        }

        private ConfigModel Load()
        {
            if (File.Exists(_path))
            {
                using (StreamReader sr = File.OpenText(_path))
                {
                    using (JsonReader reader = new JsonTextReader(sr))
                    {
                        return new JsonSerializer().Deserialize<ConfigModel>(reader);
                    }
                }
            }
            else
            {
                throw new Exception($"File \"{_path}\" not found!");
            }
        }

    }
}
