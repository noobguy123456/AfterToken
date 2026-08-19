using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 小纸条配置管理器（TbNote 包装）。
    /// </summary>
    public class NoteConfigMgr
    {
        private static NoteConfigMgr _instance;
        public static NoteConfigMgr Instance => _instance ??= new NoteConfigMgr();

        /// <summary>
        /// 获取指定 ID 的纸条配置，不存在时返回 null。
        /// </summary>
        public Note Get(int noteId)
        {
            return ConfigSystem.Instance.Tables.TbNote.GetOrDefault(noteId);
        }
    }
}
