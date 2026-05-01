using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Singingway.Utils;
using Singingway.Windows.Config.Tabs;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Singingway.Windows.Config;
internal class ConfigWindow : Window
{
    public event Action? OnConfigChanged;
    private readonly List<UiTab> _tabs;

    public ConfigWindow(Plugin plugin) : base(
        "Singingway Configuration Window")
    {
        Size = new Vector2(400, 400);
        SizeCondition = ImGuiCond.FirstUseEver;

        _tabs = [
                new DisplaySettingsTab(),
                new VolumeSettingsTab(),
                new DevelopmentSettingsTab(),
                new DebugTab(),
                new LyricBuildingTab()
            ];

        foreach (var tab in _tabs)
        {
            tab.OnConfigChanged = () => this.OnConfigChanged?.Invoke();
            tab.Initialize();
        }
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabBar"))
        {
            foreach (var tab in _tabs)
            {
                if (ImGui.BeginTabItem(tab.Name))
                {
                    tab.Draw();
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }

    public void InvokeConfigChanged()
    {
        OnConfigChanged?.Invoke();
    }
}