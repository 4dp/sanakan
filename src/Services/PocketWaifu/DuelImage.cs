#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Services.PocketWaifu
{
    public class DuelImage
    {
        public string Uri(int side) => $"./Pictures/Duel/{Name}{side}.jpg";

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("text-color")]
        public string Color { get; set; }
    }
}