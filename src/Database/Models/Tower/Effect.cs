#pragma warning disable 1591

namespace Sanakan.Database.Models.Tower
{
    public enum EffectTarget
    {
        Defence,
        Health,
        Attack,
        Energy,
        Luck,
        AP,
        TrueDmg,
    }

    public enum ValueType
    {
        Normal,
        Percent
    }

    public enum ChangeType
    {
        ChangeNow,
        ChangeMax
    }

    public class Effect
    {
        public ulong Id { get; set; }
        public ulong Level { get; set; }
        public string Name { get; set; }
        public EffectTarget Target { get; set; }
        public ValueType ValueType { get; set; }
        public ChangeType Change { get; set; }
        public int Duration { get; set; }
        public int Value { get; set; }
    }
}
