#pragma warning disable 1591

using Newtonsoft.Json;

namespace Sanakan.Database.Models.Tower
{
    public enum SpellTarget
    {
        Self,
        Ally,
        Enemy,
        AllyGroup,
        EnemyGroup
    }

    public class Spell
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public SpellTarget Target { get; set; }
        public int EnergyCost { get; set; }

        public ulong EffectId { get; set; }
        [JsonIgnore]
        public virtual Effect Effect { get; set; }
    }
}
