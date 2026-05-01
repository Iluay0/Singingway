using Dalamud.Bindings.ImGui;
using Singingway.Utils;
using Singingway.Windows.UiHelpers;
using System;

namespace Singingway.Windows.Config.Tabs;
internal class VolumeSettingsTab : UiTab
{
    public override string Name => "Volume Settings";

    public override void Initialize()
    {
    }

    public override void Draw()
    {
        bool enable = Service.Configuration.EnableVolumeOverrides;
        if (ImGui.Checkbox("Enable volume override during lyrics", ref enable))
        {
            Service.Configuration.EnableVolumeOverrides = enable;
            Service.Configuration.Save();
            OnConfigChanged?.Invoke();
        }

        ImGui.TextWrapped("When lyrics are playing, the selected channel volumes will be applied. Previous game volumes are restored when lyrics stop.");
        ImGui.Separator();

        ImGui.BeginDisabled(!enable);

        Service.Configuration.VolumeSoundMaster = DrawVolumeSlider("Master Volume", Service.Configuration.VolumeSoundMaster);
        Service.Configuration.VolumeSoundBgm = DrawVolumeSlider("BGM Volume", Service.Configuration.VolumeSoundBgm);
        Service.Configuration.VolumeSoundSe = DrawVolumeSlider("SFX Volume", Service.Configuration.VolumeSoundSe);
        Service.Configuration.VolumeSoundVoice = DrawVolumeSlider("Voice Volume", Service.Configuration.VolumeSoundVoice);
        Service.Configuration.VolumeSoundEnv = DrawVolumeSlider("Ambient Volume", Service.Configuration.VolumeSoundEnv);
        Service.Configuration.VolumeSoundSystem = DrawVolumeSlider("System Volume", Service.Configuration.VolumeSoundSystem);
        Service.Configuration.VolumeSoundPerform = DrawVolumeSlider("Performance Volume", Service.Configuration.VolumeSoundPerform);

        ImGui.EndDisabled();
    }

    private uint DrawVolumeSlider(string label, uint value)
    {
        int currentValue = (int)value;
        if (ImGui.SliderInt(label, ref currentValue, 0, 100))
        {
            uint updatedValue = (uint)currentValue;
            if (updatedValue != value)
            {
                Service.Configuration.Save();
                OnConfigChanged?.Invoke();
            }
            return updatedValue;
        }

        return value;
    }
}
