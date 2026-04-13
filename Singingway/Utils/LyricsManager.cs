using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace Singingway.Utils;
internal class TimedLine
{
    public double Time { get; set; }
    public string Text { get; set; } = string.Empty;
    public double LoopToTime { get; set; } = -1.0;

    public bool IsLooping => LoopToTime >= 0.0;
}

internal class LyricsFile
{
    public List<TimedLine> Lines { get; set; } = new();
}

internal class LyricsManager : IDisposable
{
    private static readonly Lazy<LyricsManager> _instance = new(() => new LyricsManager());
    public static LyricsManager Instance => _instance.Value;

    public event Action<bool>? PlayingChanged;

    private bool _isPlaying = false;
    public bool IsPlaying => _isPlaying;

    private DateTime _startTime;
    private List<TimedLine> _currentLines = [];
    private int _nextIndex = 0;
    private readonly Lock _lockObject = new();

    private string _currentDisplayText = string.Empty;
    private double _previousTimestamp = 0.0;

    // The FFXIV audio engine seems to utilize an internal overlap-add buffer for seamless transitions.
    // Testing across multiple tracks and loop points confirms this buffer processing pauses the
    // audio playhead relative to the system clock by exactly 150ms per transition/loop.
    private const double EngineTransitionOffset = 0.150;

    private LyricsManager() { }

