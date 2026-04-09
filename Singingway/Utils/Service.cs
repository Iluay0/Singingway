using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Singingway.Windows;

namespace Singingway.Utils;

internal class Service
{
    internal static Plugin plugin { get; set; } = null!;

    internal static ConfigWindow configWindow { get; set; } = null!;
    internal static Configuration configuration { get; set; } = null!;

    internal static Windows.LyricsWindow lyricsWindow { get; set; } = null!;

    [PluginService] public static IDalamudPluginInterface pluginInterface { get; set; } = null!;
    [PluginService] public static IChatGui chatGui { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ICommandManager commandManager { get; private set; } = null!;

    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
}
