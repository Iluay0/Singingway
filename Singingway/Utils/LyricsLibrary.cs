using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Singingway.Utils;
internal readonly record struct WordSegment(double Time, int StartChar, int EndChar);

internal class TimedLine
{
    [JsonRequired, JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    [JsonRequired, JsonPropertyName("time"), JsonConverter(typeof(LyricTimeConverter))]
    public double Time { get; set; }
    [JsonPropertyName("end"), JsonConverter(typeof(NullableLyricTimeConverter))]
    public double? End { get; set; }
    [JsonPropertyName("words"), JsonConverter(typeof(LyricTimeListConverter))]
    public List<double>? Words { get; set; }
    [JsonIgnore]
    public List<WordSegment> Segments { get; set; } = [];
    private string? _displayText;
    [JsonIgnore]
    public string DisplayText
    {
        get => _displayText ?? Text;
        set => _displayText = value;
    }
}

internal class LyricsFile
{
    [JsonRequired, JsonPropertyName("ids")]
    public List<uint> Ids { get; set; } = [];
    [JsonPropertyName("offsets")]
    public Dictionary<string, double>? Offsets { get; set; }
    [JsonPropertyName("loopLine")]
    public int? LoopLine { get; set; }
    [JsonRequired, JsonPropertyName("lines")]
    public List<TimedLine> Lines { get; set; } = [];
}

internal class LoadedLyrics
{
    public List<TimedLine> Lines { get; init; } = [];
    public double? LoopLineTime { get; init; }
}

internal class LyricTimeConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDouble();

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString() ?? string.Empty;
            if (TryParse(s, out var seconds))
                return seconds;
            throw new JsonException($"Invalid time \"{s}\"");
        }

        throw new JsonException($"Invalid time token {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    public static bool TryParse(string s, out double seconds)
    {
        seconds = 0;
        s = s.Trim();
        int colon = s.IndexOf(':');
        if (colon < 0)
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);

        var minPart = s[..colon];
        var secPart = s[(colon + 1)..];
        if (minPart.Length == 0 || !minPart.All(char.IsAsciiDigit))
            return false;

        int dot = secPart.IndexOf('.');
        var whole = dot < 0 ? secPart : secPart[..dot];
        var frac = dot < 0 ? string.Empty : secPart[(dot + 1)..];
        if (whole.Length is < 1 or > 2 || !whole.All(char.IsAsciiDigit))
            return false;
        if (dot >= 0 && (frac.Length is < 1 or > 3 || !frac.All(char.IsAsciiDigit)))
            return false;

        int mins = int.Parse(minPart, CultureInfo.InvariantCulture);
        int secs = int.Parse(whole, CultureInfo.InvariantCulture);
        if (secs >= 60)
            return false;

        double fraction = frac.Length == 0 ? 0.0 : int.Parse(frac, CultureInfo.InvariantCulture) / Math.Pow(10, frac.Length);
        seconds = mins * 60.0 + secs + fraction;
        return true;
    }
}

internal class NullableLyricTimeConverter : JsonConverter<double?>
{
    private static readonly LyricTimeConverter Inner = new();

    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return Inner.Read(ref reader, typeof(double), options);
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}

internal class LyricTimeListConverter : JsonConverter<List<double>?>
{
    private static readonly LyricTimeConverter Inner = new();

    public override List<double>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Invalid words token {reader.TokenType}");

        var list = new List<double>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            list.Add(Inner.Read(ref reader, typeof(double), options));
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<double>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        foreach (var v in value)
            writer.WriteNumberValue(v);
        writer.WriteEndArray();
    }
}

internal static class LyricsLibrary
{
    private const string ResourcePrefix = "Singingway.Resources.Lyrics.";

    private sealed record Source(string Name, bool Embedded, string Location);

    private sealed class IdsOnly
    {
        [JsonPropertyName("ids")]
        public List<uint>? Ids { get; set; }
    }

    private static Dictionary<uint, Source> _index = new();
    private static (bool, string)? _indexedSettings;

    public static int Count => _index.Count;

    public static void RebuildIfSettingsChanged()
    {
        if (_indexedSettings != CurrentSettings())
            Rebuild();
    }

    public static void Rebuild()
    {
        var settings = CurrentSettings();
        var index = new Dictionary<uint, Source>();

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            var json = ReadSource(new Source(name, true, name));
            if (json != null)
                AddToIndex(index, new Source(name, true, name), json);
        }

        if (settings.Item1)
        {
            var dir = ResolveDirectory(settings.Item2);
            if (!Directory.Exists(dir))
            {
                Plugin.DebugOut($"Lyrics directory does not exist: {dir}");
            }
            else
            {
                foreach (var path in Directory.EnumerateFiles(dir, "*.json"))
                {
                    var source = new Source(Path.GetFileName(path), false, path);
                    var json = ReadSource(source);
                    if (json != null)
                        AddToIndex(index, source, json);
                }
            }
        }

