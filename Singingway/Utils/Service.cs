using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Singingway.Windows.Config;
using Singingway.Windows.Lyrics;

namespace Singingway.Utils;
internal class Service
{
    internal static Plugin Plugin { get; set; } = null!;

    internal static ConfigWindow ConfigWindow { get; set; } = null!;
    internal static Configuration Configuration { get; set; } = null!;

    internal static LyricsWindow LyricsWindow { get; set; } = null!;

    [PluginService] public static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;

    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
}
