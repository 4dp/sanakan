using System;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class TimeStatusExtension
    {
        public static string Name(this StatusType type)
        {
            switch (type)
            {
                case StatusType.Color:
                    return "Kolor";

                case StatusType.Globals:
                    return "Globalne emoty";

                default:
                    return "--";
            }
        }

        public static bool IsSubType(this StatusType type)
        {
            switch (type)
            {
                case StatusType.Color:
                case StatusType.Globals:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsSet(this TimeStatus status)
            => status.EndsAt !=  DateTime.MinValue;

        public static bool HasEnded(this TimeStatus status)
            => status.EndsAt < DateTime.Now;

        public static bool IsActive(this TimeStatus status)
            => status.IsSet() && !status.HasEnded();

        public static string ToView(this TimeStatus status)
        {
            string dateValue = status.EndsAt.ToShortDateTime();
            if (status.HasEnded())
                dateValue = "nieaktywne";

            return $"{status.Type.Name()} do `{dateValue}`";   
        }
    }
}
