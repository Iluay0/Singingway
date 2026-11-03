using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Singingway.Utils;
internal class LyricsManager : IDisposable
{
    private static readonly Lazy<LyricsManager> _instance = new(() => new LyricsManager());
    public static LyricsManager Instance => _instance.Value;

    public event Action<bool>? PlayingChanged;

    private bool _isPlaying = false;
    public bool IsPlaying => _isPlaying;

    private List<TimedLine> _currentLines = [];
    private int _currentIndex = -1;
    private int _nextIndex = -1;
    private bool _nextWrapped = false;
    private double _lyricTime = 0.0;
    private double _songTime = 0.0;
    private double? _loopLineTime = null;
    private double _duration = 0.0;
    private double? _loopStart = null;
    private double _passStart = 0.0;
    private readonly Lock _lockObject = new();
    private const double InstrumentalGapSeconds = 3.0;
    private const string InstrumentalText = "(Instrumental)";
    private TimedLine? _instrumentalLine = null;


    private bool _isPreviewMode = false;
    private List<TimedLine>? _previewLinesCache = null;

    private LyricsManager() { }

    public void Start(uint bgmId, string bgmFileName)
    {
        try
        {
            Stop();

            Plugin.DebugOut($"Attempting to load lyrics for: {bgmId} ({bgmFileName})");

            var loaded = LyricsLibrary.Load(bgmId);
            if (loaded == null)
            {
                Plugin.DebugOut($"No lyrics found for: {bgmId}");
                return;
            }

            lock (_lockObject)
            {
                _currentLines = loaded.Lines;
                _loopLineTime = loaded.LoopLineTime;
                _isPlaying = true;
                Recompute();
            }

            Plugin.DebugOut($"Now playing lyrics for: {bgmId}");
            PlayingChanged?.Invoke(true);
        }
        finally
        {
            RestorePreviewIfNeeded();
        }
    }

    public void Stop()
    {
        bool wasPlaying;
        lock (_lockObject)
        {
            wasPlaying = _isPlaying;
            _isPlaying = false;

            _currentLines = new List<TimedLine>();
            _loopLineTime = null;
            ResetIndices();
        }
        Text.ClearCache();

        if (wasPlaying)
        {
            PlayingChanged?.Invoke(false);
        }
    }

    public void LoadPreviewLines(List<TimedLine> previewLines)
    {
        bool wasPlaying;
        lock (_lockObject)
        {
            var sortedLines = previewLines.OrderBy(l => l.Time).ToList();
            _currentLines = sortedLines;
            _loopLineTime = null;

            _previewLinesCache = new List<TimedLine>(sortedLines);
            _isPreviewMode = true;

            wasPlaying = _isPlaying;
            _isPlaying = true;

            Recompute();
        }

        if (!wasPlaying)
        {
            PlayingChanged?.Invoke(true);
        }
    }

    public void ClearPreview()
    {
        lock (_lockObject)
        {
            _previewLinesCache = null;
            _isPreviewMode = false;

            _currentLines = new List<TimedLine>();
            ResetIndices();
            _isPlaying = false;
        }

        PlayingChanged?.Invoke(false);
    }

    private void ResetIndices()
    {
        _currentIndex = -1;
        _nextIndex = -1;
        _nextWrapped = false;
    }

    private void Recompute()
    {
        var clock = SongClock.Instance;
        double duration = clock.DurationSeconds;
        double? loopStart = clock.LoopStartSeconds;
        if (loopStart.HasValue && _loopLineTime.HasValue)
        {
            duration += _loopLineTime.Value - loopStart.Value;
            loopStart = _loopLineTime.Value;
        }
        _duration = duration;
        _loopStart = loopStart;

        double t = clock.TotalElapsedSeconds * (Service.Configuration?.TimeRateMultiplier ?? 1.0);
        _passStart = 0.0;
        if (loopStart.HasValue && duration > loopStart.Value && t >= duration)
        {
            t = ((t - loopStart.Value) % (duration - loopStart.Value)) + loopStart.Value;
            _passStart = loopStart.Value;
        }
        _songTime = t;
        t -= Service.Configuration?.TimingOffsetSeconds ?? 0.0;
        _lyricTime = t;

        _currentIndex = _currentLines.FindLastIndex(l => l.Time <= t);
        _nextIndex = _currentIndex + 1;
        _nextWrapped = false;

        if (_nextIndex >= _currentLines.Count)
        {
            _nextIndex = -1;
            if (loopStart.HasValue && duration > loopStart.Value)
            {
                _nextIndex = _currentLines.FindIndex(l => l.Time >= loopStart.Value);
                _nextWrapped = _nextIndex != -1;
            }
        }
    }

    private void RestorePreviewIfNeeded()
    {
        bool invokeEvent = false;
        lock (_lockObject)
        {
            if (_isPreviewMode && _previewLinesCache != null)
            {
                _currentLines = new List<TimedLine>(_previewLinesCache);
                _loopLineTime = null;
    
                if (!_isPlaying)
                {
                    _isPlaying = true;
                    invokeEvent = true;
                }

                Recompute();
            }
        }

        if (invokeEvent)
        {
            PlayingChanged?.Invoke(true);
        }
    }

    public void Update()
    {
        lock (_lockObject)
        {
            Recompute();
        }
    }

    public string GetCurrentDisplayText()
    {
        lock (_lockObject)
            return CurrentDisplayLine()?.Text ?? string.Empty;
    }

    public double GetElapsedSeconds()
    {
        lock (_lockObject)
            return _lyricTime;
    }

    public double GetSongSeconds()
    {
        lock (_lockObject)
            return _songTime;
    }

    public double GetTotalDuration()
    {
        lock (_lockObject)
            return _duration;
    }

    public TimedLine? GetCurrentLine()
    {
        lock (_lockObject)
            return CurrentDisplayLine();
    }

    public double GetNextTimestamp()
    {
        lock (_lockObject)
            return NextTimestamp();
    }

    private double NextTimestamp()
    {
        if (_nextIndex < 0 || _nextIndex >= _currentLines.Count)
            return Math.Max(_duration, _lyricTime);

        double time = _currentLines[_nextIndex].Time;
        if (_nextWrapped && _loopStart.HasValue)
            time += _duration - _loopStart.Value;

        return time;
    }

    private TimedLine? CurrentDisplayLine()
    {
        if (_currentIndex >= _currentLines.Count)
            return null;

        TimedLine? line = _currentIndex >= 0 ? _currentLines[_currentIndex] : null;
        double end;
        if (line == null)
            end = _passStart;
        else if (line.End is double lineEnd && _lyricTime >= lineEnd)
            end = lineEnd;
        else
            return line;

        if (_nextIndex < 0 || _nextIndex >= _currentLines.Count || NextTimestamp() - end < InstrumentalGapSeconds)
            return line;

        if (_instrumentalLine == null || _instrumentalLine.Time != end)
            _instrumentalLine = new TimedLine { Time = end, Text = InstrumentalText };
        return _instrumentalLine;
    }

    public string GetNextDisplayText()
    {
        lock (_lockObject)
        {
            if (_nextIndex < 0 || _nextIndex >= _currentLines.Count) return string.Empty;
            return _currentLines[_nextIndex].Text;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
