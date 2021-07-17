#pragma warning disable 1591

using System;
using System.IO;
using Newtonsoft.Json;

namespace Sanakan.Config
{
    public class JsonFileReader
    {
        private const string DEFAULT = "./file.json";
        private readonly string _path;

        public JsonFileReader() => _path = null;

        public JsonFileReader(string path) => _path = path;

        public T Load<T>(string path = DEFAULT)
        {
            if (_path != null && path == DEFAULT)
                path = _path;

            if (!File.Exists(path))
                throw new Exception($"File \"{path}\" not found!");

            using (StreamReader sr = File.OpenText(path))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    return new JsonSerializer().Deserialize<T>(reader);
                }
            }
        }

        public void Save<T>(T obj, string path = DEFAULT)
        {
            if (_path != null && path == DEFAULT)
                path = _path;

            using (StreamWriter sw = File.CreateText(path))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    new JsonSerializer().Serialize(writer, obj);
                }
            }
        }
    }
}