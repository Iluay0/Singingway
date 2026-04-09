using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Singingway
{
    [Serializable]
    internal class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        private int minWindowWidth = 200;
        public int MinWindowWidth
        {
            get => minWindowWidth;
            set => minWindowWidth = Math.Min(value, MaxWindowWidth);
        }

        private int maxWindowWidth = 800;
        public int MaxWindowWidth
        {
            get => maxWindowWidth;
            set => maxWindowWidth = Math.Max(value, MinWindowWidth);
        }

        public uint BackgroundColor { get; set; } = 0xFF000000;
        public int BackgroundOpacityPercentage { get; set; } = 0;
        public bool BackgroundFullWindow { get; set; } = false;

        public int TextScalePercentage { get; set; } = 100;


        public bool ShowTotalTime { get; set; } = false;
        public bool ShowProgressBar { get; set; } = true;


        public bool UseLyricsDirectory { get; set; } = false;
        public string LyricsDirectory { get; set; } = "";

        public double TimingOffsetSeconds { get; set; } = 0.0;
        public double LoopTimingOffsetSeconds { get; set; } = -0.12;

        [NonSerialized]
        private IDalamudPluginInterface? pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);
        }
    }
}
