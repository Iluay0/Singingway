using FFXIVClientStructs.FFXIV.Client.Game;

namespace Singingway.Utils
{
    public unsafe class BgmHelper
    {
        public ushort GetActiveBgmId()
        {
            BGMSystem* bgmSystem = BGMSystem.Instance();

            if (bgmSystem == null)
                return 0;

            var scenes = bgmSystem->Scenes.AsSpan();

            for (int i = 0; i < bgmSystem->NumScenes; i++)
            {
                var scene = scenes[i];

                if (scene.PlayingBgmId != 0)
                {
                    return scene.PlayingBgmId;
                }
            }

            return 0;
        }
    }
}
