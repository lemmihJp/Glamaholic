using System;

namespace Glamaholic {
    internal enum PlateSlot : uint {
        MainHand = 0,
        OffHand = 1,
        Head = 2,
        Body = 3,
        Hands = 4,
        Legs = 5,
        Feet = 6,
        Ears = 7,
        Neck = 8,
        Wrists = 9,
        RightRing = 10,
        LeftRing = 11,
    }

    internal static class PlateSlotExt {
        internal static string Name(this PlateSlot slot) {
            return slot switch {
                PlateSlot.MainHand => "Main Hand",
                PlateSlot.OffHand => "Off Hand",
                PlateSlot.Head => "Head",
                PlateSlot.Body => "Body",
                PlateSlot.Hands => "Hands",
                PlateSlot.Legs => "Legs",
                PlateSlot.Feet => "Feet",
                PlateSlot.Ears => "Ears",
                PlateSlot.Neck => "Neck",
                PlateSlot.Wrists => "Wrists",
                PlateSlot.RightRing => "Right Ring",
                PlateSlot.LeftRing => "Left Ring",
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null),
            };
        }
    }
}
