#pragma warning disable 1591

namespace Sanakan.Database.Models.Tower
{
    public enum EffectTarget
    {
        Defence,
        Health,
        Attack,
        Energy
    }

    public class Effect
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public EffectTarget Target { get; set; }
        public int Duration { get; set; }
        public int Value { get; set; }
    }
}
