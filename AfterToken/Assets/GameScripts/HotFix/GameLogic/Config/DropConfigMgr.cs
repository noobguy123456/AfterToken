using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 掉落配置管理器。
    /// </summary>
    public class DropConfigMgr
    {
        private static DropConfigMgr _instance;
        public static DropConfigMgr Instance => _instance ??= new DropConfigMgr();

        private readonly List<Drop> _resultCache = new List<Drop>();

        /// <summary>
        /// 获取指定敌人的全部掉落配置（无掉落时返回空列表）。
        /// 返回的内部列表请勿缓存或修改。
        /// </summary>
        public List<Drop> GetDropsForEnemy(int enemyId)
        {
            _resultCache.Clear();
            var table = ConfigSystem.Instance.Tables.TbDrop;
            foreach (var drop in table.DataList)
            {
                if (drop.EnemyId == enemyId)
                {
                    _resultCache.Add(drop);
                }
            }
            return _resultCache;
        }
    }
}
