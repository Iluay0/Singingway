using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Singingway.Utils;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace Singingway.Windows;

internal class ConfigWindow : Window
{
    public event Action? OnConfigChanged;

        public ConfigWindow(Plugin plugin) : base(
            "Singingway Configuration Window")
    {
        Size = new Vector2(400, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("ConfigTabBar"))
        {
            if (ImGui.BeginTabItem("Display Settings"))
            {
                DrawDisplaySettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Development Settings"))
            {
                DrawDevelopmentSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebugTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawDisplaySettings()
    {
        int _MinWindowWidth = Service.Configuration.MinWindowWidth;
        if (ImGui.InputInt("Min window width", ref _MinWindowWidth))
        {
            _MinWindowWidth = Math.Clamp(_MinWindowWidth, 0, 3840);
            Service.Configuration.MinWindowWidth = _MinWindowWidth;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _MaxWindowWidth = Service.Configuration.MaxWindowWidth;
        if (ImGui.InputInt("Max window width", ref _MaxWindowWidth))
        {
            _MaxWindowWidth = Math.Clamp(_MaxWindowWidth, 0, 3840);
            Service.Configuration.MaxWindowWidth = _MaxWindowWidth;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        Vector4 _BackgroundColorVec = ImGui.ColorConvertU32ToFloat4(Service.Configuration.BackgroundColor);
        if (ImGui.ColorEdit4("Background Color", ref _BackgroundColorVec))
        {
            Service.Configuration.BackgroundColor = ImGui.GetColorU32(_BackgroundColorVec);
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _BackgroundOpacityPercentage = Service.Configuration.BackgroundOpacityPercentage;
        if (ImGui.SliderInt("Background Opacity %", ref _BackgroundOpacityPercentage, 0, 100))
        {
            Service.Configuration.BackgroundOpacityPercentage = _BackgroundOpacityPercentage;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        bool _BackgroundFullWindow = Service.Configuration.BackgroundFullWindow;
        if (ImGui.Checkbox("Background behind full window", ref _BackgroundFullWindow))
        {
            Service.Configuration.BackgroundFullWindow = _BackgroundFullWindow;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        bool _ShowTotalTime = Service.Configuration.ShowTotalTime;
        if (ImGui.Checkbox("Show total time", ref _ShowTotalTime))
        {
            Service.Configuration.ShowTotalTime = _ShowTotalTime;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        bool _ShowProgressBar = Service.Configuration.ShowProgressBar;
        if (ImGui.Checkbox("Show progress bar", ref _ShowProgressBar))
        {
            Service.Configuration.ShowProgressBar = _ShowProgressBar;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        int _TextScalePercentage = Service.Configuration.TextScalePercentage;
        if (ImGui.SliderInt("Text Scale %", ref _TextScalePercentage, 50, 200))
        {
            Service.Configuration.TextScalePercentage = _TextScalePercentage;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        int _TimingOffsetMs = (int)(Service.Configuration.TimingOffsetSeconds * 1000);
        if (ImGui.InputInt("Timing Offset (ms)", ref _TimingOffsetMs))
        {
            _TimingOffsetMs = Math.Clamp(_TimingOffsetMs, -1000, 1000);
            Service.Configuration.TimingOffsetSeconds = _TimingOffsetMs / 1000.0;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }
    }

    private void DrawDevelopmentSettings()
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
                    InvokeConfigChanged();
                }
            }
            ImGui.PopItemWidth();
        }
    }

    private void DrawDebugTab()
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

    public void InvokeConfigChanged()
    {
        OnConfigChanged?.Invoke();
    }
}
