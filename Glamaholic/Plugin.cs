using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Glamaholic {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        internal static string Name => "Glamaholic";

        [PluginService]
        internal static IPluginLog Log { get; private set; }

        [PluginService]
        internal IDalamudPluginInterface Interface { get; init; }

        [PluginService]
        internal static IChatGui ChatGui { get; private set; }

        [PluginService]
        internal IClientState ClientState { get; init; }

        [PluginService]
        internal ICommandManager CommandManager { get; init; }

        [PluginService]
        internal static IDataManager DataManager { get; private set; }

        [PluginService]
        internal IFramework Framework { get; init; }

        [PluginService]
        internal IGameGui GameGui { get; init; }

        [PluginService]
        internal ISigScanner SigScanner { get; init; }

        [PluginService]
        internal ITextureProvider TextureProvider { get; init; }

        [PluginService]
        internal IGameInteropProvider GameInteropProvider { get; init; }

        [PluginService]
        internal IAddonLifecycle AddonLifecycle { get; init; }

        internal Configuration Config { get; }
        internal GameFunctions Functions { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }

#pragma warning disable 8618
        public Plugin() {
            this.Config = Configuration.LoadAndMigrate(this.Interface!.ConfigFile);

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

        public void LogTroubleshooting(string message) {
            if (!Config.TroubleshootingMode)
                return;

            Log.Info($"[Troubleshooting] {message}");
        }

        internal void SaveConfig() {
            this.Interface.SavePluginConfig(this.Config);
        }
    }
}
