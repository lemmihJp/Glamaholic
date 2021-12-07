using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

namespace Glamaholic.Ui.Helpers {
    internal static class HelperUtil {
        internal const ImGuiWindowFlags HelperWindowFlags = ImGuiWindowFlags.NoBackground
                                                            | ImGuiWindowFlags.NoDecoration
                                                            | ImGuiWindowFlags.NoCollapse
                                                            | ImGuiWindowFlags.NoTitleBar
                                                            | ImGuiWindowFlags.NoNav
                                                            | ImGuiWindowFlags.NoNavFocus
                                                            | ImGuiWindowFlags.NoNavInputs
                                                            | ImGuiWindowFlags.NoResize
                                                            | ImGuiWindowFlags.NoScrollbar
                                                            | ImGuiWindowFlags.NoSavedSettings
                                                            | ImGuiWindowFlags.NoFocusOnAppearing
                                                            | ImGuiWindowFlags.AlwaysAutoResize
                                                            | ImGuiWindowFlags.NoDocking;

        internal static unsafe Vector2? DrawPosForAddon(AtkUnitBase* addon, bool right = false) {
            if (addon == null) {
                return null;
            }

            var root = addon->RootNode;
            if (root == null) {
                return null;
            }

            var xModifier = right
                ? root->Width * addon->Scale - DropdownWidth()
                : 0;

            return ImGuiHelpers.MainViewport.Pos
                   + new Vector2(addon->X, addon->Y)
                   + Vector2.UnitX * xModifier
                   - Vector2.UnitY * ImGui.CalcTextSize("A")
                   - Vector2.UnitY * (ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().FrameBorderSize);
        }

        internal static float DropdownWidth() {
            // arrow size is GetFrameHeight
            return (ImGui.CalcTextSize(Plugin.PluginName).X + ImGui.GetStyle().ItemInnerSpacing.X * 2 + ImGui.GetFrameHeight()) * ImGuiHelpers.GlobalScale;
        }

        internal class HelperStyles : IDisposable {
            internal HelperStyles() {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, Vector2.Zero);
            }

            public void Dispose() {
                ImGui.PopStyleVar(3);
            }
        }

        internal static unsafe void DrawHelper(AtkUnitBase* addon, string id, bool right, Action dropdown) {
            var drawPos = DrawPosForAddon(addon, right);
            if (drawPos == null) {
                return;
            }

            using (new HelperStyles()) {
                // get first frame
                ImGui.SetNextWindowPos(drawPos.Value, ImGuiCond.Appearing);
                if (!ImGui.Begin($"##{id}", HelperWindowFlags)) {
                    ImGui.End();
                    return;
                }
            }

            ImGui.SetNextItemWidth(DropdownWidth());
            if (ImGui.BeginCombo($"##{id}-combo", Plugin.PluginName)) {
                try {
                    dropdown();
                } catch (Exception ex) {
                    PluginLog.LogError(ex, "Error drawing helper combo");
                }

                ImGui.EndCombo();
            }

            ImGui.SetWindowPos(drawPos.Value);

            ImGui.End();
        }

        internal static void DrawCreatePlateMenu(PluginUi ui, Func<SavedPlate?> getter) {
            if (!ImGui.BeginMenu("Create glamour plate")) {
                return;
            }

            if (ImGui.Selectable("New")) {
                var plate = getter();
                if (plate != null) {
                    CopyToGlamourPlate(ui, plate, -1);
                }
            }

            ImGui.Separator();

            if (ImGui.BeginChild("helper-overwrite", new Vector2(250, 350))) {
                for (var i = 0; i < ui.Plugin.Config.Plates.Count; i++) {
                    var plate = ui.Plugin.Config.Plates[i];
                    var ctrl = ImGui.GetIO().KeyCtrl;
                    if (ImGui.Selectable($"{plate.Name}##{i}") && ctrl) {
                        var newPlate = getter();
                        if (newPlate != null) {
                            CopyToGlamourPlate(ui, newPlate, i);
                        }
                    }

                    if (!ctrl && ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.TextUnformatted("Hold Control and click to overwrite.");
                        ImGui.EndTooltip();
                    }
                }

                ImGui.EndChild();
            }
                
            ImGui.EndMenu();
        }

        private static void CopyToGlamourPlate(PluginUi ui, SavedPlate plate, int idx) {
            if (idx == -1) {
                ui.Plugin.Config.AddPlate(plate);
            } else {
                Configuration.SanitisePlate(plate);
                plate.Name = ui.Plugin.Config.Plates[idx].Name;
                ui.Plugin.Config.Plates[idx] = plate;
            }
            ui.Plugin.SaveConfig();
            ui.OpenMainInterface();
            ui.SwitchPlate(idx == -1 ? ui.Plugin.Config.Plates.Count - 1 : idx, true);
        }
    }
}