        _index = index;
        _indexedSettings = settings;
        Plugin.DebugOut($"Lyrics index built: {index.Count} ids");
    }

    public static LoadedLyrics? Load(uint bgmId)
    {
        if (!_index.TryGetValue(bgmId, out var source))
            return null;

        var json = ReadSource(source);
        if (json == null)
            return null;

        Plugin.DebugOut($"Loading lyrics for {bgmId} from {(source.Embedded ? "embedded" : "disk")}: {source.Name}");
        return Parse(json, bgmId, source.Name);
    }

    public static LoadedLyrics? Parse(string json, uint bgmId, string sourceName)
    {
        LyricsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<LyricsFile>(json);
        }
        catch (Exception ex)
        {
            Plugin.DebugOut($"Failed to parse {sourceName}: {ex.Message}");
            return null;
        }

        if (file == null || file.Lines.Count == 0)
        {
            Plugin.DebugOut($"Lyrics file empty: {sourceName}");
            return null;
        }

        double offset = 0.0;
        if (file.Offsets != null && file.Offsets.TryGetValue(bgmId.ToString(CultureInfo.InvariantCulture), out var o))
            offset = o;

        for (int i = 0; i < file.Lines.Count; i++)
        {
            var line = file.Lines[i];
            line.Time += offset;
            if (line.End.HasValue)
                line.End += offset;
            line.Segments = BuildSegments(line, i, sourceName, offset);
            line.Words = line.Segments.Count > 0 ? line.Words : null;
        }

        double? loopLineTime = null;
        if (file.LoopLine.HasValue)
        {
            if (file.LoopLine.Value >= 0 && file.LoopLine.Value < file.Lines.Count)
                loopLineTime = file.Lines[file.LoopLine.Value].Time;
            else
                Plugin.DebugOut($"{sourceName}: LoopLine {file.LoopLine.Value} out of range, ignored");
        }

        int dropped = file.Lines.RemoveAll(l => l.Time < 0);
        if (dropped > 0)
            Plugin.DebugOut($"{sourceName}: dropped {dropped} lines with negative time for {bgmId}");

        return new LoadedLyrics
        {
            Lines = file.Lines.OrderBy(l => l.Time).ToList(),
            LoopLineTime = loopLineTime,
        };
    }

    public static List<WordSegment> BuildSegments(TimedLine line, int lineIndex, string sourceName, double offset)
    {
        var segments = new List<WordSegment>();
        if (line.Words == null || line.Words.Count == 0)
            return segments;

        var plain = string.Concat(Text.Parse(line.Text).Select(f => f.Text));
        var displayText = Text.Unescape(line.Text);
        if (!string.Equals(plain, displayText, StringComparison.Ordinal))
        {
            Plugin.DebugOut($"{sourceName}: line {lineIndex} has markup, words dropped");
            return segments;
        }
        line.DisplayText = displayText;

        var tokens = line.Text.Split(' ');
        if (tokens.Length != line.Words.Count)
        {
            Plugin.DebugOut($"{sourceName}: line {lineIndex} has {line.Words.Count} word times for {tokens.Length} words, words dropped");
            return segments;
        }

        var joined = new StringBuilder();
        for (int w = 0; w < tokens.Length; w++)
        {
            if (w > 0)
                joined.Append(' ');
            int start = joined.Length;
            joined.Append(Text.Unescape(tokens[w]));
            segments.Add(new WordSegment(line.Words[w] + offset, start, joined.Length));
        }

        if (!string.Equals(joined.ToString(), displayText, StringComparison.Ordinal))
        {
            Plugin.DebugOut($"{sourceName}: line {lineIndex} words \"{joined}\" do not match text \"{displayText}\", words dropped");
            segments.Clear();
        }

        return segments;
    }

    private static void AddToIndex(Dictionary<uint, Source> index, Source source, string json)
    {
        List<uint>? ids;
        try
        {
            ids = JsonSerializer.Deserialize<IdsOnly>(json)?.Ids;
        }
        catch (Exception ex)
        {
            Plugin.DebugOut($"Failed to index {source.Name}: {ex.Message}");
            return;
        }

        if (ids == null || ids.Count == 0)
        {
            Plugin.DebugOut($"No Ids in {source.Name}, skipped");
            return;
        }

        foreach (var id in ids)
            index[id] = source;
    }

    private static string? ReadSource(Source source)
    {
        try
        {
            if (!source.Embedded)
                return File.ReadAllText(source.Location);

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(source.Location);
            if (stream == null)
                return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            Plugin.DebugOut($"Error reading {source.Name}: {ex.Message}");
            return null;
        }
    }

    private static (bool, string) CurrentSettings()
    {
        return (Service.Configuration?.UseLyricsDirectory ?? false, Service.Configuration?.LyricsDirectory ?? string.Empty);
    }

    private static string ResolveDirectory(string configured)
    {
        return string.IsNullOrEmpty(configured) ? Path.Combine(AppContext.BaseDirectory, "Lyrics") : configured;
    }
}
