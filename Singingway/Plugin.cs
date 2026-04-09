using Dalamud.Game.Command;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Singingway.Utils;
using Singingway.Windows;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Singingway
{
    internal sealed class Plugin : IDalamudPlugin
    {
        public static string Name => "Singingway";
        private const string LyricsCommand = "/singingway";

        public static List<string> DebugMessages { get; } = new();
        private const int MaxDebugMessages = 1000;

        public WindowSystem WindowSystem { get; } = new("Singingway");

        private const int SongFadeDurationMs = 1500;

        private bool _wasLoading = false;
        private ushort _currentBgmId = 0;
        private BgmHelper _bgmHelper = new BgmHelper();

        public Plugin(IDalamudPluginInterface pluginInterface)
        {
            Service.pluginInterface = pluginInterface;

            Service.configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Service.configuration.Initialize(pluginInterface);

            _ = pluginInterface.Create<Service>();
            Service.plugin = this;

            Service.configWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(Service.configWindow);

            Service.lyricsWindow = new Windows.LyricsWindow();
            WindowSystem.AddWindow(Service.lyricsWindow);

            Utils.LyricsManager.Instance.PlayingChanged += playing =>
            {
                Service.lyricsWindow.IsOpen = playing;
                if (!playing)
                {
                    PlayCurrentBgm();
                }
            };

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            Service.commandManager.AddHandler(LyricsCommand, new CommandInfo(OnLyricsCommand)
            {
                HelpMessage = "Singingway: /singingway start <bgmfile> | /singingway stop | /singingway config"
            });

            Service.Framework.Update += OnFrameworkUpdate;

            Fonts.Initialize().ConfigureAwait(false);

            DebugOut("Plugin Loaded!");
        }

        private CancellationTokenSource _songDelayTokenSource;
        private void OnFrameworkUpdate(IFramework framework)
        {
            LyricsManager.Instance.Update();

            bool isLoading = IsLoadingScreen();
            if (isLoading)
            {
                _wasLoading = true;
                return;
            }

            var playingBgmId = _bgmHelper.GetActiveBgmId();
            if (playingBgmId != _currentBgmId)
            {
                var previousBgmId = _currentBgmId;
                _currentBgmId = playingBgmId;

                _songDelayTokenSource?.Cancel();
                _songDelayTokenSource = new CancellationTokenSource();

                int delayMs = GetTransitionDelay(previousBgmId, _currentBgmId, _wasLoading);
                Plugin.DebugOut(new SeStringBuilder().Append($"BGM changed from {previousBgmId} to {_currentBgmId}, applying delay of {delayMs}ms").BuiltString);
                _ = PlayCurrentBgmDelayed(_currentBgmId, delayMs, _songDelayTokenSource.Token);
            }

            _wasLoading = false;
        }

        private unsafe bool IsLoadingScreen()
        {
            var titleCard = (AtkUnitBase*)Service.GameGui.GetAddonByName("_LocationTitle").Address;
            var blackScreen = (AtkUnitBase*)Service.GameGui.GetAddonByName("FadeMiddle").Address;
            return titleCard != null && titleCard->IsVisible || blackScreen != null && blackScreen->IsVisible;
        }

        private int GetTransitionDelay(ushort previousBgmId, ushort currentBgmId, bool wasLoading)
        {
            if (wasLoading)
            {
                return 0;
            }

            return SongFadeDurationMs;
        }

        private void PlayCurrentBgm()
        {
            if (_currentBgmId != 0)
            {
                var bgmSheet = Service.DataManager.GetExcelSheet<Lumina.Excel.Sheets.BGM>();
                var bgmRow = bgmSheet?.GetRowOrDefault(_currentBgmId);

                var fileName = bgmRow?.File.ToString();
                if (!string.IsNullOrEmpty(fileName))
                {
                    Utils.LyricsManager.Instance.Start(fileName);
                }
                else
                {
                    Utils.LyricsManager.Instance.Start(_currentBgmId.ToString());
                }
            }
            else
            {
                Utils.LyricsManager.Instance.Stop();
            }
        }

        private async Task PlayCurrentBgmDelayed(ushort targetBgmId, int delayMs, CancellationToken token)
        {
            try
            {
                if (targetBgmId == 0)
                {
                    Utils.LyricsManager.Instance.Stop();
                    return;
                }

                await Task.Delay(delayMs, token);

                token.ThrowIfCancellationRequested();
                PlayCurrentBgm();
            }
            catch (TaskCanceledException)
            {

            }
        }

        public static void ChatOut(SeString message)
        {
            var sb = new SeStringBuilder().AddUiForeground("[Singingway] ", 58).Append(message);
            Service.chatGui.Print(new XivChatEntry { Message = sb.BuiltString });
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
            Service.commandManager?.RemoveHandler(LyricsCommand);
            Utils.LyricsManager.Instance.Dispose();

            Service.Framework.Update -= OnFrameworkUpdate;
        }

        private void DrawUI() => WindowSystem.Draw();

        public static void DrawConfigUI() => Service.configWindow.IsOpen = true;

        private void OnLyricsCommand(string command, string args)
        {
            var parts = (args ?? string.Empty).Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                ChatOut(new SeStringBuilder().Append("Usage: /singingway start <bgmfile> | /singingway stop | /singingway config").BuiltString);
                return;
            }

            var sub = parts[0].ToLowerInvariant();
            if (sub == "start")
            {
                if (parts.Length > 1)
                    Utils.LyricsManager.Instance.Start(parts[1]);
                return;
            }
            if (sub == "stop")
            {
                Utils.LyricsManager.Instance.Stop();
                return;
            }
            if (sub == "config")
            {
                Service.configWindow.IsOpen = !Service.configWindow.IsOpen;
                return;
            }

            ChatOut(new SeStringBuilder().Append("Unknown /lyrics subcommand").BuiltString);
        }
    }
}
