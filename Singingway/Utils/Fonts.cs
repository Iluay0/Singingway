using Dalamud.Interface.ManagedFontAtlas;
using System.Threading.Tasks;

namespace Singingway.Utils;
public static class Fonts
{
    public static IFontHandle? TitleFont { get; private set; } = null;
    public static IFontHandle? SubTitleFont { get; private set; } = null;

    public static async Task Initialize()
    {
        TitleFont = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansCjkMedium, new() { SizePx = 32 }));
        });

        SubTitleFont = Service.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
        {
            tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansCjkMedium, new() { SizePx = 24 }));
        });
    }
}
