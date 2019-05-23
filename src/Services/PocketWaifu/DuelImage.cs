#pragma warning disable 1591

using Newtonsoft.Json;
using System.IO;

namespace Sanakan.Services.PocketWaifu
{
    public class DuelImage
    {
        private string ThisUri(int side) => $"./Pictures/Duel/{Name}{side}.jpg";
        public static string DefaultUri(int side) => $"./Pictures/PW/duel{side}.jpg";
        public static string DefaultColor() => "#aaaaaa";

        public string Uri(int side) => File.Exists(ThisUri(side)) ? ThisUri(side) : DefaultUri(side);

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("text-color")]
        public string Color { get; set; }
    }
}