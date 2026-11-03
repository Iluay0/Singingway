using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Singingway.Utils;
using Singingway.Windows.Config;
using Singingway.Windows.Lyrics;
using System;
using System.Collections.Generic;

namespace Singingway;
internal sealed class Plugin : IDalamudPlugin
{
    public static string Name => "Singingway";
    private const string LyricsCommand = "/singingway";

    public static List<string> DebugMessages { get; } = [];
    private const int MaxDebugMessages = 1000;

    public WindowSystem WindowSystem { get; } = new("Singingway");

    private ushort _currentBgmId = 0;
    private string _currentBgmFile = string.Empty;
    private bool _pendingLyricsStart = false;
    private readonly Dictionary<string, uint> _savedVolumeDefaults = new();
    private bool _volumeOverridesApplied = false;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Service.PluginInterface = pluginInterface;

        Service.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Service.Configuration.Initialize(pluginInterface);

        _ = pluginInterface.Create<Service>();
        Service.Plugin = this;

        Service.ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(Service.ConfigWindow);
        Service.ConfigWindow.OnConfigChanged += () =>
        {
            LyricsLibrary.RebuildIfSettingsChanged();

            if (Utils.LyricsManager.Instance.IsPlaying)
            {
                if (Service.Configuration.EnableVolumeOverrides)
                {
                    ApplyLyricVolumeOverrides();
                }
                else
                {
                    RestoreDefaultVolumes();
                }
            }
        };

        Service.LyricsWindow = new LyricsWindow();
        WindowSystem.AddWindow(Service.LyricsWindow);

        Utils.LyricsManager.Instance.PlayingChanged += playing =>
        {
            Service.LyricsWindow.IsOpen = playing;
            if (playing)
            {
                ApplyLyricVolumeOverrides();
            }
            else
            {
                RestoreDefaultVolumes();
                PlayCurrentBgm();
            }
        };

        pluginInterface.UiBuilder.Draw += DrawUI;
        pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

        LyricsLibrary.Rebuild();

        Service.CommandManager.AddHandler(LyricsCommand, new CommandInfo(OnLyricsCommand)
        {
            HelpMessage = "Singingway: /singingway start <bgmid> | /singingway stop | /singingway config"
        });

        Service.Framework.Update += OnFrameworkUpdate;

        Fonts.Initialize().ConfigureAwait(false);

        DebugOut("Plugin Loaded!");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var playingBgmId = BgmHelper.GetActiveBgmId();
        if (playingBgmId != _currentBgmId)
        {
            var previousBgmId = _currentBgmId;
            _currentBgmId = playingBgmId;
            _currentBgmFile = GetBgmFile(playingBgmId);
            _pendingLyricsStart = playingBgmId != 0;

            DebugOut(new SeStringBuilder().Append($"BGM changed from {previousBgmId} to {_currentBgmId} ({_currentBgmFile})").BuiltString);

            if (playingBgmId == 0)
            {
                Utils.LyricsManager.Instance.Stop();
            }
        }

        SongClock.Instance.Update(_currentBgmFile);

        if (_pendingLyricsStart && (string.IsNullOrEmpty(_currentBgmFile) || SongClock.Instance.IsTracking(_currentBgmFile)))
        {
            _pendingLyricsStart = false;
            PlayCurrentBgm();
        }

        LyricsManager.Instance.Update();
    }

    private static string GetBgmFile(ushort bgmId)
    {
        if (bgmId == 0)
            return string.Empty;

        var bgmSheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BGM>();
        var bgmRow = bgmSheet?.GetRowOrDefault(bgmId);
        return bgmRow?.File.ToString() ?? string.Empty;
    }

    private void PlayCurrentBgm()
    {
        if (_currentBgmId != 0)
        {
            Utils.LyricsManager.Instance.Start(_currentBgmId, _currentBgmFile);
        }
        else
        {
            Utils.LyricsManager.Instance.Stop();
        }
    }

    private void ApplyLyricVolumeOverrides()
    {
        if (!Service.Configuration.EnableVolumeOverrides)
        {
            return;
        }

        var volumeOverrides = new List<(string ConfigName, uint VolumeValue)>
        {
            ("SoundMaster", Service.Configuration.VolumeSoundMaster),
            ("SoundBgm", Service.Configuration.VolumeSoundBgm),
            ("SoundSe", Service.Configuration.VolumeSoundSe),
            ("SoundVoice", Service.Configuration.VolumeSoundVoice),
            ("SoundEnv", Service.Configuration.VolumeSoundEnv),
            ("SoundSystem", Service.Configuration.VolumeSoundSystem),
            ("SoundPerform", Service.Configuration.VolumeSoundPerform)
        };

        if (!_volumeOverridesApplied)
        {
            _savedVolumeDefaults.Clear();
            foreach (var item in volumeOverrides)
            {
                _savedVolumeDefaults[item.ConfigName] = Service.GameConfig.System.GetUInt(item.ConfigName);
            }
        }

        foreach (var item in volumeOverrides)
        {
            Service.GameConfig.System.Set(item.ConfigName, item.VolumeValue);
        }

        _volumeOverridesApplied = true;
    }

    private void RestoreDefaultVolumes()
    {
        if (!_volumeOverridesApplied)
        {
            return;
        }

        foreach (var saved in _savedVolumeDefaults)
        {
            Service.GameConfig.System.Set(saved.Key, saved.Value);
        }

        _savedVolumeDefaults.Clear();
        _volumeOverridesApplied = false;
    }

    public static void ChatOut(SeString message)
    {
        var sb = new SeStringBuilder().AddUiForeground("[Singingway] ", 58).Append(message);
        Service.ChatGui.Print(new XivChatEntry { Message = sb.BuiltString });
    }

    public static void DebugOut(SeString message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string formattedMessage = $"[{timestamp}] {message}";

        lock (DebugMessages)
        {
            DebugMessages.Add(formattedMessage);
            if (DebugMessages.Count > MaxDebugMessages)
            {
                DebugMessages.RemoveAt(0);
            }
        }
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        Service.CommandManager?.RemoveHandler(LyricsCommand);
        Utils.LyricsManager.Instance.Dispose();

        Service.Framework.Update -= OnFrameworkUpdate;
    }

    private void DrawUI() => WindowSystem.Draw();

    public static void DrawConfigUI() => Service.ConfigWindow.IsOpen = true;

    private void OnLyricsCommand(string command, string args)
    {
        var parts = (args ?? string.Empty).Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            ChatOut(new SeStringBuilder().Append("Usage: /singingway start <bgmid> | /singingway stop | /singingway config").BuiltString);
            return;
        }

        var sub = parts[0].ToLowerInvariant();
        if (sub == "start")
        {
            if (parts.Length > 1 && ushort.TryParse(parts[1].Trim(), out var bgmId))
                Utils.LyricsManager.Instance.Start(bgmId, GetBgmFile(bgmId));
            else
                ChatOut(new SeStringBuilder().Append("Usage: /singingway start <bgmid>").BuiltString);
            return;
        }
        if (sub == "stop")
        {
            Utils.LyricsManager.Instance.Stop();
            return;
        }
        if (sub == "config")
        {
            Service.ConfigWindow.IsOpen = !Service.ConfigWindow.IsOpen;
            return;
        }

        ChatOut(new SeStringBuilder().Append("Unknown /lyrics subcommand").BuiltString);
    }
}
