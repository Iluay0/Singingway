using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Singingway;

internal enum HighlightMode
{
    Sweep,
    Word,
}

[Serializable]
internal class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    private int _minWindowWidth = 200;
    public int MinWindowWidth
    {
        get => _minWindowWidth;
        set => _minWindowWidth = Math.Min(value, MaxWindowWidth);
    }

    private int _maxWindowWidth = 800;
    public int MaxWindowWidth
    {
        get => _maxWindowWidth;
        set => _maxWindowWidth = Math.Max(value, MinWindowWidth);
    }

    public uint BackgroundColor { get; set; } = 0xFF000000;
    public int BackgroundOpacityPercentage { get; set; } = 0;
    public bool BackgroundFullWindow { get; set; } = false;

    public int TextScalePercentage { get; set; } = 100;
    public int LineSpacing { get; set; } = 4;

    public uint TextColor { get; set; } = 0xFFFFFFFF;
    public uint NextLineColor { get; set; } = 0xFFAAAAAA;
    public uint ShadowColor { get; set; } = 0xFF000000;
    public uint HighlightColor { get; set; } = 0xFF40D0FF;
    public HighlightMode HighlightMode { get; set; } = HighlightMode.Sweep;

    public bool EnableVolumeOverrides { get; set; } = false;
    public uint VolumeSoundMaster { get; set; } = 100;
    public uint VolumeSoundBgm { get; set; } = 100;
    public uint VolumeSoundSe { get; set; } = 100;
    public uint VolumeSoundVoice { get; set; } = 100;
    public uint VolumeSoundEnv { get; set; } = 100;
    public uint VolumeSoundSystem { get; set; } = 100;
    public uint VolumeSoundPerform { get; set; } = 100;

    public bool ShowTotalTime { get; set; } = false;


    public bool UseLyricsDirectory { get; set; } = false;
    public string LyricsDirectory { get; set; } = "";

    public double TimingOffsetSeconds { get; set; } = 0.0;
    public double TimeRateMultiplier { get; set; } = 1.0;

    [NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this._pluginInterface = pluginInterface;
    }

    public void Save()
    {
        _pluginInterface!.SavePluginConfig(this);
    }
}
