#pragma warning disable 1591

using System.Collections.Generic;
using System.Linq;
using Sanakan.Database.Models;

namespace Sanakan.Api.Models
{
    public enum OrderType
    {
        Id, IdDes, Name, NameDes, Rarity, RarityDes, Title, TitleDes, Health, HealthDes, Atack, AtackDes, Defence, DefenceDes
    }

    /// <summary>
    /// Filtrowanie listy kart
    /// </summary>
    public class CardsQueryFilter
    {
        /// <summary>
        /// Sortowanie po parametrze
        /// </summary>
        public OrderType OrderBy { get; set; }
        /// <summary>
        /// Tekst wyszukiwania
        /// </summary>
        public string SearchText { get; set; }
        /// <summary>
        /// Tagi jakie ma zawierać karta
        /// </summary>
        public List<string> IncludeTags { get; set; }
        /// <summary>
        /// Tagi jakich karta ma nie mieć
        /// </summary>
        public List<string> ExcludeTags { get; set; }

        public static IQueryable<Card> Use(OrderType type, IQueryable<Card> query)
        {
            switch (type)
            {
                case OrderType.Atack:
                    return query.OrderBy(x => x.Attack);
                case OrderType.AtackDes:
                    return query.OrderByDescending(x => x.Attack);
                case OrderType.Defence:
                    return query.OrderBy(x => x.Defence);
                case OrderType.DefenceDes:
                    return query.OrderByDescending(x => x.Defence);
                case OrderType.Health:
                    return query.OrderBy(x => x.Health);
                case OrderType.HealthDes:
                    return query.OrderByDescending(x => x.Health);
                case OrderType.Title:
                    return query.OrderBy(x => x.Title);
                case OrderType.TitleDes:
                    return query.OrderByDescending(x => x.Title);
                case OrderType.Rarity:
                    return query.OrderBy(x => x.Rarity);
                case OrderType.RarityDes:
                    return query.OrderByDescending(x => x.Rarity);
                case OrderType.Name:
                    return query.OrderBy(x => x.Name);
                case OrderType.NameDes:
                    return query.OrderByDescending(x => x.Name);
                case OrderType.IdDes:
                    return query.OrderByDescending(x => x.Id);

                default:
                case OrderType.Id:
                    return query;
            }
        }
    }
}