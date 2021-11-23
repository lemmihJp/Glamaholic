using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace Glamaholic {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        public string Name => "Glamaholic";

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; }

        [PluginService]
        internal ChatGui ChatGui { get; init; }

        [PluginService]
        internal CommandManager CommandManager { get; init; }

        [PluginService]
        internal DataManager DataManager { get; init; }

        [PluginService]
        internal GameGui GameGui { get; init; }

        [PluginService]
        internal SigScanner SigScanner { get; init; }

        internal Configuration Config { get; }
        internal GameFunctions Functions { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

        #pragma warning disable 8618
        public Plugin() {
            this.Config = this.Interface!.GetPluginConfig() as Configuration ?? new Configuration();

            this.Functions = new GameFunctions(this);
            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
        }
        #pragma warning restore 8618

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Functions.Dispose();
        }

        internal void SaveConfig() {
            this.Interface.SavePluginConfig(this.Config);
        }
    }
}
