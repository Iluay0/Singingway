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
}
