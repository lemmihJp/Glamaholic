using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Glamaholic.Ui.Helpers {
    internal class TryOnHelper {
        private PluginUi Ui { get; }

        internal TryOnHelper(PluginUi ui) {
            this.Ui = ui;
        }

        internal unsafe void Draw() {
            if (!this.Ui.Plugin.Config.ShowTryOnMenu) {
                return;
            }

            var tryOnAddon = (AtkUnitBase*) this.Ui.Plugin.GameGui.GetAddonByName("Tryon", 1);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (tryOnAddon == null || !tryOnAddon->IsVisible) {
                return;
            }

            var right = this.Ui.Plugin.Interface.PluginInternalNames.Contains("ItemSearchPlugin");
            HelperUtil.DrawHelper(tryOnAddon, "glamaholic-helper-try-on", right, this.DrawDropdown);
        }

        private void DrawDropdown() {
            if (ImGui.Selectable("Create glamour plate")) {
                this.Ui.Plugin.Config.AddPlate(new SavedPlate("Fitting Room") {
                    Items = GetTryOnItems(),
                });
                this.Ui.Plugin.SaveConfig();

                this.Ui.OpenMainInterface();
                this.Ui.SwitchPlate(this.Ui.Plugin.Config.Plates.Count - 1, true);
            }
        }

        private static unsafe Dictionary<PlateSlot, SavedGlamourItem> GetTryOnItems() {
            var agent = (IntPtr) Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Tryon);
            var firstItem = agent + 0x2E8;

            var items = new Dictionary<PlateSlot, SavedGlamourItem>();
            
            for (var i = 0; i < 12; i++) {
                var item = (TryOnItem*) (firstItem + i * 24);
                if (item->Slot == 14 || item->ItemId == 0) {
                    continue;
                }

                var itemId = item->ItemId;
                if (item->GlamourId != 0) {
                    itemId = item->GlamourId;
                }
                
                // TODO: remove this logic in endwalker
                var slot = item->Slot > 5 ? item->Slot - 1 : item->Slot;
                items[(PlateSlot) slot] =new SavedGlamourItem {
                    ItemId = itemId % Util.HqItemOffset,
                    StainId = item->StainId,
                };
            }

            return items;
        }

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private readonly struct TryOnItem {
            [FieldOffset(0)]
            internal readonly byte Slot;

            [FieldOffset(2)]
            internal readonly byte StainId;

            [FieldOffset(5)]
            internal readonly byte UnknownByte;

            [FieldOffset(8)]
            internal readonly uint ItemId;

            [FieldOffset(12)]
            internal readonly uint GlamourId;
        }
    }
}
