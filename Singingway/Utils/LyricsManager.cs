using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Singingway.Utils
{
    internal class TimedLine
    {
        public double Time { get; set; }
        public string Text { get; set; } = string.Empty;
        public double LoopToTime { get; set; } = -1.0;
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

        private DateTime startTime;
        private List<TimedLine> currentLines = new();
        private int nextIndex = 0;
        private readonly object _lockObject = new();

        private LyricsManager() { }

        public void Start(string bgmFileName)
        {
            Stop();

            bgmFileName = Path.GetFileName(bgmFileName);
            string lyricsFile = Path.ChangeExtension(bgmFileName, ".json");

            var baseDir = Service.configuration?.LyricsDirectory;
            if (string.IsNullOrEmpty(baseDir)) baseDir = Path.Combine(AppContext.BaseDirectory, "Lyrics");

            var lyricsPath = Path.Combine(baseDir, lyricsFile);
            Plugin.DebugOut($"Attempting to load lyrics for: {bgmFileName}");

            string? json = null;
            if (Service.configuration?.UseLyricsDirectory ?? false)
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

                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
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
                    currentLines = lf.Lines.OrderBy(l => l.Time).ToList();
                    nextIndex = 0;
                    startTime = DateTime.UtcNow;
                    previousTimestamp = 0.0;
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
                currentLines.Clear();
                nextIndex = 0;
            }
            Text.ClearCache();

            if (wasPlaying)
            {
                PlayingChanged?.Invoke(false);
            }
        }

        public double GetLineTime(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= currentLines.Count)
            {
                return 0.0;
            }

            double time = currentLines[lineIndex].Time;
            time += Service.configuration?.TimingOffsetSeconds ?? 0.0;

            if (currentLines[lineIndex].LoopToTime >= 0.0)
            {
                time += Service.configuration?.LoopTimingOffsetSeconds ?? 0.0;
            }

            return time;
        }

        public void Update()
        {
            lock (_lockObject)
            {
                if (nextIndex >= currentLines.Count)
                {
                    _isPlaying = false;
                    return;
                }

                var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                while (nextIndex < currentLines.Count && GetLineTime(nextIndex) <= elapsed)
                {
                    var currentLine = currentLines[nextIndex];
                    if (currentLine.LoopToTime >= 0.0)
                    {
                        var loopDuration = GetLineTime(nextIndex) - currentLine.LoopToTime;
                        startTime = startTime.AddSeconds(loopDuration);
                        Plugin.DebugOut($"Looping back to {currentLine.LoopToTime} seconds");

                        nextIndex = currentLines.FindIndex(l => l.Time > currentLine.LoopToTime);
                        if (nextIndex == -1)
                        {
                            nextIndex = currentLines.Count;
                            Plugin.DebugOut("LoopToTime exceeds all timestamps, stopping.");
                        }
                        else if (nextIndex > 0)
                        {
                            currentLine = currentLines[nextIndex - 1];
                            previousTimestamp = currentLine.Time;
                            currentDisplayText = currentLine.Text;
                        }
                        break;
                    }

                    previousTimestamp = currentLine.Time;
                    currentDisplayText = currentLine.Text;
                    nextIndex++;
                }
            }
        }

        private string currentDisplayText = string.Empty;
        private double previousTimestamp = 0.0;

        public string GetCurrentDisplayText()
        {
            lock (_lockObject)
                return currentDisplayText;
        }
        public double GetElapsedSeconds() => startTime == default ? 0.0 : (DateTime.UtcNow - startTime).TotalSeconds;
        public double GetTotalDuration()
        {
            lock (_lockObject)
                return currentLines.Count > 0 ? currentLines.Last().Time : 0.0;
        }
        public double GetPreviousTimestamp()
        {
            lock (_lockObject)
                return previousTimestamp;
        }
        public double GetNextTimestamp()
        {
            return GetLineTime(nextIndex);
        }

        public bool IsNextLineLooped()
        {
            lock (_lockObject)
            {
                if (nextIndex >= 0 && nextIndex < currentLines.Count)
                {
                    return currentLines[nextIndex].LoopToTime >= 0.0;
                }
                return false;
            }
        }

        public string GetNextDisplayText()
        {
            lock (_lockObject)
            {
                if (nextIndex == currentLines.Count - 1)
                {
                    var currentLine = currentLines[nextIndex];
                    if (currentLine.LoopToTime >= 0.0)
                    {
                        int targetNextIndex = currentLines.FindIndex(l => l.Time >= currentLine.LoopToTime);
                        if (targetNextIndex != -1)
                        {
                            return currentLines[targetNextIndex].Text;
                        }

                        return string.Empty;
                    }
                }

                if (nextIndex >= currentLines.Count) return string.Empty;
                return currentLines[nextIndex].Text;
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
}
