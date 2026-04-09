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
        private bool positionInitialized = false;

        private float _storedCenterX;

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
            float backgroundOpacity = Service.configuration.BackgroundOpacityPercentage / 100.0f;

            Fonts.TitleFont.Push();
            var textSize = Text.CalcSize(text);
            Fonts.TitleFont.Pop();

            Fonts.SubTitleFont.Push();
            var nextTextSize = Text.CalcSize(nextText);
            Fonts.SubTitleFont.Pop();

            CalculateLayout(textSize.X, nextTextSize.X, out float targetWidth, out float mainTextScale, out float nextTextScale);
            float windowHeight = Math.Max(DefaultHeight, baseY + textSize.Y + 4f + nextTextSize.Y + 8f + 12f + 8f);

            InitializeWindowPosition(targetWidth, windowHeight);
            ImGui.SetNextWindowSize(new Vector2(targetWidth, windowHeight), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("##LyricsWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.End();
                return;
            }

            DrawFullWindowBackground(backgroundOpacity);
            AdjustWindowSizeAndPosition(targetWidth, windowHeight);
            var windowWidth = ImGui.GetWindowSize().X;

            DrawTotalTime(windowWidth, elapsed, lm.GetTotalDuration());

            Fonts.TitleFont.Push();
            DrawTextLine(text, textSize, mainTextScale, windowWidth, baseY, 0xFFFFFFFF, backgroundOpacity);
            Fonts.TitleFont.Pop();

            float nextBaseY = baseY + textSize.Y + 4f;
            if (!string.IsNullOrEmpty(nextText))
            {
                Fonts.SubTitleFont.Push();
                DrawTextLine(nextText, nextTextSize, nextTextScale, windowWidth, nextBaseY, 0xFFAAAAAA, backgroundOpacity);
                Fonts.SubTitleFont.Pop();
            }

            float barBaseY = nextBaseY + nextTextSize.Y + 8f;
            DrawProgressBar(windowWidth, textSize.X * mainTextScale, barBaseY, elapsed, nextTime, lm.GetPreviousTimestamp(), lm.GetTotalDuration());

            HandleWindowDragging();

            ImGui.End();
        }

        private void CalculateLayout(float mainWidth, float nextWidth, out float targetWidth, out float mainScale, out float nextScale)
        {
            float padding = 40f;
            float maxConfigWidth = Service.configuration.MaxWindowWidth;

            float contentWidth = Math.Max(mainWidth, nextWidth) + padding;
            targetWidth = Math.Max(Service.configuration.MinWindowWidth, Math.Min(contentWidth, maxConfigWidth));

            mainScale = CalculateTextScale(mainWidth, maxConfigWidth, padding);
            nextScale = CalculateTextScale(nextWidth, maxConfigWidth, padding);
        }

        private float CalculateTextScale(float textWidth, float maxWidth, float padding)
        {
            float scale = 1.0f;
            float contentWidth = textWidth + padding;
            if (contentWidth > maxWidth)
            {
                scale = maxWidth / contentWidth;
            }
            return scale * (Service.configuration.TextScalePercentage / 100f);
        }

        private void InitializeWindowPosition(float targetWidth, float windowHeight)
        {
            if (positionInitialized) return;

            var io = ImGui.GetIO();

            _storedCenterX = io.DisplaySize.X / 2f;

            var centerY = io.DisplaySize.Y / 2f - windowHeight / 2f;
            float initialLeftX = _storedCenterX - (targetWidth / 2f);

            ImGui.SetNextWindowPos(new Vector2(initialLeftX, centerY), ImGuiCond.FirstUseEver);
            positionInitialized = true;
        }

        private void AdjustWindowSizeAndPosition(float targetWidth, float windowHeight)
        {
            var currentSize = ImGui.GetWindowSize();

            if (Math.Abs(currentSize.X - targetWidth) > 0.1f)
            {
                var currentPos = ImGui.GetWindowPos();
                var newLeftX = (float)Math.Round(_storedCenterX - (targetWidth / 2f));

                ImGui.SetWindowPos(new Vector2(newLeftX, currentPos.Y));
                ImGui.SetWindowSize(new Vector2(targetWidth, windowHeight));
            }
        }

        private void DrawTextLine(string text, Vector2 originalSize, float scale, float windowWidth, float baseY, uint color, float bgOpacity)
        {
            float scaledSizeX = originalSize.X * scale;
            float scaledSizeY = originalSize.Y * scale;
            float xPos = Math.Max(0, (windowWidth - scaledSizeX) / 2f);
            float verticalCenterOffset = (originalSize.Y - scaledSizeY) / 2f;

            ImGui.SetWindowFontScale(scale);

            if (bgOpacity > 0.0f && !Service.configuration.BackgroundFullWindow && !string.IsNullOrEmpty(text))
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

        private void DrawFullWindowBackground(float opacity)
        {
            if (opacity <= 0.0f || !Service.configuration.BackgroundFullWindow) return;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();
            uint bgColorWithOpacity = GetBackgroundColorWithOpacity(opacity);

            drawList.AddRectFilled(windowPos, windowPos + windowSize, bgColorWithOpacity);
        }

        private uint GetBackgroundColorWithOpacity(float opacity)
        {
            uint bgColor = Service.configuration.BackgroundColor;
            byte alpha = (byte)(((bgColor >> 24) & 0xFF) * opacity);
            return (bgColor & 0x00FFFFFF) | ((uint)alpha << 24);
        }

        private string FormatTime(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.fff");
        }

        private void DrawTotalTime(float windowWidth, double elapsed, double totalSongTime)
        {
            if (!Service.configuration.ShowTotalTime) return;

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
            if (!nextTime.HasValue || !Service.configuration.ShowProgressBar) return;

            var timingOffset = Service.configuration.TimingOffsetSeconds;
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
            }
        }
    }
}
