using FFXIVClientStructs.FFXIV.Client.Sound;
using Lumina.Data.Files;
using Lumina.Data.Parsing.Scd;
using System;

namespace Singingway.Utils;
internal unsafe class SongClock
{
    private static readonly Lazy<SongClock> _instance = new(() => new SongClock());
    public static SongClock Instance => _instance.Value;

    private const double WrapThresholdSeconds = 7000.0;

    private nint _entryAddress;
    private string _fileName = string.Empty;
    private uint _soundNumber;
    private float _lastRaw;
    private double _wrapOffset;

    public bool HasSong { get; private set; }
    public string FileName => _fileName;
    public double TotalElapsedSeconds { get; private set; }
    public double DurationSeconds { get; private set; }
    public double? LoopStartSeconds { get; private set; }

    private SongClock() { }

    public bool IsTracking(string fileName)
    {
        return HasSong && string.Equals(_fileName, fileName, StringComparison.OrdinalIgnoreCase);
    }

    public void Update(string? preferredFile)
    {
        SoundData* entry = FindEntry(preferredFile);
        if (entry == null)
        {
            Reset();
            return;
        }

        string fileName = entry->GetFileName().ToString();
        uint soundNumber = entry->GetSoundNumber();
        float raw = entry->GetElapsedTime();

        bool sameSound = HasSong && string.Equals(fileName, _fileName, StringComparison.OrdinalIgnoreCase) && soundNumber == _soundNumber;
        if (!sameSound || (nint)entry != _entryAddress)
        {
            if (!sameSound)
            {
                LoadScd(fileName, soundNumber);
            }

            _entryAddress = (nint)entry;
            _fileName = fileName;
            _soundNumber = soundNumber;
            _wrapOffset = 0.0;
            HasSong = true;
            Plugin.DebugOut($"Tracking {fileName} #{soundNumber}, duration {DurationSeconds:0.000}s, loop start {(LoopStartSeconds.HasValue ? LoopStartSeconds.Value.ToString("0.000") : "none")}");
        }
        else if (raw < _lastRaw)
        {
            if (_lastRaw > WrapThresholdSeconds)
            {
                _wrapOffset += _lastRaw - raw + Service.Framework.UpdateDelta.TotalSeconds;
            }
            else
            {
                _wrapOffset = 0.0;
            }
        }

        _lastRaw = raw;
        TotalElapsedSeconds = raw + _wrapOffset;
    }

    private static SoundData* FindEntry(string? preferredFile)
    {
        var manager = SoundManager.Instance();
        if (manager == null || manager->SoundDataPool == null)
            return null;

        SoundData* preferred = null;
        SoundData* fallback = null;
        float fallbackElapsed = float.MaxValue;

        var entries = manager->SoundDataPool->Entries;
        for (int i = 0; i < entries.Length; i++)
        {
            SoundData* entry = (SoundData*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref entries[i]);
            if (!entry->IsActive || !entry->IsPlaying())
                continue;

            var namePtr = entry->GetFileName();
            if (!namePtr.HasValue)
                continue;

            string name = namePtr.ToString();
            if (!name.StartsWith("music/", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(preferredFile) && string.Equals(name, preferredFile, StringComparison.OrdinalIgnoreCase))
            {
                preferred = entry;
                break;
            }

            float elapsed = entry->GetElapsedTime();
            if (elapsed < fallbackElapsed)
            {
                fallbackElapsed = elapsed;
                fallback = entry;
            }
        }

        return preferred != null ? preferred : fallback;
    }

    private void LoadScd(string fileName, uint soundNumber)
    {
        DurationSeconds = 0.0;
        LoopStartSeconds = null;

        try
        {
            var scd = Service.DataManager.GameData.GetFile<ScdFile>(fileName);
            if (scd == null)
            {
                Plugin.DebugOut($"SCD not found: {fileName}");
                return;
            }

            var sound = scd.GetSound((int)soundNumber);
            var track = scd.GetTrack((int)soundNumber);

            var intervals = new System.Collections.Generic.List<uint>();
            foreach (var (cmd, data) in track)
            {
                if (cmd == TrackCmd.Interval)
                    intervals.Add(Convert.ToUInt32(data));
            }

            ulong sum = 0;
            foreach (var interval in intervals)
                sum += interval;

            ulong lengthMs = sound.SoundExtraDesc.HasValue && sound.SoundExtraDesc.Value.PlayTimeLength > 0
                ? sound.SoundExtraDesc.Value.PlayTimeLength
                : sum;
            DurationSeconds = lengthMs / 1000.0;

            if ((sound.SoundBasicDesc.Attribute & SoundAttribute.Loop) != 0)
            {
                ulong loopStartMs = 0;
                if (intervals.Count > 1)
                    loopStartMs = sum - intervals[^1];
                LoopStartSeconds = loopStartMs / 1000.0;
            }
        }
        catch (Exception ex)
        {
            Plugin.DebugOut($"Failed to read SCD {fileName}: {ex.Message}");
        }
    }

    private void Reset()
    {
        if (!HasSong)
            return;

        HasSong = false;
        _entryAddress = 0;
        _fileName = string.Empty;
        _soundNumber = 0;
        _lastRaw = 0f;
        _wrapOffset = 0.0;
        TotalElapsedSeconds = 0.0;
        DurationSeconds = 0.0;
        LoopStartSeconds = null;
    }
}
