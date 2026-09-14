using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 队友配置管理器（TbCompanion / TbCompanionBark 包装）。
    /// M1 期 MVP 只有一名队友（id=1 Exusiai），多队友预留。
    /// </summary>
    public class CompanionConfigMgr
    {
        private static CompanionConfigMgr _instance;
        public static CompanionConfigMgr Instance => _instance ??= new CompanionConfigMgr();

        /// <summary>MVP 唯一队友 ID。</summary>
        public const int DefaultCompanionId = 1;

        /// <summary>台词按 trigger 分组的懒建缓存；_cacheSource 引用变化（GM reload 换表）时自动重建。</summary>
        private Dictionary<string, List<CompanionBark>> _barkIndex;
        private System.Collections.Generic.IReadOnlyList<CompanionBark> _cacheSource;

        /// <summary>获取队友人设卡，不存在时返回 null。</summary>
        public Companion Get(int companionId = DefaultCompanionId)
        {
            return ConfigSystem.Instance.Tables.TbCompanion.GetOrDefault(companionId);
        }

        /// <summary>按触发键取该键全部台词（权重随机由调用方做）。</summary>
        public List<CompanionBark> GetBarks(string trigger)
        {
            if (string.IsNullOrEmpty(trigger))
            {
                return null;
            }

            var dataList = ConfigSystem.Instance.Tables.TbCompanionBark.DataList;
            if (_barkIndex == null || !ReferenceEquals(_cacheSource, dataList))
            {
                RebuildIndex(dataList);
            }

            return _barkIndex.TryGetValue(trigger, out var result) ? result : null;
        }

        private void RebuildIndex(System.Collections.Generic.IReadOnlyList<CompanionBark> dataList)
        {
            _cacheSource = dataList;
            _barkIndex = new Dictionary<string, List<CompanionBark>>(16);
            if (dataList == null)
            {
                return;
            }

            for (int i = 0; i < dataList.Count; i++)
            {
                var bark = dataList[i];
                if (bark == null || string.IsNullOrEmpty(bark.Trigger))
                {
                    continue;
                }

                if (!_barkIndex.TryGetValue(bark.Trigger, out var list))
                {
                    list = new List<CompanionBark>(4);
                    _barkIndex[bark.Trigger] = list;
                }
                list.Add(bark);
            }
        }

        /// <summary>按权重随机抽一条台词，无匹配返回 null。只抽当前好感档位解锁的台词（minTier ≤ 当前档位，池子累计扩大）。</summary>
        public CompanionBark PickBark(string trigger)
        {
            var barks = GetBarks(trigger);
            if (barks == null || barks.Count == 0)
            {
                return null;
            }

            int tier = CompanionAffinitySystem.GetTier();
            int totalWeight = 0;
            for (int i = 0; i < barks.Count; i++)
            {
                if (barks[i].MinTier <= tier)
                {
                    totalWeight += barks[i].Weight > 0 ? barks[i].Weight : 1;
                }
            }
            if (totalWeight <= 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            CompanionBark last = null;
            for (int i = 0; i < barks.Count; i++)
            {
                if (barks[i].MinTier > tier)
                {
                    continue;
                }
                last = barks[i];
                roll -= barks[i].Weight > 0 ? barks[i].Weight : 1;
                if (roll < 0)
                {
                    return barks[i];
                }
            }
            return last;
        }
    }
}
