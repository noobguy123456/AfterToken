using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 叙事动作执行（与 <see cref="NarrativeCondition"/> 配套的最小领域语言）。
    /// 词汇表（多个动作用 &amp; 连接；空串 = 无动作）：
    ///   flag:+key / flag:-key   写/清标志位
    ///   give:gold:N             发金币
    ///   quest:accept:id         弹接取确认窗（QuestAcceptConfirmUI），玩家确认后才真正接取；条件不满足时跳过并告警
    ///   quest:turnin:id         交付任务（未达可交付状态时跳过并告警）
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

                case "quest" when parts.Length == 3 && int.TryParse(parts[2], out int questId):
                    switch (parts[1])
                    {
                        case "accept":
                            // 不直接接取：弹确认窗，玩家点 Confirm 才走 QuestSystem.Accept
                            if (!QuestSystem.CanAccept(questId))
                            {
                                Log.Warning($"[NarrativeAction] 任务不可接取（条件不满足或已接取）: {term}");
                            }
                            else if (!GameModule.UI.HasWindow<QuestAcceptConfirmUI>())
                            {
                                GameModule.UI.ShowUIAsync<QuestAcceptConfirmUI>(questId);
                            }
                            break;
                        case "turnin":
                            if (!QuestSystem.TryTurnIn(questId))
                            {
                                Log.Warning($"[NarrativeAction] 任务交付失败（未达可交付状态）: {term}");
                            }
                            break;
                        default:
                            Log.Warning($"[NarrativeAction] 未知任务动作（应为 quest:accept/turnin:id）: {term}");
                            break;
                    }
                    break;

                case "quest":
                    Log.Warning($"[NarrativeAction] 无法解析任务动作（应为 quest:accept/turnin:id）: {term}");
                    break;

                default:
                    Log.Warning($"[NarrativeAction] 未知动作: {term}");
                    break;
            }
        }
    }
}
