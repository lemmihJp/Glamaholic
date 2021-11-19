using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Glamaholic.Ui.Helpers {
    internal class EditorHelper {
        private PluginUi Ui { get; }

        internal EditorHelper(PluginUi ui) {
            this.Ui = ui;
        }

        internal unsafe void Draw() {
            if (!this.Ui.Plugin.Config.ShowEditorMenu || !Util.IsEditingPlate(this.Ui.Plugin.GameGui)) {
                return;
            }

            var addon = (AtkUnitBase*) this.Ui.Plugin.GameGui.GetAddonByName(Util.PlateAddon, 1);
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (addon != null && addon->IsVisible) {
                this.DrawInner(addon);
            }
        }

        private unsafe void DrawInner(AtkUnitBase* addon) {
            var drawPos = HelperUtil.DrawPosForAddon(addon);
            if (drawPos == null) {
                return;
            }

            ImGui.SetNextWindowPos(drawPos.Value);
            if (!ImGui.Begin("##glamaholic-helper-open", HelperUtil.HelperWindowFlags)) {
                ImGui.End();
                return;
            }

            ImGui.SetNextItemWidth(ImGui.CalcTextSize(this.Ui.Plugin.Name).X + ImGui.GetStyle().ItemInnerSpacing.X * 2 + 32 * ImGuiHelpers.GlobalScale);
            if (ImGui.BeginCombo("##glamaholic-helper-examine-combo", this.Ui.Plugin.Name)) {
                if (ImGui.Selectable($"Open {this.Ui.Plugin.Name}")) {
                    this.Ui.OpenMainInterface();
                }

                ImGui.EndCombo();
            }

            ImGui.End();
        }
    }
}
