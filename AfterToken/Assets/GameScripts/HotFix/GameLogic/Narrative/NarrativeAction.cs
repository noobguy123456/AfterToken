using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 叙事动作执行（与 <see cref="NarrativeCondition"/> 配套的最小领域语言）。
    /// 词汇表（多个动作用 &amp; 连接；空串 = 无动作）：
    ///   flag:+key / flag:-key   写/清标志位
    ///   give:gold:N             发金币
    ///   quest:accept:id / quest:turnin:id   任务接取/交付（任务系统未落地，暂告警跳过）
    /// </summary>
    public static class NarrativeAction
    {
        public static void Execute(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return;

            foreach (var raw in expr.Split('&'))
            {
                var term = raw.Trim();
                if (term.Length == 0) continue;
                ExecuteTerm(term);
            }
        }

        private static void ExecuteTerm(string term)
        {
            var parts = term.Split(':');
            switch (parts[0])
            {
                case "flag" when parts.Length == 2:
                    if (parts[1].StartsWith("+"))
                    {
                        DialogueFlagSystem.Set(parts[1].Substring(1));
                    }
                    else if (parts[1].StartsWith("-"))
                    {
                        DialogueFlagSystem.Clear(parts[1].Substring(1));
                    }
                    else
                    {
                        Log.Warning($"[NarrativeAction] 无法解析标志位动作: {term}");
                    }
                    break;

                case "give" when parts.Length == 3:
                    if (parts[1] == "gold" && int.TryParse(parts[2], out int gold))
                    {
                        CurrencySystem.AddGold(gold);
                    }
                    else
                    {
                        Log.Warning($"[NarrativeAction] 无法解析发放动作: {term}");
                    }
                    break;

                case "quest":
                    // 任务系统未落地（见 docs/Proposal/narrative/quest-system.md），暂跳过
                    Log.Warning($"[NarrativeAction] quest 动作暂不支持（任务系统 pending）: {term}");
                    break;

                default:
                    Log.Warning($"[NarrativeAction] 未知动作: {term}");
                    break;
            }
        }
    }
}
