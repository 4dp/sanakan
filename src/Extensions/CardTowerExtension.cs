#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;

namespace Sanakan.Extensions
{
    public static class CardTowerExtension
    {
        public static string GetTowerProfile(this Card card)
        {
            return $"**[{card.Id}]** {card.GetNameWithUrl()}\n\n"
                + $"⚡ {card.Profile.ActionPoints} **[{card.GetTowerRealMaxAP()}]**\n"
                + $"❤ {card.Profile.Health} **[{card.GetTowerRealMaxHealth()}]**\n"
                + $"🔋 {card.Profile.Energy} **[{card.GetTowerRealMaxEnergy()}]**\n"
                + $"🔥 {card.GetTowerRealMaxAttack()} *({card.GetTowerRealTrueDmg()})*\n"
                + $"🛡 {card.GetTowerRealMaxDefence()}\n"
                + $"🎲 {card.GetTowerRealLuck()}\n\n"
                + $"Dere: **{card.Dere}**\n"
                + $"Piętro: **{card.Profile.CurrentRoom.Floor.Id}**\n\n"
                + $"**Umiejętności/Zaklęcia**: {card.Profile.Spells.Count}\n\n"
                + $"**Przedmioty**: {string.Join("\n", card.Profile.Items.Where(x => x.Active).Select(x => x.GetTowerItemString()))}\n\n"
                + $"**Efekty**: {string.Join("\n", card.Profile.ActiveEffects.Where(x => x.Remaining > 0).Select(x => x.GetTowerEffectString()))}";
        }

        public static string GetTowerBaseStats(this Card card) => $"⚡ {card.Profile.ActionPoints} ❤ {card.Profile.Health} 🔋 {card.Profile.Energy}";

        public static string ToTowerParamIcon(this EffectTarget target)
        {
            switch (target)
            {
                case EffectTarget.AP: return "⚡";
                case EffectTarget.Health: return "❤";
                case EffectTarget.Attack: return "🔥";
                case EffectTarget.Defence: return "🛡";
                case EffectTarget.Luck: return "🎲";
                case EffectTarget.Energy: return "🔋";
                case EffectTarget.TrueDmg: return "☄";
                default: return "??";
            }
        }

        public static string GetTowerEffectString(this EffectInProfile effect)
        {
            var per = (effect.Effect.ValueType == Database.Models.Tower.ValueType.Percent) ? "%" : "";
            return $"*[{effect.Remaining}T]* {effect.Effect.Name} {effect.Effect.Target.ToTowerParamIcon()} {effect.Multiplier + effect.Effect.Value}{per}";
        }

        public static string GetTowerItemString(this ItemInProfile item)
        {
            var per = (item.Item.Effect.ValueType == Database.Models.Tower.ValueType.Percent) ? "%" : "";
            return $"**[{item.ItemId}]** {item.Item.Name} {item.Item.Effect.Target.ToTowerParamIcon()} {item.Item.Effect.Value}{per}";
        }

        public static string GetTowerSpellString(this SpellInProfile spell)
        {
            var per = (spell.Spell.Effect.ValueType == Database.Models.Tower.ValueType.Percent) ? "%" : "";
            return $"**[{spell.SpellId}]** {spell.Spell.Name} {spell.Spell.Effect.Target.ToTowerParamIcon()} {spell.Spell.Effect.Value}{per} 🔋 {spell.Spell.EnergyCost}";
        }

        private static int GetTowerParamChange(this Card card, EffectTarget target, ChangeType change, Database.Models.Tower.ValueType type)
        {
            var fromItems = card.Profile.Items.Where(x => x.Active).Select(x => x.Item.Effect).Where(x => x.Change == change)
                .Where(x => x.Target == target).Where(x => x.ValueType == type).Sum(x => x.Value);

            var fromEffects = card.Profile.ActiveEffects.Where(x => x.Effect.Change == change).Where(x => x.Effect.Target == target)
                .Where(x => x.Effect.ValueType == type).Sum(x => x.Effect.Value * x.Multiplier);

            return fromItems + fromEffects;
        }

        private static int GetTowerBaseValueOfParam(this Card card, EffectTarget target)
        {
            switch (target)
            {
                case EffectTarget.AP: return 50;
                case EffectTarget.Energy: return 100;
                case EffectTarget.Attack: return card.GetAttackWithBonus();
                case EffectTarget.Defence: return card.GetDefenceWithBonus();
                case EffectTarget.Health: return card.GetHealthWithPenalty(false);

                default:
                case EffectTarget.Luck:
                case EffectTarget.TrueDmg:
                    return 0;
            }
        }

