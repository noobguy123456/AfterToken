using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// NPC 配置管理器（TbNpc 包装）。
    /// </summary>
    public class NpcConfigMgr
    {
        private static NpcConfigMgr _instance;
        public static NpcConfigMgr Instance => _instance ??= new NpcConfigMgr();

        /// <summary>
        /// 获取指定 ID 的 NPC 配置，不存在时返回 null。
        /// </summary>
        public Npc Get(int npcId)
        {
            return ConfigSystem.Instance.Tables.TbNpc.GetOrDefault(npcId);
        }
    }
}
