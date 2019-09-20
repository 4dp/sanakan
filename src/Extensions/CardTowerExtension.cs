#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;
using Sanakan.Services;
using Sanakan.Services.PocketWaifu;

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
                + $"**Zaklęcia**: {string.Join("\n", card.Profile.Spells.Select(x => x.GetTowerSpellString()))}\n\n"
                + $"**Przedmioty**: {string.Join("\n", card.Profile.Items.Where(x => x.Active).Select(x => x.GetTowerItemString()))}\n\n"
                + $"**Efekty**: {string.Join("\n", card.Profile.ActiveEffects.Where(x => x.Remaining > 0).Select(x => x.GetTowerEffectString()))}"
                .TrimToLength(2000);
        }

        public static string GetTowerBaseStats(this Card card)
            => $"⚡ {card.Profile.ActionPoints} ❤ {card.Profile.Health} 🔋 {card.Profile.Energy} 🔥 {card.GetTowerRealMaxAttack()} 🛡 {card.GetTowerRealMaxDefence()}";

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

        public static string GetTowerEnemiesString(this Card card)
        {
            var enemies = card.Profile.Enemies;
            if (enemies.Count < 1) return null;

            string toReturn = "";
            foreach (var enemy in enemies)
                toReturn += $"**[{enemy.Id}]** *{enemy.Name}* ❤{enemy.Health} 🔥{enemy.Attack} 🛡{enemy.Defence} 🔋{enemy.Energy}\n";

            return toReturn;
        }

        public static string GetRoomContent(this Room room, string more = null)
        {
            var itemString = room.GetRoomItemString();

            switch (room.Type)
            {
                case RoomType.Empty:
                    return $"Wchodzisz do pustego pokoju, chyba nic tutaj nie zdziałasz. Chcesz chwilę odpocząć przed wyruszeniem w dalszą drogę?{itemString}";
                case RoomType.Campfire:
                    return $"Znajdujesz pomieszczenie z rozpalonym ogniskiem, to chyba dobry moment na chwię odpoczynku. Chcesz zostać tu na chwilę?{itemString}";
                case RoomType.BossBattle:
                    return $"Wkraczasz do areny z bosem, teraz nie ma już odwrotu.\n{more}";
                case RoomType.Fight:
                    return $"Spotykasz przeciwników na swojej drodze, chcesz rozpocząć walkę?";
                case RoomType.Treasure:
                    return $"Udało Ci się odnaleźć pokój z skarbem, chcesz spróbować otworzyć skrzynię?";
                case RoomType.Event:
                    return $"{more}";

                default:
                case RoomType.Start:
                    return $"Nowe piętro - nowa przygoda!{itemString}";
            }
        }

        private static string GetRoomItemString(this Room room)
        {
            switch (room.ItemType)
            {
                case ItemInRoomType.Loot:
                    return $"\nOtrzymałeś przedmiot: *{room.Item.Name}*";

                default: return "";
            }
        }

        public static List<Enemy> GetTowerNewEnemies(this Room room, Waifu waifuService, IEnumerable<string> names)
        {
            var list = new List<Enemy>();

            int baseEng = 25;
            int baseAtk = 10 + (int)(room.FloorId / 2);
            int baseDef = 5 + (int)(room.FloorId / 6);
            int baseHp = 40 + (int)((room.FloorId / 4) * 3);

            int maxEng = baseEng + (int)(room.FloorId / 2);
            int maxAtk = baseAtk + (int)(room.FloorId * 4);
            int maxDef = baseDef + (int)(room.FloorId * 2);
            int maxHp = baseHp + (int)(room.FloorId * 6);

            for (int i = 0; i < room.Count; i++)
            {
                list.Add(new Enemy
                {
                    Loot = null,
                    Level = room.FloorId,
                    LootType = LootType.None,
                    Type = EnemyType.Normall,
                    Spells = new List<SpellInEnemy>(),
                    Name = Fun.GetOneRandomFrom(names),
                    Dere = waifuService.RandomizeDere(),
                    Health = Fun.GetRandomValue(baseHp, maxHp),
                    Attack = Fun.GetRandomValue(baseAtk, maxAtk),
                    Energy = Fun.GetRandomValue(baseEng, maxEng),
                    Defence = Fun.GetRandomValue(baseDef, maxDef),
                });

                //TODO: add skills after some point
            }

            return list;
        }

        public static Event GetTowerEvent(this Room room, IEnumerable<Event> events)
        {
            var viable = events.Where(x => x.Start);
            if (viable.Count() < 1)
                return null;

            return Fun.GetOneRandomFrom(viable);
        }

        public static void RecoverFromRest(this Card card, bool big = false)
        {
            var prc = big ? 18 : 4;
            var maxH = card.GetTowerRealMaxHealth();
            var maxE = card.GetTowerRealMaxEnergy();

            var recValueH = maxH * prc / 100;
            if ((card.Profile.Health + recValueH) > maxH)
                recValueH = maxH - card.Profile.Health;

            card.Profile.Health += recValueH;

            var recValueE = maxE * prc / 100;
            if ((card.Profile.Energy + recValueE) > maxE)
                recValueE = maxE - card.Profile.Energy;

            card.Profile.Energy += recValueE;
        }

        private static int GetRealMaxValueOfParam(this Effect effect, Card card)
        {
            switch (effect.Target)
            {
                case EffectTarget.AP:
                    return card.GetTowerRealMaxAP();
                case EffectTarget.Attack:
                    return card.GetTowerRealMaxAttack();
                case EffectTarget.Defence:
                    return card.GetTowerRealMaxDefence();
                case EffectTarget.Energy:
                    return card.GetTowerRealMaxEnergy();
                case EffectTarget.Health:
                    return card.GetTowerRealMaxHealth();
                case EffectTarget.Luck:
                    return card.GetTowerRealLuck();
                case EffectTarget.TrueDmg:
                    return card.GetTowerRealTrueDmg();

                default:
                    return 0;
            }
        }

        private static int GetRealEffectChangeValue(this Effect effect, Card card)
        {
            int maxValue = effect.GetRealMaxValueOfParam(card);
            if (maxValue <= 0) return 0;

            int realValue = effect.Value;
            if (effect.ValueType == ValueType.Percent)
                realValue = maxValue * effect.Value / 100;

            return realValue;
        }

        private static string InflictStaticEffect(this Card card, Effect effect)
        {
            var thisEffect = card.Profile.ActiveEffects.FirstOrDefault(x => x.EffectId == effect.Id);
            if (thisEffect == null)
            {
                thisEffect = new EffectInProfile
                {
                    Multiplier = 1,
                    Effect = effect,
                    Remaining = effect.Duration,
                };
                card.Profile.ActiveEffects.Add(thisEffect);
            }
            else thisEffect.Remaining = effect.Duration;
            return $"Nałożono efekt: {effect.Name} na {effect.Duration} tur.";
        }

        private static string InflictInstantEffect(this Card card, Effect effect, int multiplier = 1)
        {
            var change = effect.GetRealEffectChangeValue(card) * multiplier;
            var maxValue = effect.GetRealMaxValueOfParam(card);

            switch (effect.Target)
            {
                case EffectTarget.AP:
                    card.Profile.ActionPoints += change;
                    if (card.Profile.ActionPoints < 0)
                        card.Profile.ActionPoints = 0;

                    if (card.Profile.ActionPoints > maxValue)
                        card.Profile.ActionPoints = maxValue;
                    return $"Liczba punktów akcji zmieniła się o {change}";

                case EffectTarget.Energy:
                    card.Profile.Energy += change;
                    if (card.Profile.Energy < 0)
                        card.Profile.Energy = 0;

                    if (card.Profile.Energy > maxValue)
                        card.Profile.Energy = maxValue;
                    return $"Liczba punktów energii zmieniła się o {change}";

                case EffectTarget.Health:
                    card.Profile.Health += change;
                    if (card.Profile.Health < 0)
                        card.Profile.Health = 0;

                    if (card.Profile.Health > maxValue)
                        card.Profile.Health = maxValue;
                    return $"Liczba punktów życia zmieniła się o {change}";

                default: return "";
            }
        }

        public static string InflictEffect(this Card card, Effect effect, bool onlyActive = false, int multiplier = 1)
        {
            switch (effect.Change)
            {
                case ChangeType.ChangeMax:
                    if (onlyActive) return "";
                    return card.InflictStaticEffect(effect);

                default:
                    if (effect.Duration > 1 && !onlyActive)
                        return card.InflictStaticEffect(effect);

                    return card.InflictInstantEffect(effect, multiplier);
            }
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

        public static void RestartTowerFloor(this Card card)
        {
            var start = card.Profile.CurrentRoom.Floor.Rooms.FirstOrDefault(x => x.Type == RoomType.Start);

            card.Profile.Enemies.Clear();
            card.Profile.CurrentEvent = null;
            card.Profile.CurrentRoomId = start.Id;
            card.Profile.ConqueredRoomsFromFloor = $"{start.Id}";
            card.Profile.Health = card.GetTowerBaseValueOfParam(EffectTarget.Health);
            card.Profile.Energy = card.GetTowerBaseValueOfParam(EffectTarget.Energy);
            card.Profile.Defence = card.GetTowerBaseValueOfParam(EffectTarget.Defence);
        }

        public static int DealDmgToEnemy(this Card card, Enemy enemy, int? customDmg = null)
        {
            var dmg = customDmg ?? card.GetTowerRealMaxAttack();
            dmg -= enemy.Defence;

            if (enemy.Dere.IsWeakTo(card.Dere))
                dmg *= 2;

            if (enemy.Dere.IsResistTo(card.Dere))
                dmg /= 2;

            if (dmg < 1)
                dmg = 1;

            if (customDmg == null)
                dmg += card.GetTowerRealTrueDmg();

            enemy.Health -= dmg;
            return dmg;
        }

        public static int ReciveDmgFromEnemy(this Card card, Enemy enemy)
        {
            var dmg = enemy.Attack;
            dmg -= card.GetTowerRealMaxDefence();

            if (card.Dere.IsWeakTo(enemy.Dere))
                dmg *= 2;

            if (card.Dere.IsResistTo(enemy.Dere))
                dmg /= 2;

            if (dmg < 1)
                dmg = 1;

            card.Profile.Health -= dmg;
            return dmg;
        }
    }
}