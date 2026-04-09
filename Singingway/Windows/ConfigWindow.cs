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
        int _MinWindowWidth = Service.configuration.MinWindowWidth;
        if (ImGui.InputInt("Min window width", ref _MinWindowWidth))
        {
            Service.configuration.MinWindowWidth = _MinWindowWidth;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _MaxWindowWidth = Service.configuration.MaxWindowWidth;
        if (ImGui.InputInt("Max window width", ref _MaxWindowWidth))
        {
            Service.configuration.MaxWindowWidth = _MaxWindowWidth;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        Vector4 _BackgroundColorVec = ImGui.ColorConvertU32ToFloat4(Service.configuration.BackgroundColor);
        if (ImGui.ColorEdit4("Background Color", ref _BackgroundColorVec))
        {
            Service.configuration.BackgroundColor = ImGui.GetColorU32(_BackgroundColorVec);
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _BackgroundOpacityPercentage = Service.configuration.BackgroundOpacityPercentage;
        if (ImGui.SliderInt("Background Opacity %", ref _BackgroundOpacityPercentage, 0, 100))
        {
            Service.configuration.BackgroundOpacityPercentage = _BackgroundOpacityPercentage;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        bool _BackgroundFullWindow = Service.configuration.BackgroundFullWindow;
        if (ImGui.Checkbox("Background behind full window", ref _BackgroundFullWindow))
        {
            Service.configuration.BackgroundFullWindow = _BackgroundFullWindow;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        bool _ShowTotalTime = Service.configuration.ShowTotalTime;
        if (ImGui.Checkbox("Show total time", ref _ShowTotalTime))
        {
            Service.configuration.ShowTotalTime = _ShowTotalTime;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        bool _ShowProgressBar = Service.configuration.ShowProgressBar;
        if (ImGui.Checkbox("Show progress bar", ref _ShowProgressBar))
        {
            Service.configuration.ShowProgressBar = _ShowProgressBar;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        int _TextScalePercentage = Service.configuration.TextScalePercentage;
        if (ImGui.SliderInt("Text Scale %", ref _TextScalePercentage, 50, 200))
        {
            Service.configuration.TextScalePercentage = _TextScalePercentage;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        int _TimingOffsetMs = (int)(Service.configuration.TimingOffsetSeconds * 1000);
        if (ImGui.InputInt("Timing Offset (ms)", ref _TimingOffsetMs))
        {
            Service.configuration.TimingOffsetSeconds = _TimingOffsetMs / 1000.0;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _LoopTimingOffsetMs = (int)(Service.configuration.LoopTimingOffsetSeconds * 1000);
        if (ImGui.InputInt("Loop Timing Offset (ms)", ref _LoopTimingOffsetMs))
        {
            Service.configuration.LoopTimingOffsetSeconds = _LoopTimingOffsetMs / 1000.0;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }
    }

    private void DrawDevelopmentSettings()
    {
        bool _UseLyricsDirectory = Service.configuration.UseLyricsDirectory;
        if (ImGui.Checkbox("Prioritize directory on disk", ref _UseLyricsDirectory))
        {
            Service.configuration.UseLyricsDirectory = _UseLyricsDirectory;
            Service.configuration.Save();
            OnConfigChanged?.Invoke();
        }

        if (_UseLyricsDirectory)
        {
            ImGui.Text("Lyrics directory:");
            ImGui.PushItemWidth(-1);
            var current = Service.configuration?.LyricsDirectory ?? string.Empty;
            if (ImGui.InputText("##lyricsdir", ref current, 260))
            {
                if (!string.Equals(current, Service.configuration?.LyricsDirectory, StringComparison.Ordinal))
                {
                    Service.configuration.LyricsDirectory = current;
                    Service.configuration.Save();
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
