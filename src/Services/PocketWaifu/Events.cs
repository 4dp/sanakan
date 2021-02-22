#pragma warning disable 1591

using System;
using System.Collections.Generic;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Shinden;

namespace Sanakan.Services.PocketWaifu
{
    public enum EventType
    {
        MoreItems, MoreExp, IncAtk, IncDef, AddReset, NewCard,  // +
        None, ChangeDere, DecAtk, DecDef, DecAff, LoseCard      // -
    }

    public class Events
    {
        private static List<ulong> _titles = new List<ulong>
        {
            7431, 50646, 10831, 54081, 53776, 12434, 44867, 51100, 4961, 55260, 53382, 53685, 35405, 54195, 2763, 43864, 52427, 52111, 53257, 45085
        };

        private static Dictionary<CardExpedition, Dictionary<EventType, Tuple<int, int>>> _chanceOfEvent = new Dictionary<CardExpedition, Dictionary<EventType, Tuple<int, int>>>
        {
            {CardExpedition.NormalItemWithExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(0,    1500)},
                    {EventType.MoreExp,     new Tuple<int, int>(1500, 3900)},
                    {EventType.IncAtk,      new Tuple<int, int>(3900, 7400)},
                    {EventType.IncDef,      new Tuple<int, int>(7400, 10000)},
                    {EventType.AddReset,    new Tuple<int, int>(-1,   -2)},
                    {EventType.NewCard,     new Tuple<int, int>(-3,   -4)},
                    {EventType.ChangeDere,  new Tuple<int, int>(-5,   -6)},
                    {EventType.DecAtk,      new Tuple<int, int>(-7,   -8)},
                    {EventType.DecDef,      new Tuple<int, int>(-9,   -10)},
                    {EventType.DecAff,      new Tuple<int, int>(-11,  -12)},
                    {EventType.LoseCard,    new Tuple<int, int>(-13,  -14)},
                }
            },
            {CardExpedition.ExtremeItemWithExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(0,    900)},
                    {EventType.MoreExp,     new Tuple<int, int>(900,  1900)},
                    {EventType.IncAtk,      new Tuple<int, int>(1900, 3000)},
                    {EventType.IncDef,      new Tuple<int, int>(3000, 4000)},
                    {EventType.AddReset,    new Tuple<int, int>(4000, 4200)},
                    {EventType.NewCard,     new Tuple<int, int>(4200, 4300)},
                    {EventType.ChangeDere,  new Tuple<int, int>(4300, 5000)},
                    {EventType.DecAtk,      new Tuple<int, int>(5000, 6200)},
                    {EventType.DecDef,      new Tuple<int, int>(6200, 7400)},
                    {EventType.DecAff,      new Tuple<int, int>(7400, 9000)},
                    {EventType.LoseCard,    new Tuple<int, int>(9000, 10000)},
                }
            },
            {CardExpedition.DarkItemWithExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(0,    1000)},
                    {EventType.MoreExp,     new Tuple<int, int>(1000, 2500)},
                    {EventType.IncAtk,      new Tuple<int, int>(2500, 5000)},
                    {EventType.IncDef,      new Tuple<int, int>(5000, 7000)},
                    {EventType.AddReset,    new Tuple<int, int>(-1,   -2)},
                    {EventType.NewCard,     new Tuple<int, int>(7000, 7300)},
                    {EventType.ChangeDere,  new Tuple<int, int>(7300, 7900)},
                    {EventType.DecAtk,      new Tuple<int, int>(7900, 8500)},
                    {EventType.DecDef,      new Tuple<int, int>(8500, 9000)},
                    {EventType.DecAff,      new Tuple<int, int>(9000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-3,   -4)},
                }
            },
            {CardExpedition.LightItemWithExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(0,    1000)},
                    {EventType.MoreExp,     new Tuple<int, int>(1000, 2500)},
                    {EventType.IncAtk,      new Tuple<int, int>(2500, 5000)},
                    {EventType.IncDef,      new Tuple<int, int>(5000, 7000)},
                    {EventType.AddReset,    new Tuple<int, int>(-1,   -2)},
                    {EventType.NewCard,     new Tuple<int, int>(7000, 7300)},
                    {EventType.ChangeDere,  new Tuple<int, int>(7300, 7900)},
                    {EventType.DecAtk,      new Tuple<int, int>(7900, 8500)},
                    {EventType.DecDef,      new Tuple<int, int>(8500, 9000)},
                    {EventType.DecAff,      new Tuple<int, int>(9000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-3,   -4)},
                }
            },
            {CardExpedition.DarkItems, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(-1,   -2)},
                    {EventType.MoreExp,     new Tuple<int, int>(-3,   -4)},
                    {EventType.IncAtk,      new Tuple<int, int>(0,    2200)},
                    {EventType.IncDef,      new Tuple<int, int>(2200, 4100)},
                    {EventType.AddReset,    new Tuple<int, int>(-5,   -6)},
                    {EventType.NewCard,     new Tuple<int, int>(4100, 4400)},
                    {EventType.ChangeDere,  new Tuple<int, int>(4400, 5400)},
                    {EventType.DecAtk,      new Tuple<int, int>(5400, 6600)},
                    {EventType.DecDef,      new Tuple<int, int>(6600, 8000)},
                    {EventType.DecAff,      new Tuple<int, int>(8000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-7,   -8)},
                }
            },
            {CardExpedition.LightItems, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(-1,   -2)},
                    {EventType.MoreExp,     new Tuple<int, int>(-3,   -4)},
                    {EventType.IncAtk,      new Tuple<int, int>(0,    2200)},
                    {EventType.IncDef,      new Tuple<int, int>(2200, 4100)},
                    {EventType.AddReset,    new Tuple<int, int>(-5,   -6)},
                    {EventType.NewCard,     new Tuple<int, int>(4100, 4400)},
                    {EventType.ChangeDere,  new Tuple<int, int>(4400, 5400)},
                    {EventType.DecAtk,      new Tuple<int, int>(5400, 6600)},
                    {EventType.DecDef,      new Tuple<int, int>(6600, 8000)},
                    {EventType.DecAff,      new Tuple<int, int>(8000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-7,   -8)},
                }
            },
            {CardExpedition.DarkExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(-1,   -2)},
                    {EventType.MoreExp,     new Tuple<int, int>(-3,   -4)},
                    {EventType.IncAtk,      new Tuple<int, int>(0,    2200)},
                    {EventType.IncDef,      new Tuple<int, int>(2200, 4100)},
                    {EventType.AddReset,    new Tuple<int, int>(-5,   -6)},
                    {EventType.NewCard,     new Tuple<int, int>(4100, 4400)},
                    {EventType.ChangeDere,  new Tuple<int, int>(4400, 5400)},
                    {EventType.DecAtk,      new Tuple<int, int>(5400, 6600)},
                    {EventType.DecDef,      new Tuple<int, int>(6600, 8000)},
                    {EventType.DecAff,      new Tuple<int, int>(8000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-7,   -8)},
                }
            },
            {CardExpedition.LightExp, new Dictionary<EventType, Tuple<int, int>>
                {
                    {EventType.MoreItems,   new Tuple<int, int>(-1,   -2)},
                    {EventType.MoreExp,     new Tuple<int, int>(-3,   -4)},
                    {EventType.IncAtk,      new Tuple<int, int>(0,    2200)},
                    {EventType.IncDef,      new Tuple<int, int>(2200, 4100)},
                    {EventType.AddReset,    new Tuple<int, int>(-5,   -6)},
                    {EventType.NewCard,     new Tuple<int, int>(4100, 4400)},
                    {EventType.ChangeDere,  new Tuple<int, int>(4400, 5400)},
                    {EventType.DecAtk,      new Tuple<int, int>(5400, 6600)},
                    {EventType.DecDef,      new Tuple<int, int>(6600, 8000)},
                    {EventType.DecAff,      new Tuple<int, int>(8000, 10000)},
                    {EventType.LoseCard,    new Tuple<int, int>(-7,   -8)},
                }
            }
        };

        private ShindenClient _shClient;

        public Events(ShindenClient client)
        {
            _shClient = client;
        }

        private EventType CheckChanceBasedOnTime(CardExpedition expedition, Tuple<double, double> duration)
        {
            switch (expedition)
            {
                case CardExpedition.ExtremeItemWithExp:
                    if (duration.Item1 > 45 || duration.Item2 > 240)
                    {
                        if (Services.Fun.TakeATry(2))
                            return EventType.LoseCard;
                    }
                    return EventType.None;

                default:
                    return EventType.None;
            }
        }

        public EventType RandomizeEvent(CardExpedition expedition, Tuple<double, double> duration)
        {
            var timeBased = CheckChanceBasedOnTime(expedition, duration);
            if (timeBased != EventType.None) return timeBased;

            var c = _chanceOfEvent[expedition];

            switch (Fun.GetRandomValue(10000))
            {
                case int n when (n < c[EventType.MoreItems].Item2
                                && n >= c[EventType.MoreItems].Item1):
                    return EventType.MoreItems;

                case int n when (n < c[EventType.MoreExp].Item2
                                && n >= c[EventType.MoreExp].Item1):
                    return EventType.MoreExp;

                case int n when (n < c[EventType.IncAtk].Item2
                                && n >= c[EventType.IncAtk].Item1):
                    return EventType.IncAtk;

                case int n when (n < c[EventType.IncDef].Item2
                                && n >= c[EventType.IncDef].Item1):
                    return EventType.IncDef;

                case int n when (n < c[EventType.AddReset].Item2
                                && n >= c[EventType.AddReset].Item1):
                    return EventType.AddReset;

                case int n when (n < c[EventType.NewCard].Item2
                                && n >= c[EventType.NewCard].Item1):
                    return EventType.NewCard;

                case int n when (n < c[EventType.ChangeDere].Item2
                                && n >= c[EventType.ChangeDere].Item1):
                    return EventType.ChangeDere;

                case int n when (n < c[EventType.DecAtk].Item2
                                && n >= c[EventType.DecAtk].Item1):
                    return EventType.DecAtk;

                case int n when (n < c[EventType.DecDef].Item2
                                && n >= c[EventType.DecDef].Item1):
                    return EventType.DecDef;

                case int n when (n < c[EventType.DecAff].Item2
                                && n >= c[EventType.DecAff].Item1):
                    return EventType.DecAff;

                case int n when (n < c[EventType.LoseCard].Item2
                                && n >= c[EventType.LoseCard].Item1):
                    return EventType.LoseCard;

                default: return EventType.None;
            }
        }

        public bool ExecuteEvent(EventType e, User user, Card card, ref string msg)
        {
            var aVal = Services.Fun.GetRandomValue(1, 4);

            switch (e)
            {
                case EventType.NewCard:
                {
                    var boosterPack = new BoosterPack
                    {
                        RarityExcludedFromPack = new List<RarityExcluded>(),
                        Title = Services.Fun.GetOneRandomFrom(_titles),
                        Characters = new List<BoosterPackCharacter>(),
                        CardSourceFromPack = CardSource.Expedition,
                        Name = "Losowa karta z wyprawy",
                        IsCardFromPackTradable = true,
                        MinRarity = Rarity.E,
                        CardCnt = 1
                    };

                    user.GameDeck.BoosterPacks.Add(boosterPack);
                    msg += "Wydarzenie: Pakit z kartą.\n";
                }
                break;

                case EventType.IncAtk:
                {
                    var max = card.Rarity.GetAttackMax();
                    card.Attack += aVal;

                    if (card.Attack > max)
                        card.Attack = max;

                    msg += $"Wydarzenie: Zwiększenie ataku do {card.Attack}.\n";
                }
                break;

                case EventType.IncDef:
                {
                    var max = card.Rarity.GetDefenceMax();
                    card.Defence += aVal;

                    if (card.Defence > max)
                        card.Defence = max;

                    msg += $"Wydarzenie: Zwiększenie obrony do {card.Defence}.\n";
                }
                break;

                case EventType.MoreExp:
                {
                    var addExp = Services.Fun.GetRandomValue(1, 5);
                    card.ExpCnt += addExp;

                    msg += $"Wydarzenie: Dodatkowe punkty doświadczenia. (+{addExp} exp)\n";
                }
                break;

                case EventType.MoreItems:
                {
                    msg += "Wydarzenie: Dodatkowe przedmioty.\n";
                }
                break;

                case EventType.AddReset:
                {
                    ++card.RestartCnt;
                    msg += "Wydarzenie: Zwiększenie ilości restartów karty.\n";
                }
                break;

                case EventType.ChangeDere:
                {
                    msg += "Wydarzenie: Zmiana dere na ";
                }
                break;

                case EventType.DecAff:
                {
                    card.Affection -= aVal;
                    msg += "Wydarzenie: Zmniejszenie relacji.\n";
                }
                break;

                case EventType.DecAtk:
                {
                    var min = card.Rarity.GetAttackMin();
                    card.Attack -= aVal;

                    if (card.Attack < min)
                        card.Attack = min;

                    msg += $"Wydarzenie: Zmniejszenie ataku obrony do {card.Attack}.\n";
                }
                break;

                case EventType.DecDef:
                {
                    var min = card.Rarity.GetDefenceMin();
                    card.Defence -= aVal;

                    if (card.Defence < min)
                        card.Defence = min;

                    msg += $"Wydarzenie: Zmniejszenie obrony do {card.Defence}.\n";
                }
                break;

                case EventType.LoseCard:
                {
                    user.GameDeck.Cards.Remove(card);
                    msg += "Wydarzenie: Utrata karty.\n";
                }
                return false;

                default:
                    return true;
            }

            return true;
        }

        public int GetMoreItems(EventType e)
        {
            switch (e)
            {
                case EventType.MoreItems:
                    return Services.Fun.GetRandomValue(2, 8);

                default:
                    return 0;
            }
        }
    }
}
