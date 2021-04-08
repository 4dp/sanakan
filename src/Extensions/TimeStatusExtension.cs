#pragma warning disable 1591

using System;
using System.Collections.Generic;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class TimeStatusExtension
    {
        private static List<StatusType> _dailyQuests = new List<StatusType>()
        {
            StatusType.DExpeditions,
            StatusType.DUsedItems,
            StatusType.DHourly,
            StatusType.DPacket,
            StatusType.DMarket,
            StatusType.DPvp,
        };

        public static string Name(this StatusType type)
        {
            switch (type)
            {
                case StatusType.Color:
                    return "Kolor";

                case StatusType.Globals:
                    return "Globalne emoty";

                case StatusType.DHourly:
                    return "Odbierz zaskórniaki";

                case StatusType.DExpeditions:
                    return "Wyślij karte na wyprawę";

                case StatusType.DMarket:
                    return "Odwiedź rynek lub czarny rynek";

                case StatusType.DPacket:
                    return "Otwórz pakiet kart";

                case StatusType.DPvp:
                    return "Rozegraj pojedynek PVP";

                case StatusType.DUsedItems:
                    return "Użyj przedmiot";

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

        public static int ToComplete(this StatusType type)
        {
            switch (type)
            {
                case StatusType.DExpeditions:   return 3;
                case StatusType.DUsedItems:     return 10;
                case StatusType.DHourly:        return 4;
                case StatusType.DPacket:        return 2;
                case StatusType.DMarket:        return 2;
                case StatusType.DPvp:           return 5;

                default:
                    return -1;
            }
        }

        public static string GetEmoteString(this StatusType type)
        {
            switch (type)
            {
                case StatusType.DExpeditions:   return "<:icon_expeditions:829327738124369930>";
                case StatusType.DUsedItems:     return "<:icon_items:829327738141409310>";
                case StatusType.DHourly:        return "<:icon_money:829327739340718121>";
                case StatusType.DPacket:        return "<:icon_packet:829327738585743400>";
                case StatusType.DMarket:        return "<:icon_market:829327738145210399>";
                case StatusType.DPvp:           return "<:icon_pvp:829327738157662229>";

                default:
                    return "";
            }
        }

        public static Discord.IEmote Icon(this StatusType type) => Discord.Emote.Parse(type.GetEmoteString());

        public static List<StatusType> GetQuestTypes(this TimeStatus status) => _dailyQuests;

        public static bool IsDailyQuestType(this StatusType type)
        {
            switch (type)
            {
                case StatusType.DExpeditions:
                case StatusType.DUsedItems:
                case StatusType.DHourly:
                case StatusType.DPacket:
                case StatusType.DMarket:
                case StatusType.DPvp:
                    return true;

                default:
                    return false;
            }
        }

        public static TimeStatus NewTimeStatus(this StatusType type, ulong guildId = 0) => new TimeStatus()
        {
            IValue = 0,
            Type = type,
            BValue = false,
            Guild = guildId,
            EndsAt = DateTime.MinValue,
        };

        public static void Reset(this TimeStatus status)
        {
            status.IValue = 0;
            status.BValue = false;
            status.EndsAt = DateTime.MinValue;
        }

        public static void Count(this TimeStatus status, int times = 1)
        {
            if (status.Type.IsDailyQuestType())
            {
                if (status.IsActive() && !status.BValue)
                {
                    status.IValue += times;
                }
                else
                {
                    status.IValue = times;
                    status.BValue = false;
                    status.EndsAt = DateTime.Now.Date.AddDays(1);
                }
            }

            var max = status.Type.ToComplete();
            if (max > 0 && status.IValue > max)
                status.IValue = max;
        }

        public static bool CanClaim(this TimeStatus status) => status.IsActive() && !status.BValue
            && status.Type.IsDailyQuestType() && status.IValue >= status.Type.ToComplete();

        public static double RemainingMinutes(this TimeStatus status)
            => (status.EndsAt - DateTime.Now).TotalMinutes;

        public static double RemainingSeconds(this TimeStatus status)
            => (status.EndsAt - DateTime.Now).TotalSeconds;

        public static bool IsSet(this TimeStatus status)
            => status.EndsAt !=  DateTime.MinValue;

        public static bool HasEnded(this TimeStatus status)
            => status.EndsAt < DateTime.Now;

        public static bool IsActive(this TimeStatus status)
            => status.IsSet() && !status.HasEnded();

        public static string ToView(this TimeStatus status)
        {
            if (status.Type.IsDailyQuestType())
            {
                long actualProgress = status.IsActive() ? status.IValue : 0;
                string claimed = status.IsActive() ? (status.BValue ? " *C*" : "") : "";
                return $"{status.Type.Icon()} **{status.Type.Name()}**: [{actualProgress}/{status.Type.ToComplete()}]{claimed}";
            }

            string dateValue = status.EndsAt.ToShortDateTime();
            if (status.HasEnded()) dateValue = "nieaktywne";

            return $"{status.Type.Name()} do `{dateValue}`";
        }
    }
}
