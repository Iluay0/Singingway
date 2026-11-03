using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Singingway.Utils;
using System;
using System.Numerics;

namespace Singingway.Windows.Lyrics;
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
        float baseY = 10f;
        var lm = Utils.LyricsManager.Instance;
        var currentLine = lm.GetCurrentLine();
        bool plainCurrent = currentLine != null && currentLine.Segments.Count > 0;
        var text = plainCurrent ? currentLine!.DisplayText : lm.GetCurrentDisplayText();
        var nextText = lm.GetNextDisplayText();
        var nextTime = lm.GetNextTimestamp();
        var elapsed = lm.GetElapsedSeconds();
        float backgroundOpacity = Service.Configuration.BackgroundOpacityPercentage / 100.0f;

        Fonts.TitleFont?.Push();
        var textSize = plainCurrent ? ImGui.CalcTextSize(text) : Text.CalcSize(text);
        if (string.IsNullOrEmpty(text))
            textSize = new Vector2(0f, ImGui.GetTextLineHeight());
        Fonts.TitleFont?.Pop();

        Fonts.SubTitleFont?.Push();
        var nextTextSize = Text.CalcSize(nextText);
        Fonts.SubTitleFont?.Pop();

        CalculateLayoutAndResize(textSize, nextTextSize, baseY, out float targetWidth, out float windowHeight, out float mainTextScale, out float nextTextScale);

        if (!ImGui.Begin("##LyricsWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.End();
            return;
        }

        InitializeWindow();
        DrawFullWindowBackground(backgroundOpacity);

        var windowWidth = ImGui.GetWindowSize().X;

        float lineSpacing = Service.Configuration.LineSpacing;

        DrawTotalTime(windowWidth, elapsed, lm.GetTotalDuration());

        Fonts.TitleFont?.Push();
        DrawTextLine(text, textSize, mainTextScale, windowWidth, baseY, Service.Configuration.TextColor, backgroundOpacity, currentLine, elapsed, nextTime);
        Fonts.TitleFont?.Pop();

        float nextBaseY = baseY + textSize.Y * mainTextScale + lineSpacing;
        if (!string.IsNullOrEmpty(nextText))
        {
            Fonts.SubTitleFont?.Push();
            DrawTextLine(nextText, nextTextSize, nextTextScale, windowWidth, nextBaseY, Service.Configuration.NextLineColor, backgroundOpacity);
            Fonts.SubTitleFont?.Pop();
        }

        HandleWindowDragging();

        ImGui.End();
    }

    private void CalculateLayoutAndResize(Vector2 mainSize, Vector2 nextSize, float baseY, out float targetWidth, out float windowHeight, out float mainScale, out float nextScale)
    {
        float padding = 40f;
        float maxConfigWidth = Service.Configuration.MaxWindowWidth;
        float configScale = Service.Configuration.TextScalePercentage / 100f;
        float scaledMainWidth = mainSize.X * configScale;
        float scaledNextWidth = nextSize.X * configScale;

        float contentWidth = Math.Max(scaledMainWidth, scaledNextWidth) + padding;
        targetWidth = Math.Max(Service.Configuration.MinWindowWidth, Math.Min(contentWidth, maxConfigWidth));

        mainScale = configScale;
        if (scaledMainWidth + padding > maxConfigWidth)
        {
            mainScale = configScale * (maxConfigWidth / (scaledMainWidth + padding));
        }

        nextScale = configScale;
        if (scaledNextWidth + padding > maxConfigWidth)
        {
            nextScale = configScale * (maxConfigWidth / (scaledNextWidth + padding));
        }

        float finalMainHeight = mainSize.Y * mainScale;
        float finalNextHeight = nextSize.Y * nextScale;

        float lineSpacing = Service.Configuration.LineSpacing;
        windowHeight = Math.Max(DefaultHeight, baseY + finalMainHeight + lineSpacing + finalNextHeight + 8f);

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

            _lastWidth = targetWidth;
            _lastHeight = windowHeight;
        }
    }

    private void DrawTextLine(string text, Vector2 originalSize, float scale, float windowWidth, float baseY, uint color, float bgOpacity, TimedLine? highlightLine = null, double lyricTime = 0.0, double nextTime = 0.0)
    {
        float scaledSizeX = originalSize.X * scale;
        float scaledSizeY = originalSize.Y * scale;
        float xPos = Math.Max(0, (windowWidth - scaledSizeX) / 2f);

        ImGui.SetWindowFontScale(scale);

        if (bgOpacity > 0.0f && !Service.Configuration.BackgroundFullWindow && !string.IsNullOrEmpty(text))
        {
            DrawTextBackground(xPos, baseY, scaledSizeX, scaledSizeY, bgOpacity);
        }

        bool plain = highlightLine != null && highlightLine.Segments.Count > 0;

        Vector2 linePos = new Vector2(xPos, baseY);
        ImGui.SetCursorPos(linePos);
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        if (plain)
            DrawPlainWithShadow(text, Service.Configuration.ShadowColor);
        else
            Text.DrawWithShadow(text, Service.Configuration.ShadowColor);
        ImGui.PopStyleColor();

        if (highlightLine != null && !string.IsNullOrEmpty(text))
        {
            float highlightWidth = CalcHighlightWidth(text, highlightLine, lyricTime, nextTime);
            if (highlightWidth > 0f)
            {
                Vector2 clipMin = ImGui.GetWindowPos() + linePos;
                float lineHeight = plain ? ImGui.CalcTextSize(text).Y : Text.CalcSize(text).Y;
                Vector2 clipMax = clipMin + new Vector2(highlightWidth, lineHeight + 1f);
                ImGui.PushClipRect(clipMin, clipMax, true);
                ImGui.SetCursorPos(linePos);
                Text.PushStyleColor(ImGuiCol.Text, Service.Configuration.HighlightColor);
                if (plain)
                    ImGui.TextUnformatted(text);
                else
                    Text.Draw(text);
                Text.PopStyleColor();
                ImGui.PopClipRect();
            }
        }

        ImGui.SetWindowFontScale(1.0f);
    }

    private static void DrawPlainWithShadow(string text, uint shadowColor)
    {
        if (string.IsNullOrEmpty(text))
            return;

        Vector2 cursorPos = ImGui.GetCursorPos();
        ImGui.PushStyleColor(ImGuiCol.Text, shadowColor);
        foreach (var offset in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
        {
            ImGui.SetCursorPos(cursorPos + offset);
            ImGui.TextUnformatted(text);
        }
        ImGui.PopStyleColor();

        ImGui.SetCursorPos(cursorPos);
        ImGui.TextUnformatted(text);
    }

    private static float CalcHighlightWidth(string text, TimedLine line, double t, double nextTime)
    {
        bool sweep = Service.Configuration.HighlightMode == HighlightMode.Sweep;
        var segments = line.Segments;

        if (segments.Count == 0)
        {
            double lineEnd = line.End ?? nextTime;
            float progress = Progress(t, line.Time, lineEnd);
            float fullWidth = Text.CalcSize(text).X;
            if (sweep)
                return fullWidth * progress;
            return progress > 0f ? fullWidth : 0f;
        }

        int current = -1;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].Time <= t)
                current = i;
            else
                break;
        }
        if (current < 0)
            return 0f;

        var segment = segments[current];
        double segEnd = current + 1 < segments.Count ? segments[current + 1].Time : line.End ?? nextTime;
        float segProgress = Progress(t, segment.Time, segEnd);

        int startChar = Math.Clamp(segment.StartChar, 0, text.Length);
        int endChar = Math.Clamp(segment.EndChar, startChar, text.Length);
        float startWidth = ImGui.CalcTextSize(text[..startChar]).X;

        if (sweep)
        {
            float segWidth = ImGui.CalcTextSize(text[startChar..endChar].TrimEnd()).X;
            return startWidth + segWidth * segProgress;
        }

        return segProgress > 0f ? ImGui.CalcTextSize(text[..endChar].TrimEnd()).X : startWidth;
    }

    private static float Progress(double t, double start, double end)
    {
        double span = end - start;
        if (span <= 0)
            return 1f;
        return (float)Math.Clamp((t - start) / span, 0.0, 1.0);
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
        Text.DrawWithShadow(timeText, Service.Configuration.ShadowColor);
        ImGui.PopStyleColor();
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