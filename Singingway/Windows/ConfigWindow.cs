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

    private string _builderInput = "";
    private string _builderOutput = "{\n  \"Lines\": [\n  ]\n}";
    private List<string> _recordedJsonLines = new();
    private string _lastRecordedLyric = "None";

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

            if (ImGui.BeginTabItem("Build Lyrics"))
            {
                DrawBuildLyricsTab();
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

    private void DrawBuildLyricsTab()
    {
        double currentSeconds = LyricsManager.Instance.GetElapsedSeconds();
        TimeSpan currentTime = TimeSpan.FromSeconds(currentSeconds);
        string currentTimeStr = $"{currentTime.Minutes:D2}:{currentTime.Seconds:D2}.{currentTime.Milliseconds / 10:D2}";

        ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), $"Current Song Time: {currentTimeStr}");
        ImGui.Separator();

        string nextLyric = "None";
        if (!string.IsNullOrEmpty(_builderInput))
        {
            var lines = _builderInput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    nextLyric = line.Trim();
                    break;
                }
            }
        }

        ImGui.TextColored(new Vector4(0.53f, 0.81f, 0.98f, 1.0f), $"Current Lyric: {_lastRecordedLyric}"); // Sky Blue
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), $"Next Lyric: {nextLyric}"); // Grey
        ImGui.Separator();

        ImGui.TextWrapped("Paste full lyrics into the input box. Start playing the song, ideally using Orchestrion Plugin or a similar plugin. Press \"Time Next Line\" to register the top line with the current song time.");
        ImGui.Separator();

        if (ImGui.Button("Time Next Line"))
        {
            RecordNextLyricLine();
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear All"))
        {
            _builderInput = "";
            _recordedJsonLines.Clear();
            _lastRecordedLyric = "None";
            UpdateBuilderOutput();
        }

        ImGui.Spacing();

        ImGui.Text("Input (Raw Lyrics):");
        ImGui.InputTextMultiline("##BuilderInput", ref _builderInput, 16384, new Vector2(-1, ImGui.GetTextLineHeight() * 10));

        ImGui.Spacing();

        ImGui.Text("Output (JSON Format):");
        ImGui.InputTextMultiline("##BuilderOutput", ref _builderOutput, 16384, new Vector2(-1, ImGui.GetTextLineHeight() * 15));
    }

    private void RecordNextLyricLine()
    {
        if (string.IsNullOrEmpty(_builderInput)) return;

        var lines = _builderInput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0) return;

        int linesConsumed = 0;
        double elapsedSeconds = LyricsManager.Instance.GetElapsedSeconds();
        TimeSpan time = TimeSpan.FromSeconds(elapsedSeconds);

        string timeStr = $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

        foreach (var line in lines)
        {
            linesConsumed++;
            string trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                // It's an empty line for formatting
                _recordedJsonLines.Add("");
                continue;
            }
            else
            {
                // We hit a valid lyric line. Record it, escape any existing quotes, and stop consuming
                string safeText = trimmedLine.Replace("\"", "\\\"");
                _recordedJsonLines.Add($"    {{ \"Time\": \"{timeStr}\", \"Text\": \"{safeText}\" }}");
                _lastRecordedLyric = trimmedLine;
                break;
            }
        }

        if (linesConsumed < lines.Length)
        {
            _builderInput = string.Join("\n", lines, linesConsumed, lines.Length - linesConsumed);
        }
        else
        {
            _builderInput = "";
        }

        UpdateBuilderOutput();
    }

    private void UpdateBuilderOutput()
    {
        if (_recordedJsonLines.Count == 0)
        {
            _builderOutput = "{\n  \"Lines\": [\n  ]\n}";
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"Lines\": [");

        for (int i = 0; i < _recordedJsonLines.Count; i++)
        {
            string line = _recordedJsonLines[i];

            if (string.IsNullOrEmpty(line))
            {
                sb.AppendLine();
                continue;
            }

            sb.Append(line);

            // Check if there is another valid line after this one to see if we need a comma
            bool hasNextValidLine = false;
            for (int j = i + 1; j < _recordedJsonLines.Count; j++)
            {
                if (!string.IsNullOrEmpty(_recordedJsonLines[j]))
                {
                    hasNextValidLine = true;
                    break;
                }
            }

            if (hasNextValidLine)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("  ]");
        sb.Append("}");

        _builderOutput = sb.ToString();
    }

    public void InvokeConfigChanged()
    {
        OnConfigChanged?.Invoke();
    }
}