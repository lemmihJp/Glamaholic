using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Glamaholic.Ui.Helpers {
    internal class ExamineHelper {
        private PluginUi Ui { get; }

        internal ExamineHelper(PluginUi ui) {
            this.Ui = ui;
        }

        internal unsafe void Draw() {
            if (!this.Ui.Plugin.Config.ShowExamineMenu) {
                return;
            }

            var examineAddon = (AtkUnitBase*) this.Ui.Plugin.GameGui.GetAddonByName("CharacterInspect", 1);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (examineAddon != null && examineAddon->IsVisible) {
                this.DrawInner(examineAddon);
            }
        }

        private unsafe void DrawInner(AtkUnitBase* addon) {
            var drawPos = HelperUtil.DrawPosForAddon(addon);
            if (drawPos == null) {
                return;
            }

            ImGui.SetNextWindowPos(drawPos.Value);
            if (!ImGui.Begin("##glamaholic-helper-examine", HelperUtil.HelperWindowFlags)) {
                ImGui.End();
                return;
            }

            ImGui.SetNextItemWidth(ImGui.CalcTextSize(this.Ui.Plugin.Name).X + ImGui.GetStyle().ItemInnerSpacing.X * 2 + 32 * ImGuiHelpers.GlobalScale);
            if (ImGui.BeginCombo("##glamaholic-helper-examine-combo", this.Ui.Plugin.Name)) {
                if (ImGui.Selectable("Create glamour plate")) {
                    void DoIt() {
                        var inventory = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
                        if (inventory == null) {
                            return;
                        }

                        var name = this.Ui.Plugin.Functions.ExamineName;
                        if (string.IsNullOrEmpty(name)) {
                            name = "Copied glamour";
                        }

                        var plate = new SavedPlate(name);
                        for (var i = 0; i < inventory->Size; i++) {
                            var item = inventory->Items[i];
                            var itemId = item.GlamourID;
                            if (itemId == 0) {
                                itemId = item.ItemID;
                            }

                            if (itemId == 0) {
                                continue;
                            }

                            var stainId = item.Stain;

                            // TODO: remove this logic in endwalker
                            var slot = i > 5 ? i - 1 : i;
                            plate.Items[(PlateSlot) slot] = new SavedGlamourItem {
                                ItemId = itemId,
                                StainId = stainId,
                            };
                        }

                        this.Ui.Plugin.Config.AddPlate(plate);
                        this.Ui.Plugin.SaveConfig();
                        this.Ui.OpenMainInterface();
                        this.Ui.SwitchPlate(this.Ui.Plugin.Config.Plates.Count - 1, true);
                    }

                    DoIt();
                }

                ImGui.EndCombo();
            }

            ImGui.End();
        }
    }
}
