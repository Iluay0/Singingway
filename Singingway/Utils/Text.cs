using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Singingway.Utils
{
    public static class Text
    {
        static bool HasForcedColor = false;

        public struct TextFragment
        {
            public string Text;
            public Vector4? Color;
            public bool IsBold;
            public bool IsUnderline;
        }

        public static Vector2 CalcSize(string? text)
        {
            if (string.IsNullOrEmpty(text)) return ImGui.CalcTextSize(text);

            if (!_cache.TryGetValue(text, out List<TextFragment>? fragments))
            {
                fragments = Parse(text);
                _cache[text] = fragments;
            }

            float totalWidth = 0f;
            float maxHeight = 0f;

            for (int i = 0; i < fragments.Count; i++)
            {
                var frag = fragments[i];

                Vector2 fragSize = ImGui.CalcTextSize(frag.Text);

                if (frag.IsBold)
                {
                    fragSize.X += 1.0f;
                    fragSize.Y += 1.0f;
                }

                totalWidth += fragSize.X;
                maxHeight = Math.Max(maxHeight, fragSize.Y);
            }

            return new Vector2(totalWidth, maxHeight);
        }

        public static void PushStyleColor(ImGuiCol idx, uint col)
        {
            ImGui.PushStyleColor(idx, col);
            HasForcedColor = true;
        }

        public static void PopStyleColor()
        {
            ImGui.PopStyleColor();
            HasForcedColor = false;
        }

        private static readonly Dictionary<string, List<TextFragment>> _cache = new Dictionary<string, List<TextFragment>>();

        public static List<TextFragment> Parse(string? text)
        {
            var fragments = new List<TextFragment>();
            if (string.IsNullOrEmpty(text)) return fragments;

            bool isBold = false;
            bool isUnderline = false;
            Vector4? currentColor = null;

            int i = 0;
            int lastTextStart = 0;

            void FlushText(int currentIndex)
            {
                if (currentIndex > lastTextStart)
                {
                    fragments.Add(new TextFragment
                    {
                        Text = text.Substring(lastTextStart, currentIndex - lastTextStart),
                        IsBold = isBold,
                        IsUnderline = isUnderline,
                        Color = currentColor
                    });
                }
            }

            while (i < text.Length)
            {
                if (i < text.Length - 1 && text[i] == '*' && text[i + 1] == '*')
                {
                    FlushText(i);
                    isBold = !isBold;
                    i += 2;
                    lastTextStart = i;
                    continue;
                }
                if (i < text.Length - 1 && text[i] == '_' && text[i + 1] == '_')
                {
                    FlushText(i);
                    isUnderline = !isUnderline;
                    i += 2;
                    lastTextStart = i;
                    continue;
                }
                if (i + 14 <= text.Length && text.Substring(i, 8) == "<color=#")
                {
                    int closeIdx = text.IndexOf('>', i);
                    if (closeIdx != -1)
                    {
                        FlushText(i);
                        string hex = text.Substring(i + 8, closeIdx - (i + 8));
                        currentColor = ParseHexColor(hex);
                        i = closeIdx + 1;
                        lastTextStart = i;
                        continue;
                    }
                }
                if (i + 8 <= text.Length && text.Substring(i, 8) == "</color>")
                {
                    FlushText(i);
                    currentColor = null;
                    i += 8;
                    lastTextStart = i;
                    continue;
                }
                i++;
            }

            FlushText(text.Length);
            return fragments;
        }

        private static Vector4? ParseHexColor(string hex)
        {
            try
            {
                if (hex.Length == 6 || hex.Length == 8)
                {
                    float r = Convert.ToByte(hex.Substring(0, 2), 16) / 255f;
                    float g = Convert.ToByte(hex.Substring(2, 2), 16) / 255f;
                    float b = Convert.ToByte(hex.Substring(4, 2), 16) / 255f;
                    float a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) / 255f : 1.0f;
                    return new Vector4(r, g, b, a);
                }
            }
            catch { }
            return null;
        }

        public static void DrawWithShadow(string? text, uint shadowColor, float shadowOffsetX = 1.0f, float shadowOffsetY = 1.0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Vector2 cursorPos = ImGui.GetCursorPos();
            uint mainColor = ImGui.GetColorU32(ImGuiCol.Text);

            PushStyleColor(ImGuiCol.Text, shadowColor);
            ImGui.SetCursorPos(new Vector2(cursorPos.X - shadowOffsetX, cursorPos.Y - shadowOffsetY));
            Draw(text);
            ImGui.SetCursorPos(new Vector2(cursorPos.X + shadowOffsetX, cursorPos.Y - shadowOffsetY));
            Draw(text);
            ImGui.SetCursorPos(new Vector2(cursorPos.X - shadowOffsetX, cursorPos.Y + shadowOffsetY));
            Draw(text);
            ImGui.SetCursorPos(new Vector2(cursorPos.X + shadowOffsetX, cursorPos.Y + shadowOffsetY));
            Draw(text);
            PopStyleColor();

            ImGui.SetCursorPos(cursorPos);
            PushStyleColor(ImGuiCol.Text, mainColor);
            Draw(text);
            PopStyleColor();
        }

        public static void Draw(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!_cache.TryGetValue(text, out List<TextFragment>? fragments))
            {
                fragments = Parse(text);
                _cache[text] = fragments;
            }

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            for (int i = 0; i < fragments.Count; i++)
            {
                var frag = fragments[i];

                if (frag.Color.HasValue && !HasForcedColor)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, frag.Color.Value);
                }

                Vector2 cursorPos = ImGui.GetCursorScreenPos();
                uint currentTextColor = ImGui.GetColorU32(ImGuiCol.Text);

                if (frag.IsBold)
                {
                    drawList.AddText(cursorPos + new Vector2(1, 0), currentTextColor, frag.Text);
                    drawList.AddText(cursorPos + new Vector2(0, 1), currentTextColor, frag.Text);
                    drawList.AddText(cursorPos + new Vector2(1, 1), currentTextColor, frag.Text);
                }

                Vector2 textSize = ImGui.CalcTextSize(frag.Text);

                ImGui.TextUnformatted(frag.Text);

                if (frag.IsUnderline)
                {
                    Vector2 lineStart = new Vector2(cursorPos.X, cursorPos.Y + textSize.Y);
                    Vector2 lineEnd = new Vector2(cursorPos.X + textSize.X, cursorPos.Y + textSize.Y);
                    drawList.AddLine(lineStart, lineEnd, currentTextColor, 1.0f);
                }

                if (frag.Color.HasValue && !HasForcedColor)
                {
                    ImGui.PopStyleColor();
                }

                if (i < fragments.Count - 1)
                {
                    ImGui.SameLine(0, 0);
                }
            }

            if (fragments.Count > 0)
            {
                ImGui.NewLine();
            }
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
