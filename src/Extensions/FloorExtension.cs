#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;
using Sanakan.Services;
using System;
using System.Collections.Generic;
using System.Linq;

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
                Count = 0,
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
                ConnectedRoom = room2
            });

            return new List<Room>() { room1, room2, room3, room4, room5, room6 };;
        }

        private static Enemy NewBoss(ulong floorLevel)
        {
            //TODO: generate boss/enemy to floor

            var boss = new Enemy
            {
                Attack = 100,
                Energy = 100,
                Health = 100,
                Defence = 100,
                Profile = null,
                Loot = $"1|100",
                Level = floorLevel,
                Dere = Dere.Kuudere,
                Type = EnemyType.Boss,
                Name = "Pomniejszy bosik",
                LootType = LootType.TowerItem,
                Spells = new List<SpellInEnemy>()
            };

            boss.Spells.Add(new SpellInEnemy
            {
                Chance = 50,
                Spell = new Spell
                {
                    EnergyCost = 10,
                    Level = floorLevel,
                    Name = $"Pierdnięcie",
                    Target = SpellTarget.AllyGroup,
                    Effect = new Effect
                    {
                        Value = -30,
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
