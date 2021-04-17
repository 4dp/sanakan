#pragma warning disable 1591

using System;
using System.Collections.Generic;
using Sanakan.Database.Models;

namespace Sanakan.Extensions
{
    public static class TimeStatusExtension
    {
        private static Discord.Emote _toClaim = Discord.Emote.Parse("<:icon_empty:829327739034402817>");
        private static Discord.Emote _claimed = Discord.Emote.Parse("<:icon_full:829327738233421875>");

        private static List<StatusType> _dailyQuests = new List<StatusType>()
        {
            StatusType.DExpeditions,
            StatusType.DUsedItems,
            StatusType.DHourly,
            StatusType.DPacket,
            StatusType.DMarket,
            StatusType.DPvp,
        };

        private static List<StatusType> _weeklyQuests = new List<StatusType>()
        {
            StatusType.WCardPlus,
            StatusType.WDaily,
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

                case StatusType.WCardPlus:
                    return "Odbierz Karte+";

                case StatusType.WDaily:
                    return "Odbierz drobne";

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

                case StatusType.WCardPlus:      return 7;
                case StatusType.WDaily:         return 7;

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

                case StatusType.WCardPlus:      return "<a:miko:826132578703507526>";
                case StatusType.WDaily:         return "<a:gamemoney:465528603266777101>";

                default:
                    return "";
            }
        }

        public static string GetRewardString(this StatusType type)
        {
            switch (type)
            {
                case StatusType.DExpeditions:   return "1 AC";
                case StatusType.DUsedItems:     return "1 AC";
                case StatusType.DHourly:        return "100 SC";
                case StatusType.DPacket:        return "2 AC";
                case StatusType.DMarket:        return "2 AC";
                case StatusType.DPvp:           return "200 PC";

                case StatusType.WCardPlus:      return "50 AC";
                case StatusType.WDaily:         return "1000 SC i 10 AC";

                default:
                    return "";
            }
        }

        public static Discord.IEmote Icon(this StatusType type) => Discord.Emote.Parse(type.GetEmoteString());

        public static List<StatusType> GetDailyQuestTypes(this TimeStatus status) => _dailyQuests;

        public static List<StatusType> GetWeeklyQuestTypes(this TimeStatus status) => _weeklyQuests;

        public static bool IsQuest(this StatusType type) => type.IsWeeklyQuestType() || type.IsDailyQuestType();

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

        public static bool IsWeeklyQuestType(this StatusType type)
        {
            switch (type)
            {
                case StatusType.WCardPlus:
                case StatusType.WDaily:
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
            if (status.Type.IsQuest())
            {
                if (status.IsActive() && !status.BValue)
                {
                    status.IValue += times;
                }
                else if (!status.IsActive())
                {
                    status.IValue = times;
                    status.BValue = false;

                    if (status.Type.IsDailyQuestType())
                        status.EndsAt = DateTime.Now.Date.AddDays(1);

                    if (status.Type.IsWeeklyQuestType())
                        status.EndsAt = DateTime.Now.Date.AddDays(7 - (int) DateTime.Now.DayOfWeek);
                }
            }

            var max = status.Type.ToComplete();
            if (max > 0 && status.IValue > max)
                status.IValue = max;
        }

        public static void Claim(this TimeStatus status, User user)
        {
            if (status.BValue) return;

            status.BValue = true;

            switch(status.Type)
            {
                case StatusType.DExpeditions:
                case StatusType.DUsedItems:
                    user.AcCnt += 1;
                    break;

                case StatusType.DHourly:
                    user.ScCnt += 100;
                    break;

                case StatusType.DPacket:
                case StatusType.DMarket:
                    user.AcCnt += 2;
                    break;

                case StatusType.DPvp:
                    user.GameDeck.PVPCoins += 200;
                    break;

                case StatusType.WCardPlus:
                    user.AcCnt += 50;
                    break;

                case StatusType.WDaily:
                    user.ScCnt += 1000;
                    user.AcCnt += 10;
                    break;

                default:
                    break;
            }
        }

        public static bool IsClaimed(this TimeStatus status) => status.IsActive() && status.BValue;

        public static bool CanClaim(this TimeStatus status) => status.IsActive() && !status.BValue
            && status.Type.IsQuest() && status.IValue >= status.Type.ToComplete();

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
            if (status.Type.IsQuest())
            {
                long max = status.Type.ToComplete();
                long actualProgress = status.IsActive() ? status.IValue : 0;

                string progress = (actualProgress >= max) ? (status.BValue ? _claimed.ToString() : _toClaim.ToString())
                    : $"[{actualProgress}/{status.Type.ToComplete()}]";

                string reward = status.IsActive() && status.BValue ? "" : $"\nNagroda: `{status.Type.GetRewardString()}`";

                return $"{status.Type.Icon()} **{status.Type.Name()}** {progress}{reward}";
            }

            string dateValue = status.EndsAt.ToShortDateTime();
            if (status.HasEnded()) dateValue = "nieaktywne";

            return $"{status.Type.Name()} do `{dateValue}`";
        }
    }
}
