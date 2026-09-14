using GameConfig.cfg;

namespace GameLogic
{
    /// <summary>
    /// 队友人格分档配置管理器（TbCompanionPersona 包装）。
    /// 档位 promptAdd 叠加在 companion.xlsx 的统一人设基底之上，由 PromptBuilder 注入。
    /// </summary>
    public class CompanionPersonaConfigMgr
    {
        private static CompanionPersonaConfigMgr _instance;
        public static CompanionPersonaConfigMgr Instance => _instance ??= new CompanionPersonaConfigMgr();

        /// <summary>
        /// 取队友在指定档位的人格追加文本；该档缺失时逐档向下回退，全缺返回空串。
        /// </summary>
        public string GetPromptAdd(int companionId, int tier)
        {
            for (int t = tier; t >= 1; t--)
            {
                var cfg = ConfigSystem.Instance.Tables.TbCompanionPersona.GetOrDefault(companionId * 10 + t);
                if (cfg != null && !string.IsNullOrEmpty(cfg.PromptAdd))
                {
                    return cfg.PromptAdd;
                }
            }
            return string.Empty;
        }
    }
}
