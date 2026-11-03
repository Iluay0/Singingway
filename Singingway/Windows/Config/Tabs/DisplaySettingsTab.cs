using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Singingway.Windows.Config.Tabs;
internal class DisplaySettingsTab : UiTab
{
    public override string Name => "Display Settings";

    private static readonly string[] HighlightModeNames = ["Sweep", "Word"];

    public override void Initialize()
    {

    }

    public override void Draw()
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

        ImGui.Separator();

        int _TextScalePercentage = Service.Configuration.TextScalePercentage;
        if (ImGui.SliderInt("Text Scale %", ref _TextScalePercentage, 50, 200))
        {
            Service.Configuration.TextScalePercentage = _TextScalePercentage;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _LineSpacing = Math.Clamp(Service.Configuration.LineSpacing, -10, 10);
        if (ImGui.SliderInt("Line spacing", ref _LineSpacing, -10, 10))
        {
            Service.Configuration.LineSpacing = _LineSpacing;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.Separator();

        Vector4 _TextColorVec = ImGui.ColorConvertU32ToFloat4(Service.Configuration.TextColor);
        if (ImGui.ColorEdit4("Text colour", ref _TextColorVec))
        {
            Service.Configuration.TextColor = ImGui.GetColorU32(_TextColorVec);
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        Vector4 _NextLineColorVec = ImGui.ColorConvertU32ToFloat4(Service.Configuration.NextLineColor);
        if (ImGui.ColorEdit4("Next line colour", ref _NextLineColorVec))
        {
            Service.Configuration.NextLineColor = ImGui.GetColorU32(_NextLineColorVec);
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        Vector4 _ShadowColorVec = ImGui.ColorConvertU32ToFloat4(Service.Configuration.ShadowColor);
        if (ImGui.ColorEdit4("Shadow colour", ref _ShadowColorVec))
        {
            Service.Configuration.ShadowColor = ImGui.GetColorU32(_ShadowColorVec);
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        Vector4 _HighlightColorVec = ImGui.ColorConvertU32ToFloat4(Service.Configuration.HighlightColor);
        if (ImGui.ColorEdit4("Highlight colour", ref _HighlightColorVec))
        {
            Service.Configuration.HighlightColor = ImGui.GetColorU32(_HighlightColorVec);
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        int _HighlightMode = (int)Service.Configuration.HighlightMode;
        if (ImGui.Combo("Highlight mode", ref _HighlightMode, HighlightModeNames, HighlightModeNames.Length))
        {
            Service.Configuration.HighlightMode = (HighlightMode)_HighlightMode;
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

        double _TimeRateSecondsPerHour = (Service.Configuration.TimeRateMultiplier - 1.0) * 3600.0;
        if (ImGui.InputDouble("Time rate (s/hr)", ref _TimeRateSecondsPerHour, 0.01, 0.1, "%.3f"))
        {
            Service.Configuration.TimeRateMultiplier = (_TimeRateSecondsPerHour / 3600.0) + 1.0;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }
    }
}
