using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Singingway.Windows.Config.Tabs;

internal class LyricBuildingTab : UiTab
{
    public override string Name => "Lyric Building";

    private class EditableLine
    {
        public string TimeStr = "";
        public string Text = "";
        public string LoopToTime = "";
        public bool IsLoopLine = false;
    }

    private string _builderInput = "";
    private string _builderOutput = "";
    private List<EditableLine> _lines = new();
    private string _lastRecordedLyric = "None";

    private bool _livePreviewEnabled = false;

    public override void Initialize()
    {
        ResetState();
    }

    private void ResetState()
    {
        _lines.Clear();
        _lastRecordedLyric = "None";

        _lines.Add(new EditableLine
        {
            TimeStr = "00:00.00",
            Text = "First lyric",
            LoopToTime = "00:00.00",
            IsLoopLine = false
        });
        _lines.Add(new EditableLine
        {
            TimeStr = "30:00.00",
            Text = "",
            LoopToTime = "00:00.00",
            IsLoopLine = true
        });

        UpdateOutputAndPreview();
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

        ImGui.TextWrapped("Paste full lyrics into the input box. Start playing the song, ideally using Orchestrion Plugin. Press \"Time Next Line\" to register the top line with the current song time.");
        ImGui.Separator();

        bool dataChanged = false;

        if (ImGui.Button("Time Next Line"))
        {
            RecordNextLyricLine(currentTimeStr);
            dataChanged = true;
        }

        ImGui.SameLine();

        if (ImGui.Button("Clear All"))
        {
            ResetState();
            dataChanged = true;
        }

        ImGui.SameLine();

        if (ImGui.Checkbox("Live Preview", ref _livePreviewEnabled))
        {
            dataChanged = true;
            if (!_livePreviewEnabled)
            {
                LyricsManager.Instance.ClearPreview();
            }
        }

        ImGui.Spacing();

        ImGui.Text("Lyric Editor:");
        if (ImGui.BeginTable("LyricsEditorTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Loop To", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 60f);
            ImGui.TableHeadersRow();

            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                dataChanged |= ImGui.InputText($"##time_{i}", ref line.TimeStr, 32);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                dataChanged |= ImGui.InputText($"##text_{i}", ref line.Text, 256);

                ImGui.TableNextColumn();
                if (line.IsLoopLine)
                {
                    ImGui.SetNextItemWidth(-1);
                    dataChanged |= ImGui.InputText($"##loop_{i}", ref line.LoopToTime, 32);
                }

                ImGui.TableNextColumn();
                if (ImGui.Checkbox($"Loop##type_{i}", ref line.IsLoopLine)) dataChanged = true;

                ImGui.TableNextColumn();
                if (ImGui.Button($"X##del_{i}"))
                {
                    _lines.RemoveAt(i);
                    dataChanged = true;
                    i--;
                }
            }
            ImGui.EndTable();
        }

        if (ImGui.Button("+ Add Custom Line"))
        {
            int insertIdx = _lines.Count > 0 && _lines.Last().IsLoopLine ? _lines.Count - 1 : _lines.Count;
            _lines.Insert(insertIdx, new EditableLine { TimeStr = "00:00.00", Text = "New Line" });
            dataChanged = true;
        }

        if (dataChanged)
        {
            UpdateOutputAndPreview();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("InputOutputTabs"))
        {
            if (ImGui.BeginTabItem("Raw Lyrics Input"))
            {
                ImGui.InputTextMultiline("##BuilderInput", ref _builderInput, 16384, new Vector2(-1, ImGui.GetTextLineHeight() * 10));
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("JSON Output"))
            {
                ImGui.InputTextMultiline("##BuilderOutput", ref _builderOutput, 65536, new Vector2(-1, ImGui.GetTextLineHeight() * 15), ImGuiInputTextFlags.ReadOnly);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void RecordNextLyricLine(string currentTimeStr)
    {
        if (string.IsNullOrEmpty(_builderInput)) return;

        var lines = _builderInput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0) return;

        int linesConsumed = 0;
        string recordedText = "";

        foreach (var line in lines)
        {
            linesConsumed++;
            string trimmedLine = line.Trim();

            if (!string.IsNullOrEmpty(trimmedLine))
            {
                recordedText = trimmedLine;
                break;
            }
        }

        if (!string.IsNullOrEmpty(recordedText))
        {
            int insertIdx = _lines.Count > 0 && _lines.Last().IsLoopLine ? _lines.Count - 1 : _lines.Count;
            _lines.Insert(insertIdx, new EditableLine
            {
                TimeStr = currentTimeStr,
                Text = recordedText
            });

            _lastRecordedLyric = recordedText;
        }

        if (linesConsumed < lines.Length)
        {
            _builderInput = string.Join("\n", lines, linesConsumed, lines.Length - linesConsumed);
        }
        else
        {
            _builderInput = "";
        }
    }

    private void UpdateOutputAndPreview()
    {
        UpdateBuilderOutput();
        UpdateLivePreview();
    }

    private void UpdateBuilderOutput()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"Lines\": [");

        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            string safeText = line.Text.Replace("\"", "\\\"");

            sb.Append($"    {{ \"Time\": \"{line.TimeStr}\", \"Text\": \"{safeText}\"");

            if (line.IsLoopLine)
            {
                sb.Append($", \"LoopToTime\": \"{line.LoopToTime}\"");
            }

            sb.Append(" }");

            if (i < _lines.Count - 1)
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

    private void UpdateLivePreview()
    {
        if (!_livePreviewEnabled) return;

        var timedLines = new List<TimedLine>();
        foreach (var line in _lines)
        {
            LyricsManager.TryParseMmSsMs(line.TimeStr, out double timeSeconds);

            double loopSeconds = -1.0;
            if (line.IsLoopLine)
            {
                LyricsManager.TryParseMmSsMs(line.LoopToTime, out loopSeconds);
            }

            timedLines.Add(new TimedLine
            {
                Time = timeSeconds,
                Text = line.Text,
                LoopToTime = loopSeconds
            });
        }

        LyricsManager.Instance.LoadPreviewLines(timedLines);
    }
}