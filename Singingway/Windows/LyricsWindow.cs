using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Singingway.Utils;
using System;
using System.Numerics;

namespace Singingway.Windows
{
    internal class LyricsWindow : Window
    {
        private const float DefaultHeight = 100f;

        private bool _isInitialized = false;
        private float _storedCenterX;
        private float _currentY;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;

        public LyricsWindow() : base("Lyrics")
        {
            Flags = ImGuiWindowFlags.NoDecoration;
            RespectCloseHotkey = false;
        }

        public override void Draw()
        {
            float baseY = 8f;
            var lm = Utils.LyricsManager.Instance;
            var text = lm.GetCurrentDisplayText();
            var nextText = lm.GetNextDisplayText();
            var nextTime = lm.GetNextTimestamp();
            var elapsed = lm.GetElapsedSeconds();
            float backgroundOpacity = Service.Configuration.BackgroundOpacityPercentage / 100.0f;

            Fonts.TitleFont?.Push();
            var textSize = Text.CalcSize(text);
            Fonts.TitleFont?.Pop();

            Fonts.SubTitleFont?.Push();
            var nextTextSize = Text.CalcSize(nextText);
            Fonts.SubTitleFont?.Pop();

            float windowHeight = Math.Max(DefaultHeight, baseY + textSize.Y + 4f + nextTextSize.Y + 8f + 12f + 8f);
            CalculateLayoutAndResize(textSize.X, nextTextSize.X, windowHeight, out float targetWidth, out float mainTextScale, out float nextTextScale);

            if (!ImGui.Begin("##LyricsWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.End();
                return;
            }

            InitializeWindow();
            DrawFullWindowBackground(backgroundOpacity);

            var windowWidth = ImGui.GetWindowSize().X;

            DrawTotalTime(windowWidth, elapsed, lm.GetTotalDuration());

            Fonts.TitleFont?.Push();
            DrawTextLine(text, textSize, mainTextScale, windowWidth, baseY, 0xFFFFFFFF, backgroundOpacity);
            Fonts.TitleFont?.Pop();

            float nextBaseY = baseY + textSize.Y + 4f;
            if (!string.IsNullOrEmpty(nextText))
            {
                Fonts.SubTitleFont?.Push();
                DrawTextLine(nextText, nextTextSize, nextTextScale, windowWidth, nextBaseY, 0xFFAAAAAA, backgroundOpacity);
                Fonts.SubTitleFont?.Pop();
            }

            float barBaseY = nextBaseY + nextTextSize.Y + 8f;
            DrawProgressBar(windowWidth, textSize.X * mainTextScale, barBaseY, elapsed, nextTime, lm.GetPreviousTimestamp(), lm.GetTotalDuration());

            HandleWindowDragging();

            ImGui.End();
        }

        private void CalculateLayoutAndResize(float mainWidth, float nextWidth, float windowHeight, out float targetWidth, out float mainScale, out float nextScale)
        {
            float padding = 40f;
            float maxConfigWidth = Service.Configuration.MaxWindowWidth;

            float contentWidth = Math.Max(mainWidth, nextWidth) + padding;
            targetWidth = Math.Max(Service.Configuration.MinWindowWidth, Math.Min(contentWidth, maxConfigWidth));

            mainScale = CalculateTextScale(mainWidth, maxConfigWidth, padding);
            nextScale = CalculateTextScale(nextWidth, maxConfigWidth, padding);

            if (!_isInitialized)
            {
                var io = ImGui.GetIO();
                float fallbackX = io.DisplaySize.X / 2f - targetWidth / 2f;
                float fallbackY = io.DisplaySize.Y / 2f - windowHeight / 2f;

                ImGui.SetNextWindowPos(new Vector2(fallbackX, fallbackY), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(targetWidth, windowHeight), ImGuiCond.FirstUseEver);
            }
            else if (Math.Abs(targetWidth - _lastWidth) > 0.1f || Math.Abs(windowHeight - _lastHeight) > 0.1f)
            {
                float newLeftX = (float)Math.Round(_storedCenterX - (targetWidth / 2f));
                ImGui.SetNextWindowPos(new Vector2(newLeftX, _currentY), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(targetWidth, windowHeight), ImGuiCond.Always);
            }
        }

        private float CalculateTextScale(float textWidth, float maxWidth, float padding)
        {
            float scale = 1.0f;
            float contentWidth = textWidth + padding;
            if (contentWidth > maxWidth)
            {
                scale = maxWidth / contentWidth;
            }
            return scale * (Service.Configuration.TextScalePercentage / 100f);
        }

        private void DrawTextLine(string text, Vector2 originalSize, float scale, float windowWidth, float baseY, uint color, float bgOpacity)
        {
            float scaledSizeX = originalSize.X * scale;
            float scaledSizeY = originalSize.Y * scale;
            float xPos = Math.Max(0, (windowWidth - scaledSizeX) / 2f);
            float verticalCenterOffset = (originalSize.Y - scaledSizeY) / 2f;

            ImGui.SetWindowFontScale(scale);

            if (bgOpacity > 0.0f && !Service.Configuration.BackgroundFullWindow && !string.IsNullOrEmpty(text))
            {
                DrawTextBackground(xPos, baseY + verticalCenterOffset, scaledSizeX, scaledSizeY, bgOpacity);
            }

            ImGui.SetCursorPos(new Vector2(xPos, baseY + verticalCenterOffset));
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            Text.DrawWithShadow(text, 0xFF000000);
            ImGui.PopStyleColor();

            ImGui.SetWindowFontScale(1.0f);
        }

        private void DrawTextBackground(float x, float y, float width, float height, float opacity)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            uint bgColorWithOpacity = GetBackgroundColorWithOpacity(opacity);

            Vector2 bgStart = windowPos + new Vector2(x - 5, y - 5);
            Vector2 bgEnd = windowPos + new Vector2(x + width + 5, y + height + 5);
            drawList.AddRectFilled(bgStart, bgEnd, bgColorWithOpacity);
        }

