using Dalamud.IoC;
using Dalamud.Plugin.Services;

internal static class PluginHelpers {

    [PluginService]
    internal static IPluginLog Log { get; private set; }

    [PluginService]
    internal static IChatGui ChatGui { get; private set; }

    [PluginService]
    internal static IDataManager DataManager { get; private set; }
}