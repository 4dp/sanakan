#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;
using System;
using System.Collections.Generic;

namespace Sanakan.Extensions
{
    public static class FloorExtension
    {
        public static Floor NewFloor(this Floor u, ulong floorLevel)
        {
            return new Floor
            {
                Id = floorLevel,
                UserIdFirstToBeat = 0,
                CreateDate = DateTime.Now,
                Boss = NewBoss(floorLevel),
                BeatDate = DateTime.MinValue,
                Rooms = GenerateFloorRooms(floorLevel),
            };
        }

        public static Enemy GetBossOfFloor(this Floor u)
        {
            return new Enemy
            {
                Dere = u.Boss.Dere,
                Loot = u.Boss.Loot,
                Name = u.Boss.Name,
                Level = u.Boss.Level,
                Type = EnemyType.Boss,
                Attack = u.Boss.Attack,
                Energy = u.Boss.Energy,
                Health = u.Boss.Health,
                Spells = u.Boss.Spells,
                Defence = u.Boss.Defence,
                LootType = u.Boss.LootType,
            };
        }

        public static int GetTowerMinEnergy(this Floor floor) => GetTowerMinEnergy(floor.Id);
        public static int GetTowerMaxEnergy(this Floor floor) => GetTowerMaxEnergy(floor.Id);
        public static int GetTowerMinAttack(this Floor floor) => GetTowerMinAttack(floor.Id);
        public static int GetTowerMaxAttack(this Floor floor) => GetTowerMaxAttack(floor.Id);
        public static int GetTowerMinDefence(this Floor floor) => GetTowerMinDefence(floor.Id);
        public static int GetTowerMaxDefence(this Floor floor) => GetTowerMaxDefence(floor.Id);
        public static int GetTowerMinHp(this Floor floor) => GetTowerMinHp(floor.Id);
        public static int GetTowerMaxHp(this Floor floor) => GetTowerMaxHp(floor.Id);

        internal static int GetTowerMinEnergy(ulong floor) => 25 + (int)(floor / 35);
        internal static int GetTowerMaxEnergy(ulong floor) => GetTowerMinEnergy(floor) + (int)(floor / 10);

        internal static int GetTowerMinAttack(ulong floor) => 10 + (int)(floor * 2);
        internal static int GetTowerMaxAttack(ulong floor) => GetTowerMinAttack(floor) + (int)(floor * 2);

        internal static int GetTowerMinDefence(ulong floor) => 5 + (int)(floor / 2);
        internal static int GetTowerMaxDefence(ulong floor) => GetTowerMinDefence(floor) + (int)(floor * 2);

        internal static int GetTowerMinHp(ulong floor) => 40 + (int)(floor * 8);
        internal static int GetTowerMaxHp(ulong floor) => GetTowerMinHp(floor) + (int)(floor * 6);

        internal static int GetTowerBossEnergy(ulong floor) => GetTowerMaxEnergy(floor) * 2;
        internal static int GetTowerBossAttack(ulong floor) => GetTowerMaxAttack(floor) * 2;
        internal static int GetTowerBossDefence(ulong floor) => GetTowerMaxDefence(floor) * 2;
        internal static int GetTowerBossHp(ulong floor) => GetTowerMaxHp(floor) * 3;

        private static List<Room> GenerateFloorRooms(ulong floorLevel)
        {
            //TODO: generate rooms to floor
            var item = new TowerItem
            {
                Level = floorLevel,
                Name = $"Mieczyk blasku",
                Rarity = ItemRairty.Magickal,
                UseType = ItemUseType.Wearable,
                Type = Database.Models.Tower.ItemType.Weapon,
                Effect = new Effect
                {
                    Value = 60,
                    Duration = 0,
                    Level = floorLevel,
                    Name = $"Atak blasku",
                    Target = EffectTarget.Attack,
                    Change = ChangeType.ChangeMax,
                    ValueType = Database.Models.Tower.ValueType.Normal
                }
            };

            var room1 = new Room
            {
                Count = 0,
                Item = null,
                IsHidden = false,
                Type = RoomType.Start,
                ItemType = ItemInRoomType.None,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room2 = new Room
            {
                Count = 0,
                Item = null,
                IsHidden = false,
                Type = RoomType.BossBattle,
                ItemType = ItemInRoomType.None,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room3 = new Room
            {
                Count = 2,
                Item = item,
                IsHidden = false,
                Type = RoomType.Fight,
                ItemType = ItemInRoomType.Loot,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room4 = new Room
            {
                Count = 0,
                Item = null,
                IsHidden = false,
                Type = RoomType.Campfire,
                ItemType = ItemInRoomType.None,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room5 = new Room
            {
                Count = 0,
                Item = null,
                IsHidden = false,
                Type = RoomType.Event,
                ItemType = ItemInRoomType.None,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room6 = new Room
            {
                Count = 0,
                Item = item,
                IsHidden = true,
                Type = RoomType.Empty,
                ItemType = ItemInRoomType.ToOpen,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            var room7 = new Room
            {
                Count = 1,
                Item = null,
                IsHidden = true,
                Type = RoomType.Treasure,
                ItemType = ItemInRoomType.None,
                ConnectedRooms = new List<RoomConnection>(),
                RetConnectedRooms = new List<RoomConnection>()
            };

            room1.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room3
            });

            room1.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room4
            });

            room4.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room5
            });

            room5.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room6
            });

            room5.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room7
            });

            room5.ConnectedRooms.Add(new RoomConnection
            {
                ConnectedRoom = room2
            });

            return new List<Room>() { room1, room2, room3, room4, room5, room6, room7 };
        }

        private static Enemy NewBoss(ulong floorLevel)
        {
            //TODO: generate boss/enemy to floor

            var boss = new Enemy
            {
                Profile = null,
                Loot = $"1|100",
                Level = floorLevel,
                Dere = Dere.Kuudere,
                Type = EnemyType.Boss,
                Name = "Pomniejszy bosik",
                LootType = LootType.TowerItem,
                Spells = new List<SpellInEnemy>(),
                Health = GetTowerBossHp(floorLevel),
                Energy = GetTowerBossEnergy(floorLevel),
                Attack = GetTowerBossAttack(floorLevel),
                Defence = GetTowerBossDefence(floorLevel),
            };

            boss.Spells.Add(new SpellInEnemy
            {
                Chance = 50,
                Spell = new Spell
                {
                    EnergyCost = 10,
                    Level = floorLevel,
                    Name = $"Pierdnięcie",
                    Target = SpellTarget.EnemyGroup,
                    Effect = new Effect
                    {
                        Value = 30,
                        Duration = 3,
                        Name = $"Smród",
                        Level = floorLevel,
                        Target = EffectTarget.Health,
                        Change = ChangeType.ChangeNow,
                        ValueType = Database.Models.Tower.ValueType.Normal,
                    }
                }
            });

            return boss;
        }
    }
}
