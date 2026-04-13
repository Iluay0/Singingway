using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Singingway.Windows.Config.Tabs;
internal class DevelopmentSettingsTab : UiTab
{
    public override string Name => "Development Settings";

    public override void Initialize()
    {

    }

    public override void Draw()
    {
        bool _UseLyricsDirectory = Service.Configuration.UseLyricsDirectory;
        if (ImGui.Checkbox("Prioritize directory on disk", ref _UseLyricsDirectory))
        {
            Service.Configuration.UseLyricsDirectory = _UseLyricsDirectory;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        if (_UseLyricsDirectory)
        {
            ImGui.Text("Lyrics directory:");
            ImGui.PushItemWidth(-1);
            var current = Service.Configuration?.LyricsDirectory ?? string.Empty;
            if (ImGui.InputText("##lyricsdir", ref current, 260))
            {
                if (!string.Equals(current, Service.Configuration?.LyricsDirectory, StringComparison.Ordinal))
                {
                    Service.Configuration?.LyricsDirectory = current;
                    Service.Configuration?.Save();
                    OnConfigChanged?.Invoke();
                }
            }
            ImGui.PopItemWidth();
        }
    }
}