        private void InitializeWindow()
        {
            if (_isInitialized) return;

            var pos = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();

            _storedCenterX = pos.X + (size.X / 2f);
            _currentY = pos.Y;
            _lastWidth = size.X;
            _lastHeight = size.Y;

            _isInitialized = true;
        }

        private void DrawFullWindowBackground(float opacity)
        {
            if (opacity <= 0.0f || !Service.Configuration.BackgroundFullWindow) return;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            uint bgColorWithOpacity = GetBackgroundColorWithOpacity(opacity);

            drawList.AddRectFilled(windowPos, windowPos + windowSize, bgColorWithOpacity);
        }

        private uint GetBackgroundColorWithOpacity(float opacity)
        {
            uint bgColor = Service.Configuration.BackgroundColor;
            byte alpha = (byte)(((bgColor >> 24) & 0xFF) * opacity);
            return (bgColor & 0x00FFFFFF) | ((uint)alpha << 24);
        }

        private string FormatTime(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.fff");
        }

        private void DrawTotalTime(float windowWidth, double elapsed, double totalSongTime)
        {
            if (!Service.Configuration.ShowTotalTime) return;

            string timeText = $"{FormatTime(elapsed)} / {FormatTime(totalSongTime)}";
            var timeTextSize = ImGui.CalcTextSize(timeText);

            float timeXPos = windowWidth / 2f - timeTextSize.X / 2f;
            ImGui.SetCursorPos(new Vector2(timeXPos, 0f));

            ImGui.PushStyleColor(ImGuiCol.Text, 0xFFCCCCCC);
            Text.DrawWithShadow(timeText, 0xFF000000);
            ImGui.PopStyleColor();
        }

        private void DrawProgressBar(float windowWidth, float scaledMainTextWidth, float baseY, double elapsed, double? nextTime, double prevTime, double totalSongTime)
        {
            if (!nextTime.HasValue || !Service.Configuration.ShowProgressBar) return;

            var timingOffset = Service.Configuration.TimingOffsetSeconds;
            var adjustedElapsed = elapsed - timingOffset;
            var adjustedNextTime = nextTime.Value - timingOffset;
            var total = adjustedNextTime - prevTime;
            var prog = 0.0f;

            if (total > 0)
                prog = (float)Math.Clamp((adjustedElapsed - prevTime) / total, 0.0, 1.0);

            float progressBarWidth = Math.Min(windowWidth, Math.Max(320f, scaledMainTextWidth));
            float barXPos = Math.Max(0, (windowWidth - progressBarWidth) / 2f);

            ImGui.SetCursorPos(new Vector2(barXPos, baseY));
            ImGui.ProgressBar(prog, new Vector2(progressBarWidth, 12), "");
        }

        private void HandleWindowDragging()
        {
            if (ImGui.IsWindowHovered() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var currentWindowPos = ImGui.GetWindowPos();
                currentWindowPos += ImGui.GetIO().MouseDelta;
                ImGui.SetWindowPos(currentWindowPos);

                var currentSize = ImGui.GetWindowSize();
                _storedCenterX = currentWindowPos.X + (currentSize.X / 2f);
                _currentY = currentWindowPos.Y;
            }
        }
    }
}