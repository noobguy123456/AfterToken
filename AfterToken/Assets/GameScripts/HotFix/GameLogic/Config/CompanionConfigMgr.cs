using System.Collections.Generic;
using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 队友配置管理器（TbCompanion / TbCompanionBark 包装）。
    /// M1 期 MVP 只有一名队友（id=1 Rook），多队友预留。
    /// </summary>
    public class CompanionConfigMgr
    {
        private static CompanionConfigMgr _instance;
        public static CompanionConfigMgr Instance => _instance ??= new CompanionConfigMgr();

        /// <summary>MVP 唯一队友 ID。</summary>
        public const int DefaultCompanionId = 1;

        private System.Collections.Generic.IReadOnlyList<CompanionBark> _barkCache;

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

            _barkCache ??= ConfigSystem.Instance.Tables.TbCompanionBark.DataList;

            List<CompanionBark> result = null;
            for (int i = 0; i < _barkCache.Count; i++)
            {
                var bark = _barkCache[i];
                if (bark.Trigger == trigger)
                {
                    result ??= new List<CompanionBark>(4);
                    result.Add(bark);
                }
            }
            return result;
        }

        /// <summary>按权重随机抽一条台词，无匹配返回 null。</summary>
        public CompanionBark PickBark(string trigger)
        {
            var barks = GetBarks(trigger);
            if (barks == null || barks.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < barks.Count; i++)
            {
                totalWeight += barks[i].Weight > 0 ? barks[i].Weight : 1;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < barks.Count; i++)
            {
                roll -= barks[i].Weight > 0 ? barks[i].Weight : 1;
                if (roll < 0)
                {
                    return barks[i];
                }
            }
            return barks[barks.Count - 1];
        }
    }
}
