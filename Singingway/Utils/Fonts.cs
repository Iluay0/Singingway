using Dalamud.Interface.ManagedFontAtlas;
using System.Threading.Tasks;

namespace Singingway.Utils
{
    public static class Fonts
    {
        public static IFontHandle TitleFont { get; private set; }
        public static IFontHandle SubTitleFont { get; private set; }

        public static async Task Initialize()
        {
            TitleFont = Service.pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
            {
                tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new() { SizePx = 32 }));
            });

            SubTitleFont = Service.pluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(tk =>
            {
                tk.OnPreBuild(tk => tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansJpMedium, new() { SizePx = 24 }));
            });
        }
    }
}
