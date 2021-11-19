using System.Numerics;
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
                                                            | ImGuiWindowFlags.AlwaysAutoResize;

        internal static unsafe Vector2? DrawPosForAddon(AtkUnitBase* addon) {
            if (addon == null) {
                return null;
            }
            
            var root = addon->RootNode;
            if (root == null) {
                return null;
            }

            return new Vector2(addon->X, addon->Y)
                   - new Vector2(0, ImGui.CalcTextSize("A").Y)
                   - new Vector2(0, ImGui.GetStyle().ItemInnerSpacing.Y * 2)
                   - new Vector2(0, ImGui.GetStyle().CellPadding.Y * 2);
        }
    }
}
