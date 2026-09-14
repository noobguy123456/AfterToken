using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 队友好感度配置管理器（TbCompanionAffinityTier / TbCompanionAffinityGain 包装）。
    /// 档位表按累计阈值升序；来源表按 source 字符串索引。
    /// </summary>
    public class CompanionAffinityConfigMgr
    {
        private static CompanionAffinityConfigMgr _instance;
        public static CompanionAffinityConfigMgr Instance => _instance ??= new CompanionAffinityConfigMgr();

        /// <summary>按累计好感值取当前档位配置（threshold ≤ exp 的最高档）。</summary>
        public CompanionAffinityTier GetTierForExp(int exp)
        {
            var list = ConfigSystem.Instance.Tables.TbCompanionAffinityTier.DataList;
            CompanionAffinityTier best = null;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var t = list[i];
                    if (t != null && t.Threshold <= exp && (best == null || t.Threshold > best.Threshold))
                    {
                        best = t;
                    }
                }
            }
            return best;
        }

        /// <summary>取指定档位配置，不存在返回 null。</summary>
        public CompanionAffinityTier GetTier(int tier)
        {
            return ConfigSystem.Instance.Tables.TbCompanionAffinityTier.GetOrDefault(tier);
        }

        /// <summary>最高档位。</summary>
        public int MaxTier
        {
            get
            {
                int max = 1;
                var list = ConfigSystem.Instance.Tables.TbCompanionAffinityTier.DataList;
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && list[i].Tier > max) max = list[i].Tier;
                    }
                }
                return max;
            }
        }

        /// <summary>取某来源的好感增益配置（chat/extract/protect），不存在返回 null。</summary>
        public CompanionAffinityGain GetGain(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return null;
            }
            return ConfigSystem.Instance.Tables.TbCompanionAffinityGain.GetOrDefault(source);
        }
    }
}
