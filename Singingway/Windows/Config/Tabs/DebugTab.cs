using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Singingway.Windows.Config.Tabs;
internal class DebugTab : UiTab
{
    public override string Name => "Debug";

    public override void Initialize()
    {

    }

    public override void Draw()
    {
        ImGui.Text("Debug Messages:");
        ImGui.Separator();

        List<string> debugMessages;
        lock (Plugin.DebugMessages)
        {
            debugMessages = new List<string>(Plugin.DebugMessages);
        }

        if (ImGui.BeginChild("DebugMessages", new Vector2(-1, -ImGui.GetFrameHeightWithSpacing() - 10)))
        {
            foreach (var message in debugMessages)
            {
                ImGui.TextWrapped(message);
            }

            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }
        }
        ImGui.EndChild();

        if (ImGui.Button("Clear Debug Messages"))
        {
            lock (Plugin.DebugMessages)
            {
                Plugin.DebugMessages.Clear();
            }
        }
    }
}