        public static int GetTowerValueOfParam(this Card card, EffectTarget target, ChangeType change)
        {
            var val = card.GetTowerBaseValueOfParam(target);
            val += card.GetTowerParamChange(target, change, Database.Models.Tower.ValueType.Normal);

            var pChange = val * card.GetTowerParamChange(target, change, Database.Models.Tower.ValueType.Percent) / 100;
            return val + pChange;
        }

        public static int GetTowerRealMaxHealth(this Card card) => card.GetTowerValueOfParam(EffectTarget.Health, ChangeType.ChangeMax);
        public static int GetTowerRealMaxEnergy(this Card card) => card.GetTowerValueOfParam(EffectTarget.Energy, ChangeType.ChangeMax);
        public static int GetTowerRealMaxDefence(this Card card) => card.GetTowerValueOfParam(EffectTarget.Defence, ChangeType.ChangeMax);
        public static int GetTowerRealMaxAttack(this Card card) => card.GetTowerValueOfParam(EffectTarget.Attack, ChangeType.ChangeMax);
        public static int GetTowerRealMaxAP(this Card card) => card.GetTowerValueOfParam(EffectTarget.AP, ChangeType.ChangeMax);

        public static int GetTowerRealLuck(this Card card) => card.GetTowerValueOfParam(EffectTarget.Luck, ChangeType.ChangeNow);
        public static int GetTowerRealTrueDmg(this Card card) => card.GetTowerValueOfParam(EffectTarget.TrueDmg, ChangeType.ChangeNow);

        public static TowerProfile GenerateTowerProfile(this Card card)
        {
            return new TowerProfile
            {
                ActionPoints = card.GetTowerBaseValueOfParam(EffectTarget.AP),
                Defence = card.GetTowerBaseValueOfParam(EffectTarget.Defence),
                Attack = card.GetTowerBaseValueOfParam(EffectTarget.Attack),
                Energy = card.GetTowerBaseValueOfParam(EffectTarget.Energy),
                Health = card.GetTowerBaseValueOfParam(EffectTarget.Health),
                Luck = card.GetTowerBaseValueOfParam(EffectTarget.Luck),
                Spells = new List<SpellInProfile>(),
                Items = new List<ItemInProfile>(),
                Enemies = new List<Enemy>(),
                CurrentEvent = null,
                Id = card.Id,
                MaxFloor = 0,
                ExpCnt = 0,
                Level = 0
            };
        }

        public static string GetRoomContent(this Room room)
        {
            switch (room.Type)
            {
                case RoomType.Empty:
                    return $"Wchodzisz do pustego pokoju, chyba nic tutaj nie zdziałasz. Chcesz chwilę odpocząć przed wyruszeniem w dalszą drogę?";
                case RoomType.Campfire:
                    return $"Znajdujesz pomieszczenie z rozpalonym ogniskiem, to chyba dobry moment na chwię odpoczynku. Chcesz zostać tu na chwilę?";
                case RoomType.BossBattle:
                    return $"Wkraczasz do areny z bosem, teraz nie ma już odwrotu.";
                case RoomType.Fight:
                    return $"Spotykasz przeciwników na swojej drodze, chcesz rozpocząć walkę?";
                case RoomType.Event:
                    return $"";

                default:
                case RoomType.Start:
                    return $"Nowe piętro - nowa przygoda!";
            }
        }

        public static List<Enemy> GetTowerNewEnemies(this Room room)
        {
            //TODO: generate enemies according to room
            return new List<Enemy>();
        }

        public static void RecoverFromRest(this Card card, bool big = false)
        {
            var prc = big ? 20 : 5;
            var max = card.GetTowerRealMaxHealth();

            var recValue = max * prc / 100;
            if ((card.Health + recValue) > max)
                recValue = max - card.Health;

            card.Health += recValue;
        }

        public static bool CheckLuck(this Card card, int chanceToWinInPromiles)
        {
            var realChance = chanceToWinInPromiles + card.GetTowerRealLuck();
            if (realChance > 1000) return true;
            if (realChance < 1) return false;

            return Services.Fun.TakeATry(1000 / realChance);
        }

        public static void MarkCurrentRoomAsConquered(this Card card)
        {
            var crr = $"{card.Profile.CurrentRoomId}";
            var cnq = card.Profile.ConqueredRoomsFromFloor.Split(";").ToList();

            if (!cnq.Any(x => x == crr))
            {
                cnq.Add(crr);
                card.Profile.ConqueredRoomsFromFloor = string.Join(";", cnq);
            }
        }
    }
}