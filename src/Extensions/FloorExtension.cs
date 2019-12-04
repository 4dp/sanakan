#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Database.Models.Tower;
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
            var minRooms = 20 + (int) floorLevel / 20;
            var maxRooms = 35 + (int) floorLevel / 10;
            if (minRooms < 20 || minRooms > 100) minRooms = 100;
            if (maxRooms < 35 || maxRooms > 200) maxRooms = 200;
            var roomsCount = Services.Fun.GetRandomValue(minRooms, maxRooms);

            // create unique item for hidden room
            var item = new TowerItem
            {
                Level = floorLevel,
                Rarity = ItemRairty.Legendary,
                UseType = ItemUseType.Wearable,
                Type = Database.Models.Tower.ItemType.Ring,
                Name = $"Pierścień szczęścia #{floorLevel}",
                Effect = new Effect
                {
                    Duration = 0,
                    Level = floorLevel,
                    Target = EffectTarget.Luck,
                    Change = ChangeType.ChangeNow,
                    Value = 10 + (int) floorLevel,
                    ValueType = Database.Models.Tower.ValueType.Normal,
                    Name = $"Blask pierścienia szczęścia #{floorLevel}",
                }
            };

            // create list with predefined special rooms
            // index 0: start room
            // index 1: boss room
            // index 2: hidden room
            var rooms = new List<Room>()
            {
                new Room
                {
                    Count = 0,
                    Item = null,
                    IsHidden = false,
                    Type = RoomType.Start,
                    ItemType = ItemInRoomType.None,
                    ConnectedRooms = new List<RoomConnection>(),
                    RetConnectedRooms = new List<RoomConnection>()
                },
                new Room
                {
                    Item = null,
                    IsHidden = false,
                    Type = RoomType.BossBattle,
                    ItemType = ItemInRoomType.None,
                    ConnectedRooms = new List<RoomConnection>(),
                    RetConnectedRooms = new List<RoomConnection>(),
                    Count = floorLevel > 100 ? Services.Fun.GetRandomValue(0, 4): 0,
                },
                new Room
                {
                    Count = 5,
                    Item = item,
                    IsHidden = true,
                    Type = RoomType.Treasure,
                    ItemType = ItemInRoomType.ToOpen,
                    ConnectedRooms = new List<RoomConnection>(),
                    RetConnectedRooms = new List<RoomConnection>()
                }
            };

            var randRooms = new List<Room>();
            var campfires = roomsCount / 15;
            for (int i = 0; i < campfires; i++)
            {
                randRooms.Add(new Room
                {
                    Count = 0,
                    Item = null,
                    IsHidden = false,
                    Type = RoomType.Campfire,
                    ItemType = ItemInRoomType.None,
                    ConnectedRooms = new List<RoomConnection>(),
                    RetConnectedRooms = new List<RoomConnection>()
                });
            }

            var treasures = roomsCount / 20;
            for (int i = 0; i < treasures; i++)
            {
                randRooms.Add(new Room
                {
                    Item = null,
                    IsHidden = false,
                    Type = RoomType.Treasure,
                    ItemType = ItemInRoomType.None,
                    Count = Services.Fun.GetRandomValue(1, 3),
                    ConnectedRooms = new List<RoomConnection>(),
                    RetConnectedRooms = new List<RoomConnection>()
                });
            }

            var events = roomsCount / 30;
            for (int i = 0; i < events; i++)
            {
                //TODO: generate event rooms
                // randRooms.Add(new Room
                // {
                //     Count = 0,
                //     Item = null,
                //     IsHidden = false,
                //     Type = RoomType.Event,
                //     ItemType = ItemInRoomType.None,
                //     ConnectedRooms = new List<RoomConnection>(),
                //     RetConnectedRooms = new List<RoomConnection>()
                // });
            }

            var leftRooms = roomsCount - 3 - campfires - treasures - events;
            for (int i = 0; i < leftRooms; i++)
            {
                if (Services.Fun.TakeATry(3))
                {
                    randRooms.Add(new Room
                    {
                        Count = 0,
                        Item = null,
                        IsHidden = false,
                        Type = RoomType.Empty,
                        ItemType = ItemInRoomType.None,
                        ConnectedRooms = new List<RoomConnection>(),
                        RetConnectedRooms = new List<RoomConnection>()
                    });
                }
                else
                {
                    randRooms.Add(new Room
                    {
                        Item = null,
                        IsHidden = false,
                        Type = RoomType.Fight,
                        ItemType = ItemInRoomType.None,
                        Count = Services.Fun.GetRandomValue(1, 5),
                        ConnectedRooms = new List<RoomConnection>(),
                        RetConnectedRooms = new List<RoomConnection>()
                    });
                }
            }

            // generate maze
            randRooms.Shuffle();
            var nextRooms = new List<Room>() { rooms[0] };

            do
            {
                var futureRooms = new List<Room>();
                for (int j = nextRooms.Count; j > 0; j--)
                {
                    var startRoom = nextRooms[j - 1];
                    nextRooms.Remove(startRoom);

                    var connectionsCnt = Services.Fun.GetRandomValue(1, 4);
                    if (nextRooms.Count == 0 || !Services.Fun.TakeATry(5))
                    {
                        for (int i = 0; i < connectionsCnt; i++)
                        {
                            if (randRooms.Count > 0)
                            {
                                var rN = Services.Fun.GetOneRandomFrom(randRooms);
                                randRooms.Remove(rN);
                                futureRooms.Add(rN);
                                rooms.Add(rN);

                                startRoom.ConnectedRooms.Add(new RoomConnection { ConnectedRoom = rN });
                                rN.ConnectedRooms.Add(new RoomConnection { ConnectedRoom = startRoom });
                            }
                        }
                    }
                }

                nextRooms.AddRange(futureRooms);
                futureRooms.Clear();

            } while (randRooms.Count > 0);

            // add special rooms
            var randRoom = Services.Fun.GetOneRandomFrom(rooms);
            rooms[2].ConnectedRooms.Add(new RoomConnection { ConnectedRoom = randRoom });
            randRoom.ConnectedRooms.Add(new RoomConnection { ConnectedRoom = rooms[2] });

            var lastRoom = rooms.Last();
            rooms[1].ConnectedRooms.Add(new RoomConnection { ConnectedRoom = lastRoom });
            lastRoom.ConnectedRooms.Add(new RoomConnection { ConnectedRoom = rooms[1] });

            return rooms;
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
