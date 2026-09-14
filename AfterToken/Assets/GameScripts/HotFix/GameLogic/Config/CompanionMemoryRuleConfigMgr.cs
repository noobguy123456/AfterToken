using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 队友记忆规则配置管理器（TbCompanionMemoryRule 包装）。
    /// 解耦核心：什么档位能记什么类型、容量与淘汰规则全部走表，代码只执行规则。
    /// </summary>
    public class CompanionMemoryRuleConfigMgr
    {
        private static CompanionMemoryRuleConfigMgr _instance;
        public static CompanionMemoryRuleConfigMgr Instance => _instance ??= new CompanionMemoryRuleConfigMgr();

        /// <summary>记忆类型常量（与表 memoryType 列一致）。</summary>
        public const string TypeGift = "gift";
        public const string TypeChatPlayer = "chat_player";
        public const string TypeChatAboutAi = "chat_about_ai";
        public const string TypeBattleEvent = "battle_event";

        /// <summary>淘汰规则常量（与表 sampleRule 列一致）。</summary>
        public const string RuleFifo = "fifo";
        public const string RuleRandomForget = "random_forget";
        public const string RuleRandomEvict = "random_evict";

        /// <summary>
        /// 取某记忆类型在指定档位生效的规则（minTier ≤ tier 的最高 minTier 行）。
        /// 返回 null = 该档位不可记录此类型。
        /// </summary>
        public CompanionMemoryRule GetRule(string memoryType, int tier)
        {
            if (string.IsNullOrEmpty(memoryType))
            {
                return null;
            }

            var list = ConfigSystem.Instance.Tables.TbCompanionMemoryRule.DataList;
            CompanionMemoryRule best = null;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var r = list[i];
                    if (r != null && r.MemoryType == memoryType && r.MinTier <= tier
                        && (best == null || r.MinTier > best.MinTier))
                    {
                        best = r;
                    }
                }
            }
            return best;
        }
    }
}
