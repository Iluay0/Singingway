using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Singingway.Windows.Config.Tabs;

internal class LyricBuildingTab : UiTab
{
    public override string Name => "Lyric Building";

    private class EditableWord
    {
        public string TimeStr = "";
    }

    private class EditableLine
    {
        public string TimeStr = "";
        public string Text = "";
        public string EndStr = "";
        public List<EditableWord> Words = new();
    }

    private class OutputLine
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
        [JsonPropertyName("time")]
        public string Time { get; set; } = "";
        [JsonPropertyName("end")]
        public string? End { get; set; }
        [JsonPropertyName("words")]
        public List<string>? Words { get; set; }
    }

    private class OutputFile
    {
        [JsonPropertyName("ids")]
        public List<uint> Ids { get; set; } = new();
        [JsonPropertyName("loopLine")]
        public int? LoopLine { get; set; }
        [JsonPropertyName("lines")]
        public List<OutputLine> Lines { get; set; } = new();
    }

    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private string _builderInput = "";
    private string _builderOutput = "";
    private List<EditableLine> _lines = new();
    private string _lastRecordedLyric = "None";
    private int _expandedLine = -1;
    private int _loopLine = -1;
    private ushort _outputBgmId;

    private bool _livePreviewEnabled = false;

    public override void Initialize()
    {
        ResetState();
    }

    private void ResetState()
    {
        _lines.Clear();
        _lastRecordedLyric = "None";
        _expandedLine = -1;
        _loopLine = -1;

        _lines.Add(new EditableLine
        {
            TimeStr = "00:00.000",
            Text = "First lyric"
        });
        _lines.Add(new EditableLine
        {
            TimeStr = "30:00.000",
            Text = ""
        });

        foreach (var line in _lines)
            SyncWords(line);

        UpdateOutputAndPreview();
    }

    public override void Draw()
    {
        string currentTimeStr = FormatTime(LyricsManager.Instance.GetSongSeconds());
        ushort bgmId = BgmHelper.GetActiveBgmId();

        ImGui.TextColored(new Vector4(0.5f, 1.0f, 0.5f, 1.0f), $"Current Song Time: {currentTimeStr}");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f), $"BGM {bgmId}");
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

        ImGui.TextWrapped("Paste full lyrics into the input box. Start playing the song, ideally using Orchestrion Plugin. Press \"Time Next Line\" to register the top line with the current song time. Press + on a line to time its words.");
        ImGui.Separator();

        bool dataChanged = bgmId != _outputBgmId;

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

        ImGui.SetNextItemWidth(120f);
        if (ImGui.InputInt("Loop line (-1 = none)", ref _loopLine))
        {
            if (_loopLine < -1)
                _loopLine = -1;
            dataChanged = true;
        }

        ImGui.Spacing();

        ImGui.Text("Lyric Editor:");
        if (ImGui.BeginTable("LyricsEditorTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
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
                if (ImGui.InputText($"##text_{i}", ref line.Text, 256))
                {
                    SyncWords(line);
                    dataChanged = true;
                }

                ImGui.TableNextColumn();
                if (ImGui.Button($"{(_expandedLine == i ? "-" : "+")}##words_{i}"))
                {
                    _expandedLine = _expandedLine == i ? -1 : i;
                }
                ImGui.SameLine();
                if (ImGui.Button($"X##del_{i}"))
                {
                    _lines.RemoveAt(i);
                    if (_expandedLine == i)
                        _expandedLine = -1;
                    else if (_expandedLine > i)
                        _expandedLine--;
                    dataChanged = true;
                    i--;
                    continue;
                }

                if (_expandedLine == i)
                {
                    dataChanged |= DrawWordEditor(i, line, currentTimeStr);
                }
            }
            ImGui.EndTable();
        }

        if (ImGui.Button("+ Add Custom Line"))
        {
            var line = new EditableLine { TimeStr = "00:00.000", Text = "New Line" };
            SyncWords(line);
            _lines.Add(line);
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
                ImGui.InputTextMultiline("##BuilderOutput", ref _builderOutput, 262144, new Vector2(-1, ImGui.GetTextLineHeight() * 15), ImGuiInputTextFlags.ReadOnly);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private bool DrawWordEditor(int lineIndex, EditableLine line, string currentTimeStr)
    {
        bool changed = false;
        var texts = SplitWords(line.Text);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TableNextColumn();

        if (ImGui.Button($"Time Next Word##tnw_{lineIndex}"))
        {
            var word = line.Words.Find(w => !TryParseTime(w.TimeStr, out _));
            if (word != null)
            {
                word.TimeStr = currentTimeStr;
                changed = true;
            }
        }
        ImGui.SameLine();
        if (ImGui.Button($"Clear Words##cw_{lineIndex}"))
        {
            foreach (var word in line.Words)
                word.TimeStr = "";
            changed = true;
        }
        ImGui.SameLine();
        ImGui.Text("End");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(80f);
        changed |= ImGui.InputText($"##end_{lineIndex}", ref line.EndStr, 32);
        ImGui.SameLine();
        if (ImGui.Button($"Stamp End##se_{lineIndex}"))
        {
            line.EndStr = currentTimeStr;
            changed = true;
        }

        if (texts.Length > 0 && ImGui.BeginTable($"WordsTable_{lineIndex}", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableSetupColumn("Word", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            for (int w = 0; w < texts.Length && w < line.Words.Count; w++)
            {
                var word = line.Words[w];
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                changed |= ImGui.InputText($"##wt_{lineIndex}_{w}", ref word.TimeStr, 32);

                ImGui.TableNextColumn();
                ImGui.TextUnformatted(texts[w]);
            }
            ImGui.EndTable();
        }

        ImGui.TableNextColumn();
        return changed;
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
            var newLine = new EditableLine
            {
                TimeStr = currentTimeStr,
                Text = recordedText
            };
            SyncWords(newLine);
            _lines.Add(newLine);

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

    private static string[] SplitWords(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static void SyncWords(EditableLine line)
    {
        int count = SplitWords(line.Text).Length;
        if (line.Words.Count > count)
            line.Words.RemoveRange(count, line.Words.Count - count);
        while (line.Words.Count < count)
            line.Words.Add(new EditableWord());
    }

    private static bool TryParseTime(string s, out double seconds)
    {
        seconds = 0;
        return !string.IsNullOrWhiteSpace(s) && LyricTimeConverter.TryParse(s, out seconds);
    }

    private static string FormatTime(double seconds)
    {
        long ms = (long)Math.Round(Math.Max(0.0, seconds) * 1000.0);
        return string.Create(CultureInfo.InvariantCulture, $"{ms / 60000:D2}:{ms / 1000 % 60:D2}.{ms % 1000:D3}");
    }

    private static string NormalizeTime(string s)
    {
        return TryParseTime(s, out var seconds) ? FormatTime(seconds) : s.Trim();
    }

    private static List<double>? CompleteWords(EditableLine line)
    {
        var texts = SplitWords(line.Text);
        if (texts.Length == 0 || line.Words.Count != texts.Length)
            return null;

        var words = new List<double>(texts.Length);
        for (int w = 0; w < texts.Length; w++)
        {
            if (!TryParseTime(line.Words[w].TimeStr, out var time))
                return null;
            words.Add(time);
        }
        return words;
    }

    private void UpdateOutputAndPreview()
    {
        UpdateBuilderOutput();
        UpdateLivePreview();
    }

    private void UpdateBuilderOutput()
    {
        _outputBgmId = BgmHelper.GetActiveBgmId();
        var file = new OutputFile
        {
            Ids = [_outputBgmId],
            LoopLine = _loopLine >= 0 ? _loopLine : null,
        };

        foreach (var line in _lines)
        {
            var words = CompleteWords(line);
            file.Lines.Add(new OutputLine
            {
                Text = line.Text,
                Time = NormalizeTime(line.TimeStr),
                End = TryParseTime(line.EndStr, out var end) ? FormatTime(end) : null,
                Words = words?.ConvertAll(FormatTime),
            });
        }

        _builderOutput = JsonSerializer.Serialize(file, OutputOptions);
    }

    private void UpdateLivePreview()
    {
        if (!_livePreviewEnabled) return;

        var timedLines = new List<TimedLine>();
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            TryParseTime(line.TimeStr, out double timeSeconds);
            var words = CompleteWords(line);

            var timed = new TimedLine
            {
                Time = timeSeconds,
                Text = line.Text,
                End = TryParseTime(line.EndStr, out var end) ? end : null,
            };

            if (words != null)
            {
                timed.Words = words;
                timed.Segments = LyricsLibrary.BuildSegments(timed, i, Name, 0.0);
                if (timed.Segments.Count == 0)
                    timed.Words = null;
            }

            timedLines.Add(timed);
        }

        LyricsManager.Instance.LoadPreviewLines(timedLines);
    }
}
