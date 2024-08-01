using Dalamud.Plugin;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using System;

namespace Glamaholic.Interop {
    internal class Glamourer {
        private static SetItem _SetItem { get; set; } = null!;
        private static RevertState _RevertState { get; set; } = null!;

        private static bool Initialized { get; set; } = false;
        private static bool Available { get; set; } = false;

        public static void TryOn(SavedPlate plate) {
            if (!IsAvailable())
                return;

            try {
                var player = Service.ClientState.LocalPlayer;
                if (player == null)
                    return;

                ushort playerIndex = player.ObjectIndex;

                _RevertState.Invoke(playerIndex, flags: ApplyFlag.Equipment);

                foreach (var slot in Enum.GetValues<PlateSlot>()) {
                    if (!plate.Items.TryGetValue(slot, out var item)) {
                        if (!plate.FillWithNewEmperor)
                            continue;

                        uint empItem = Util.GetEmperorItemForSlot(slot);
                        if (empItem != 0)
                            _SetItem.Invoke(playerIndex, ConvertSlot(slot), empItem, [0, 0]);

                        continue;
                    }

                    Service.Log.Info($"{slot} -> {ConvertSlot(slot)}");

                    _SetItem.Invoke(playerIndex, ConvertSlot(slot), item.ItemId, [item.Stain1, item.Stain2]);
                }
            } catch (Exception) { }
        }

        private static ApiEquipSlot ConvertSlot(PlateSlot slot) {
            switch (slot) {
                case PlateSlot.LeftRing:
                    return ApiEquipSlot.LFinger;

                case >= (PlateSlot) 5:
                    return (ApiEquipSlot) ((int) slot + 2);

                default:
                    return (ApiEquipSlot) ((int) slot + 1);
            }
        }

        public static void Initialize(IDalamudPluginInterface pluginInterface) {
            if (Initialized)
                return;

            _SetItem = new SetItem(pluginInterface);
            _RevertState = new RevertState(pluginInterface);

            Initialized = true;

            CheckIfAvailable(pluginInterface);
        }

        public static void CheckIfAvailable(IDalamudPluginInterface pluginInterface) {
            Available = false;

            foreach (var plugin in pluginInterface.InstalledPlugins) {
                if (plugin.Name == "Glamourer") {
                    Available = plugin.IsLoaded;
                    break;
                }
            }
        }

        public static bool IsAvailable() {
            return Available && IsIPCValid();
        }

        public static bool IsIPCValid() {
            return _SetItem.Valid && _RevertState.Valid;
        }
    }
}
