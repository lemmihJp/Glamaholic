using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Glamaholic {
    // size may be incorrect
    [StructLayout(LayoutKind.Explicit, Size = 0x3B38)]
    internal struct AgentMiragePrismMiragePlateData {
        [FieldOffset(0x0)]
        private uint _IsModified; // may actually be a bitset, but seems to be used as a 4-byte bool.. :(

        [FieldOffset(0x14)]
        private uint _SelectedMiragePlateIndex;

        // The index of the item selected in the current Mirage Plate
        [FieldOffset(0x18)]
        private uint _SelectedItemIndex;

        // The index of the item the context menu is associated with
        [FieldOffset(0x1C)]
        private uint _ContextMenuItemIndex;

        // If anyone feels like figuring out what the hell is in here..
        // Please, be my guest.

        [FieldOffset(0x3864)]
        private FixedSizeArray12<MiragePlateItem> _Items;

        public bool IsModified {
            get => _IsModified != 0;
            set => _IsModified = value ? 1u : 0u;
        }

        public uint SelectedMiragePlateIndex {
            get => _SelectedMiragePlateIndex;
            set => _SelectedMiragePlateIndex = Math.Clamp(value, 0, 19);
        }

        public uint SelectedItemIndex {
            get => _SelectedItemIndex;
            set => _SelectedItemIndex = Math.Clamp(value, 0, (uint) PlateSlot.LeftRing);
        }

        public uint ContextMenuItemIndex {
            get => _ContextMenuItemIndex;
            set => _ContextMenuItemIndex = Math.Clamp(value, 0, (uint) PlateSlot.LeftRing);
        }

        public unsafe Span<MiragePlateItem> Items =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<FixedSizeArray12<MiragePlateItem>, MiragePlateItem>(ref _Items), 12);
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x3C)]
    internal struct MiragePlateItem {
        [FieldOffset(0x0)]
        public uint ItemId;

        [FieldOffset(0x4)]
        public uint SlotOrCabinetId;

        [FieldOffset(0x8)]
        public MirageSource Source;

        [FieldOffset(0x18)]
        public byte Stain1;

        [FieldOffset(0x19)]
        public byte Stain2;

        [FieldOffset(0x1A)]
        public byte PreviewStain1;

        [FieldOffset(0x1B)]
        public byte PreviewStain2;

        [FieldOffset(0x1C)]
        public bool HasChanged;

        // After this seem to be 3 ints, not sure what they are yet
    }

    internal enum MirageSource : uint {
        GlamourDresser = 1,
        Armoire = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [InlineArray(12)]
    internal struct FixedSizeArray12<T> where T : unmanaged {
        private T _element0;
    }
}