    public void Start(string bgmFileName)
    {
        Stop();

        lock (_lockObject)
        {
            _nextIndex = 0;
            _startTime = DateTime.UtcNow;
            _previousTimestamp = 0.0;
        }

        bgmFileName = Path.GetFileName(bgmFileName);
        string lyricsFile = Path.ChangeExtension(bgmFileName, ".json");

        var baseDir = Service.Configuration?.LyricsDirectory;
        if (string.IsNullOrEmpty(baseDir)) baseDir = Path.Combine(AppContext.BaseDirectory, "Lyrics");

        var lyricsPath = Path.Combine(baseDir, lyricsFile);
        Plugin.DebugOut($"Attempting to load lyrics for: {bgmFileName}");

        string? json = null;
        if (Service.Configuration?.UseLyricsDirectory ?? false)
        {
            if (!Directory.Exists(baseDir))
            {
                Plugin.DebugOut($"Lyrics directory does not exist: {baseDir}");
            }
            else if (File.Exists(lyricsPath))
            {
                try
                {
                    json = File.ReadAllText(lyricsPath);
                    Plugin.DebugOut($"Loaded lyrics from disk: {lyricsPath}");
                }
                catch (Exception ex)
                {
                    Plugin.DebugOut($"Error reading from disk: {ex.Message}");
                }
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            try
            {
                string resourceName = $"Singingway.Resources.Lyrics.{lyricsFile}";
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            json = reader.ReadToEnd();
                        }
                        Plugin.DebugOut($"Loaded lyrics from embedded resource: {resourceName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.DebugOut($"Error reading from resources: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(json))
        {
            Plugin.DebugOut($"No lyrics found on disk or in resources for: {bgmFileName}");
            return;
        }

        try
        {
            json = PreprocessTimes(json);
            var lf = JsonSerializer.Deserialize<LyricsFile>(json);
            if (lf == null || lf.Lines.Count == 0)
            {
                Plugin.DebugOut("Lyrics file empty.");
                return;
            }

            lock (_lockObject)
            {
                _currentLines = lf.Lines.OrderBy(l => l.Time).ToList();
                _isPlaying = true;
            }

            Plugin.DebugOut($"Now playing lyrics for: {bgmFileName}");
            PlayingChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            Plugin.DebugOut($"Failed to parse lyrics JSON: {ex.Message}");
        }
    }

    public void Stop()
    {
        bool wasPlaying;
        lock (_lockObject)
        {
            wasPlaying = _isPlaying;
            _isPlaying = false;
            _currentLines.Clear();
            _nextIndex = 0;
        }
        Text.ClearCache();

        if (wasPlaying)
        {
            PlayingChanged?.Invoke(false);
        }
    }

    public double GetLineTime(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _currentLines.Count)
        {
            return 0.0;
        }

        double time = _currentLines[lineIndex].Time;
        if (_currentLines[lineIndex].IsLooping)
        {
            time -= EngineTransitionOffset;
        }
        else
        {
            time += Service.Configuration?.TimingOffsetSeconds ?? 0.0;
        }

        return time;
    }

    public void Update()
    {
        lock (_lockObject)
        {
            if (_nextIndex >= _currentLines.Count)
            {
                _isPlaying = false;
                return;
            }

            var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
            while (_nextIndex < _currentLines.Count && GetLineTime(_nextIndex) <= elapsed)
            {
                var currentLine = _currentLines[_nextIndex];
                if (currentLine.IsLooping)
                {
                    var loopDuration = GetLineTime(_nextIndex) - currentLine.LoopToTime;
                    _startTime = _startTime.AddSeconds(loopDuration);
                    Plugin.DebugOut($"Looping back to {currentLine.LoopToTime} seconds");

                    _nextIndex = _currentLines.FindIndex(l => l.Time > currentLine.LoopToTime);
                    if (_nextIndex == -1)
                    {
                        _nextIndex = _currentLines.Count;
                        Plugin.DebugOut("LoopToTime exceeds all timestamps, stopping.");
                    }
                    else if (_nextIndex > 0)
                    {
                        currentLine = _currentLines[_nextIndex - 1];
                        _previousTimestamp = currentLine.Time;
                        _currentDisplayText = currentLine.Text;
                    }
                    break;
                }

                _previousTimestamp = currentLine.Time;
                _currentDisplayText = currentLine.Text;
                _nextIndex++;
            }
        }
    }

    public string GetCurrentDisplayText()
    {
        lock (_lockObject)
            return _currentDisplayText;
    }
    public double GetElapsedSeconds() => _startTime == default ? 0.0 : (DateTime.UtcNow - _startTime).TotalSeconds;
    public double GetTotalDuration()
    {
        lock (_lockObject)
            return _currentLines.Count > 0 ? _currentLines.Last().Time : 0.0;
    }
    public double GetPreviousTimestamp()
    {
        lock (_lockObject)
            return _previousTimestamp;
    }
    public double GetNextTimestamp()
    {
        return GetLineTime(_nextIndex);
    }

    public bool IsNextLineLooped()
    {
        lock (_lockObject)
        {
            if (_nextIndex >= 0 && _nextIndex < _currentLines.Count)
            {
                return _currentLines[_nextIndex].IsLooping;
            }
            return false;
        }
    }

    public string GetNextDisplayText()
    {
        lock (_lockObject)
        {
            if (_nextIndex == _currentLines.Count - 1)
            {
                var currentLine = _currentLines[_nextIndex];
                if (currentLine.IsLooping)
                {
                    int target_nextIndex = _currentLines.FindIndex(l => l.Time >= currentLine.LoopToTime);
                    if (target_nextIndex != -1)
                    {
                        return _currentLines[target_nextIndex].Text;
                    }

                    return string.Empty;
                }
            }

            if (_nextIndex >= _currentLines.Count) return string.Empty;
            return _currentLines[_nextIndex].Text;
        }
    }

    private static string PreprocessTimes(string json)
    {
        var outJson = json;

        string[] keysToProcess = { "\"Time\"", "\"LoopToTime\"" };

        foreach (var key in keysToProcess)
        {
            int idx = 0;
            while (true)
            {
                var pos = outJson.IndexOf(key, idx, StringComparison.Ordinal);
                if (pos == -1) break;

                var colon = outJson.IndexOf(':', pos);
                if (colon == -1) break;

                var firstQuote = outJson.IndexOf('"', colon);
                if (firstQuote == -1) { idx = pos + 1; continue; }

                var secondQuote = outJson.IndexOf('"', firstQuote + 1);
                if (secondQuote == -1) { idx = pos + 1; continue; }

                var timeText = outJson.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                if (timeText.Contains(':'))
                {
                    if (TryParseMmSsMs(timeText, out var seconds))
                    {
                        outJson = outJson.Substring(0, firstQuote) +
                                  seconds.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                                  outJson.Substring(secondQuote + 1);
                        idx = firstQuote + 1;
                        continue;
                    }
                }
                idx = secondQuote + 1;
            }
        }

        return outJson;
    }

    private static bool TryParseMmSsMs(string s, out double seconds)
    {
        seconds = 0;
        // formats: mm:ss, m:ss.ms, ss.ms, mm:ss.ms
        var parts = s.Split(':');
        try
        {
            if (parts.Length == 1)
            {
                seconds = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            if (parts.Length == 2)
            {
                var mins = double.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
                var sec = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                seconds = mins * 60.0 + sec;
                return true;
            }
        }
        catch { }
        return false;
    }

    public void Dispose()
    {
        Stop();
    }
}
