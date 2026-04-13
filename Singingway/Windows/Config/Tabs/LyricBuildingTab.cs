using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Singingway.Windows.Config.Tabs;
internal class LyricBuildingTab : UiTab
{
    public override string Name => "Lyric Building";

    private string _builderInput = "";
    private string _builderOutput = "{\n  \"Lines\": [\n  ]\n}";
    private List<string> _recordedJsonLines = new();
    private string _lastRecordedLyric = "None";

    public override void Initialize()
    {

    }

    public override void Draw()
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

        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1.0f, 1.0f), $"Current Lyric: {_lastRecordedLyric}");
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), $"Next Lyric: {nextLyric}");
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
}
