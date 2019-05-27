#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models
{
    public enum SlotMachineBeat : int
    {
        b1 = 1, b10 = 10, b100 = 100
    }

    public enum SlotMachineBeatMultiplier : int
    {
        x1 = 1, x2 = 2, x3 = 3
    }

    public enum SlotMachineSelectedRows : int
    {
        r1 = 1, r2 = 2, r3 = 3
    }

    public class SlotMachineConfig
    {
        public ulong Id { get; set; }
        public long PsayMode { get; set; }
        public SlotMachineBeat Beat { get; set; }
        public SlotMachineSelectedRows Rows { get; set; }
        public SlotMachineBeatMultiplier Multiplier { get; set; }

        public ulong UserId { get; set; }
        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
