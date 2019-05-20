#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Services.PocketWaifu
{
    public class SafariImage
    {
        [JsonIgnore]public string PrevLocalImage => $"./Pictures/Poke/{Index}.jpg";
        [JsonIgnore]public string NextLocalImage => $"./Pictures/Poke/{Index}a.jpg";

        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("x")]
        public int X { get; set; }
        [JsonProperty("y")]
        public int Y { get; set; }
    }
}