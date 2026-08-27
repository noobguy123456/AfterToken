using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 叙事条件表达式求值（对话/任务共用的最小领域语言，不做算术与脚本）。
    /// 词汇表（多个条件用 &amp; 连接，与关系；空串 = 恒真）：
    ///   flag:key        拥有标志位
    ///   flag:!key       没有标志位
    ///   level:&gt;=N      玩家等级 &gt;= N
    ///   quest:id:active / quest:id:done   任务状态（任务系统未落地，暂求值 false 并告警）
    /// </summary>
    public static class NarrativeCondition
    {
        public static bool Evaluate(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return true;

            foreach (var raw in expr.Split('&'))
            {
                var term = raw.Trim();
                if (term.Length == 0) continue;
                if (!EvaluateTerm(term)) return false;
            }
            return true;
        }

        private static bool EvaluateTerm(string term)
        {
            var parts = term.Split(':');
            switch (parts[0])
            {
                case "flag" when parts.Length == 2:
                    // flag:!key 取反
                    if (parts[1].StartsWith("!"))
                    {
                        return !DialogueFlagSystem.Has(parts[1].Substring(1));
                    }
                    return DialogueFlagSystem.Has(parts[1]);

                case "level" when parts.Length == 2:
                    // 仅支持 level:>=N
                    if (parts[1].StartsWith(">=") && int.TryParse(parts[1].Substring(2), out int need))
                    {
                        return PlayerProfileSystem.Level >= need;
                    }
                    Log.Warning($"[NarrativeCondition] 无法解析等级条件: {term}");
                    return false;

                case "quest":
                    // 任务系统未落地（见 docs/Proposal/narrative/quest-system.md），暂恒 false
                    Log.Warning($"[NarrativeCondition] quest 条件暂不支持（任务系统 pending）: {term}");
                    return false;

                default:
                    Log.Warning($"[NarrativeCondition] 未知条件: {term}");
                    return false;
            }
        }
    }
}
