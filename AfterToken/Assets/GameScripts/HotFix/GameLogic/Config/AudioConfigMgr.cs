using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 音频配置管理器（TbAudio 包装）。地址 = YooAsset 文件名寻址（AssetRaw/Audios 下勿重名）。
    /// </summary>
    public class AudioConfigMgr
    {
        private static AudioConfigMgr _instance;
        public static AudioConfigMgr Instance => _instance ??= new AudioConfigMgr();

        /// <summary>按地址（文件名）取音频条目，不存在返回 null。</summary>
        public Audio GetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            var list = ConfigSystem.Instance.Tables.TbAudio.DataList;
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Name == name)
                {
                    return list[i];
                }
            }
            return null;
        }

        /// <summary>按场景键取 BGM 条目（MainMenu/Simulation/Battle/Combat），不存在返回 null。</summary>
        public Audio GetSceneBgm(string sceneKey)
        {
            if (string.IsNullOrEmpty(sceneKey))
            {
                return null;
            }

            var list = ConfigSystem.Instance.Tables.TbAudio.DataList;
            if (list == null)
            {
                return null;
            }

            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a != null && a.AudioType == "bgm" && a.SceneKey == sceneKey)
                {
                    return a;
                }
            }
            return null;
        }
    }
}
