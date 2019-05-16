#pragma warning disable 1591

using Sanakan.Database.Models;
using Sanakan.Services;
using System;

namespace Sanakan.Extensions
{
    public static class SlotMachineExtension
    {
        public static string Icon(this SlotMachineSlots slot, bool psay = false)
        {
            switch(slot)
            {
                case SlotMachineSlots.j: return psay ? "<:psajajaj:481762467534471178>" : "⭐";
                case SlotMachineSlots.z: return psay ? "<:rozowypsaj:481757430943055892>" : "🍑";
                case SlotMachineSlots.g: return psay ? "<:fioletowypsaj:481756959419400192>" : "🍒";
                case SlotMachineSlots.f: return psay ? "<:niebieskipsaj:481758813024681994>" : "🦋";
                case SlotMachineSlots.c: return psay ? "<:zielonypsaj:481757219394813952>" : "🐸";
                case SlotMachineSlots.p: return psay ? "<:brazowypsaj:482158913744142368>" : "🐷";
                case SlotMachineSlots.q: return "<:klasycznypsaj:482136878120828938>";
                default: return "✖";
            }
        }

        public static SlotMachineWinSlots WinType(this SlotMachineSlots slot, int count)
        {
            if (count < 3) return SlotMachineWinSlots.nothing;
            return (SlotMachineWinSlots) Enum.Parse(typeof(SlotMachineWinSlots), $"{slot}{count}");
        }

        public static int WinValue(this SlotMachineSlots slot, int count, bool psay = false)
        {
            if (count < 3) return 0;
            var win = ((SlotMachineWinSlots) Enum.Parse(typeof(SlotMachineWinSlots), $"{slot}{count}")).Value();
            return psay ? (win * 2) : win;
        }

        public static string ToIcon(this SlotMachineWinSlots wins)
        {
            if (wins == SlotMachineWinSlots.nothing)
                return "✖";

            string strWin = wins.ToString();
            char cnt = strWin.ToCharArray()[1];
            SlotMachineSlots icon  = (SlotMachineSlots) Enum.Parse(typeof(SlotMachineSlots), $"{strWin.ToCharArray()[0]}");

            return $"{cnt}x{icon.Icon()}";
        }

        public static int Value(this SlotMachineBeatMultiplier multi) => (int)multi;
        public static int Value(this SlotMachineWinSlots winSlot) => (int)winSlot;
        public static int Value(this SlotMachineSelectedRows slot) => (int)slot;
        public static int Value(this SlotMachineBeat beat) => (int)beat;
        public static int Value(this SlotMachineSlots sl) => (int)sl;

        public static SlotMachineSlots ToSMS(this int sl) => (SlotMachineSlots)sl;
    }
}
