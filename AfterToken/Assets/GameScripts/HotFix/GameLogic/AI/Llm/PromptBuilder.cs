using System.Collections.Generic;
using System.Text;
using GameConfig.cfg;

namespace GameLogic.AI.Llm
{
    /// <summary>一次 LLM 请求的上下文快照（值语义，组装 prompt 前的采集结果）。</summary>
    public struct CompanionPromptContext
    {
        /// <summary>safe / combat。</summary>
        public string GameState;
        /// <summary>触发事件键（safe_idle 等）。</summary>
        public string Trigger;
        public int PlayerHp;
        public int PlayerMaxHp;
        public int CompanionHp;
        public int CompanionMaxHp;
        public int ThreatCount;
        /// <summary>最近已说台词（防复读）。</summary>
        public List<string> RecentLines;
    }

    /// <summary>
    /// Prompt 组装：人设卡 + 世界观摘要 + 输出契约（system），上下文快照（user）。
    /// 不知道 HTTP 细节，也不知道 FSM 存在。
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>输出契约说明（写进 system prompt，与请求体 response_format 双保险）。</summary>
        private const string ContractInstruction =
            "Respond ONLY with a JSON object, no other text: " +
            "{\"say\": \"<one short in-character line, 60 characters max, English>\", " +
            "\"intent\": \"none|follow|hold|retreat\", " +
            "\"mood\": \"calm|tense|hurt\"}. " +
            "Use intent \"none\" unless the situation clearly calls for a movement suggestion.";

        private const string WorldSummary =
            "World: After the 'Token' fell, scavengers run extraction raids into ruined sectors, " +
            "then return to a fortified base between runs.";

        public static string BuildSystem(Companion persona)
        {
            var sb = new StringBuilder(512);
            sb.Append(persona?.PersonaPrompt ?? "You are a calm battlefield companion.");
            sb.Append('\n');
            sb.Append(WorldSummary);
            sb.Append('\n');
            sb.Append(ContractInstruction);
            return sb.ToString();
        }

        public static string BuildUser(in CompanionPromptContext ctx)
        {
            var sb = new StringBuilder(256);
            sb.Append("state: ").Append(ctx.GameState);
            sb.Append("; trigger: ").Append(ctx.Trigger);
            sb.Append("; player_hp: ").Append(ctx.PlayerHp).Append('/').Append(ctx.PlayerMaxHp);
            sb.Append("; your_hp: ").Append(ctx.CompanionHp).Append('/').Append(ctx.CompanionMaxHp);
            sb.Append("; threats: ").Append(ctx.ThreatCount);
            if (ctx.RecentLines != null && ctx.RecentLines.Count > 0)
            {
                sb.Append("; do NOT repeat these recent lines: ");
                for (int i = 0; i < ctx.RecentLines.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append('"').Append(ctx.RecentLines[i]).Append('"');
                }
            }
            return sb.ToString();
        }
    }
}
