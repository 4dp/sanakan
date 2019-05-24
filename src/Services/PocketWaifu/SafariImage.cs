#pragma warning disable 1591

using System.IO;
using Newtonsoft.Json;

namespace Sanakan.Services.PocketWaifu
{
    public class SafariImage
    {
        public enum Type
        {
            Mystery, Truth
        }

        private string ThisUri(Type type)
        {
            switch (type)
            {
                case Type.Mystery:
                    return $"./Pictures/Poke/{Index}.jpg";

                default:
                case Type.Truth:
                    return $"./Pictures/Poke/{Index}a.jpg";
            }
        }

        public static string DefaultUri(Type type)
        {
            switch (type)
            {
                case Type.Mystery:
                    return $"./Pictures/PW/poke.jpg";

                default:
                case Type.Truth:
                    return $"./Pictures/PW/pokea.jpg";
            }
        }

        public static int DefaultX() => 884;
        public static int DefaultY() => 198;

        public int GetX() => File.Exists(ThisUri(Type.Truth)) ? X : DefaultX();
        public int GetY() => File.Exists(ThisUri(Type.Truth)) ? Y : DefaultY();

        public string Uri(Type type) => File.Exists(ThisUri(type)) ? ThisUri(type) : DefaultUri(type);

        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public int Y { get; set; }
    }
}