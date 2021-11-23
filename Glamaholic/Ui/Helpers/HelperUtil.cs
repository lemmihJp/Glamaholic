using System;
using System.Numerics;
using Dalamud.Interface;
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
                ? root->Width - DropdownWidth()
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
    }
}
