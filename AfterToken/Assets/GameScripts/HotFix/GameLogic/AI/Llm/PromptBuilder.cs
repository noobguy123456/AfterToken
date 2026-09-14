using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameConfig.cfg;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>战场快报里的敌人条目。</summary>
    public struct ControlEnemyInfo
    {
        public int Id;
        public Vector2 Pos;
        /// <summary>是否正在追击/攻击（威胁集合内）。</summary>
        public bool Hunting;
    }

    /// <summary>战场快报里的掉落物条目。</summary>
    public struct ControlLootInfo
    {
        public int Id;
        public Vector2 Pos;
    }

    /// <summary>一次操控决策（M5）的战场快照。</summary>
    public struct CompanionControlContext
    {
        /// <summary>safe / combat。</summary>
        public string GameState;
        public Vector2 CompanionPos;
        public int CompanionHp;
        public int CompanionMaxHp;
        public Vector2 PlayerPos;
        public int PlayerHp;
        public int PlayerMaxHp;
        public List<ControlEnemyInfo> Enemies;
        public List<ControlLootInfo> Loot;
        /// <summary>撤离点位置（无撤离点为 null）。</summary>
        public Vector2? ExtractionPos;
        /// <summary>队友当前在执行的动作名（follow/hold/ping/llm 动作等）。</summary>
        public string CurrentAction;
    }
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
    /// say 文本的输出语言跟随玩家游戏语言（LocalizationSystem.Current），防止中英混杂。
    /// </summary>
    public static class PromptBuilder
    {
        /// <summary>当前游戏语言对应的 LLM 输出语言名（写给模型看的）。</summary>
        private static string OutputLanguageName
        {
            get
            {
                var lang = LocalizationSystem.Instance != null
                    ? LocalizationSystem.Instance.Current
                    : TEngine.Language.English;
                switch (lang)
                {
                    case TEngine.Language.ChineseSimplified: return "Simplified Chinese";
                    case TEngine.Language.ChineseTraditional: return "Traditional Chinese";
                    case TEngine.Language.Japanese: return "Japanese";
                    case TEngine.Language.Korean: return "Korean";
                    default: return "English";
                }
            }
        }

        /// <summary>把输出语言指令追加到 system prompt 末尾（指令放最后权重最高）。</summary>
        private static void AppendLanguageInstruction(StringBuilder sb)
        {
            sb.Append("\nLanguage rule: write the \"say\" field entirely in ")
              .Append(OutputLanguageName)
              .Append(" (the player's selected game language). Never mix languages in one line.");
        }

        /// <summary>
        /// 好感度档位人格追加：统一人设基底（personaPrompt）之后叠加当前档位的"关系姿态"
        /// （TbCompanionPersona.promptAdd）。基底不变保证人格统一，档位只调关系温度。
        /// </summary>
        private static void AppendTierPersona(StringBuilder sb, Companion persona)
        {
            int companionId = persona != null ? persona.Id : CompanionAffinitySystem.DefaultCompanionId;
            string add = CompanionPersonaConfigMgr.Instance.GetPromptAdd(companionId, CompanionAffinitySystem.GetTier(companionId));
            if (!string.IsNullOrEmpty(add))
            {
                sb.Append('\n').Append(add);
            }
        }

        /// <summary>
        /// 聊天契约 intent 白名单（prompt 文本侧）。
        /// 执行侧裁决见 <see cref="CompanionBrain.IntentNone"/> 等常量，两处取值必须一致。
        /// </summary>
        public const string ChatIntentWhitelist = "none|follow|hold|retreat";

        /// <summary>输出契约说明（写进 system prompt，与请求体 response_format 双保险）。</summary>
        private const string ContractInstruction =
            "Respond ONLY with a JSON object, no other text: " +
            "{\"say\": \"<one short in-character line, 60 characters max>\", " +
            "\"intent\": \"" + ChatIntentWhitelist + "\", " +
            "\"mood\": \"calm|tense|hurt\"}. " +
            "Use intent \"none\" unless the situation clearly calls for a movement suggestion.";

        private const string WorldSummary =
            "World: After the 'Token' fell, scavengers run extraction raids into ruined sectors, " +
            "then return to a fortified base between runs.";

        /// <summary>
        /// Few-shot 示例：教会模型快照格式与契约回答的一一对应，同时固化人格语气。
        /// 示例快照字段顺序与 <see cref="BuildUser"/> 输出完全一致。
        /// </summary>
        private const string FewShotExamples =
            "Examples of how to respond:\n" +
            "Input: state: safe; trigger: safe_idle; player_hp: 100/100; your_hp: 200/200; threats: 0\n" +
            "Output: {\"say\": \"All quiet, boss. Enjoy it while it lasts.\", \"intent\": \"none\", \"mood\": \"calm\"}\n" +
            "Input: state: safe; trigger: safe_idle; player_hp: 100/100; your_hp: 200/200; threats: 0\n" +
            "Output: {\"say\": \"Eleven sectors, boss. We're still breathing.\", \"intent\": \"none\", \"mood\": \"calm\"}\n" +
            "Input: state: combat; trigger: combat_start; player_hp: 100/100; your_hp: 200/200; threats: 3\n" +
            "Output: {\"say\": \"Contacts. Lovely.\", \"intent\": \"none\", \"mood\": \"tense\"}\n" +
            "Input: state: combat; trigger: player_low_hp; player_hp: 22/100; your_hp: 200/200; threats: 2\n" +
            "Output: {\"say\": \"You're bleeding, boss. Fall back!\", \"intent\": \"retreat\", \"mood\": \"tense\"}\n" +
            "Input: state: combat; trigger: self_low_hp; player_hp: 80/100; your_hp: 40/200; threats: 4\n" +
            "Output: {\"say\": \"I'm shot up bad. Pulling back.\", \"intent\": \"retreat\", \"mood\": \"hurt\"}\n" +
            "Input: state: safe; trigger: link_recovered; player_hp: 100/100; your_hp: 200/200; threats: 0\n" +
            "Output: {\"say\": \"Signal's back. Miss me?\", \"intent\": \"follow\", \"mood\": \"calm\"}\n" +
            "Input: state: combat; trigger: threat_cleared; player_hp: 90/100; your_hp: 180/200; threats: 0\n" +
            "Output: {\"say\": \"Hostile down. Area's clean.\", \"intent\": \"none\", \"mood\": \"calm\"}\n" +
            "Input: state: safe; trigger: safe_idle; player_hp: 55/100; your_hp: 200/200; threats: 0\n" +
            "Output: {\"say\": \"Patch yourself up, boss. Doctor's orders.\", \"intent\": \"none\", \"mood\": \"calm\"}";

        public static string BuildSystem(Companion persona)
        {
            var sb = new StringBuilder(2048);
            sb.Append(persona?.PersonaPrompt ?? "You are a calm battlefield companion.");
            AppendTierPersona(sb, persona);
            sb.Append('\n');
            sb.Append(WorldSummary);
            sb.Append('\n');
            sb.Append(ContractInstruction);
            sb.Append('\n');
            sb.Append(FewShotExamples);
            AppendLanguageInstruction(sb);
            return sb.ToString();
        }

        public static string BuildUser(in CompanionPromptContext ctx)
        {
            var sb = new StringBuilder(256);
            sb.Append("state: ").Append(ctx.GameState);
            sb.Append("; trigger: ").Append(ctx.Trigger);
            if (ctx.PlayerMaxHp > 0)
            {
                sb.Append("; player_hp: ").Append(ctx.PlayerHp).Append('/').Append(ctx.PlayerMaxHp);
            }
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

        // ── M5 操控通道：战场快报 + 动作契约 ──

        /// <summary>操控契约说明（action/target 白名单，防瞎编）。</summary>
        private const string ControlContractInstruction =
            "You are deciding your NEXT ACTION in the field. Respond ONLY with a JSON object: " +
            "{\"action\": \"" + DecisionDriver.ControlActionWhitelist + "\", " +
            "\"target\": \"<enemy id for engage | loot id for loot | \\\"x,z\\\" for move_to | empty string otherwise>\", " +
            "\"say\": \"<one short in-character line, 60 characters max>\", " +
            "\"mood\": \"calm|tense|hurt\"}. " +
            "Rules: never invent ids that are not in the report; " +
            "pick \"none\" to keep doing what you are doing; " +
            "protect the player first, but do not suicide into a bigger group.";

        private const string ControlFewShot =
            "Examples:\n" +
            "Report: state: combat\nyou: hp 200/200 at (12.5, 3.2)\nplayer: hp 65/100 at (14.0, 4.1)\nenemies: #301 at (18.0, 6.0) hunting; #305 at (8.0, 1.0) idle\ncurrent_action: follow\n" +
            "Output: {\"action\": \"engage\", \"target\": \"301\", \"say\": \"Contact closing on you, boss. On it.\", \"mood\": \"tense\"}\n" +
            "Report: state: safe\nyou: hp 200/200 at (12.5, 3.2)\nplayer: hp 100/100 at (14.0, 4.1)\nloot: #77 at (10.0, 2.0)\nextraction: (18.0, 18.0)\ncurrent_action: follow\n" +
            "Output: {\"action\": \"loot\", \"target\": \"77\", \"say\": \"Supplies on the ground. Grabbing them.\", \"mood\": \"calm\"}\n" +
            "Report: state: combat\nyou: hp 40/200 at (12.5, 3.2)\nplayer: hp 90/100 at (14.0, 4.1)\nenemies: #301 at (13.0, 3.5) hunting; #302 at (12.0, 3.8) hunting; #303 at (13.5, 3.0) hunting\ncurrent_action: engage\n" +
            "Output: {\"action\": \"retreat\", \"target\": \"\", \"say\": \"Three on me. Falling back!\", \"mood\": \"hurt\"}";

        /// <summary>操控通道 system prompt：人设 + 世界观 + 动作契约 + 示例。</summary>
        public static string BuildControlSystem(Companion persona)
        {
            var sb = new StringBuilder(2048);
            sb.Append(persona?.PersonaPrompt ?? "You are a calm battlefield companion.");
            AppendTierPersona(sb, persona);
            sb.Append('\n');
            sb.Append(WorldSummary);
            sb.Append('\n');
            sb.Append(ControlContractInstruction);
            sb.Append('\n');
            sb.Append(ControlFewShot);
            AppendLanguageInstruction(sb);
            return sb.ToString();
        }

        /// <summary>战场快报（user prompt）。数字一律不变区域性格式，供 target 解析复用。</summary>
        public static string BuildControlUser(in CompanionControlContext ctx)
        {
            var sb = new StringBuilder(384);
            sb.Append("state: ").Append(ctx.GameState).Append('\n');
            sb.Append("you: hp ").Append(ctx.CompanionHp).Append('/').Append(ctx.CompanionMaxHp)
              .Append(" at ").Append(FormatPos(ctx.CompanionPos)).Append('\n');
            sb.Append("player: hp ").Append(ctx.PlayerHp).Append('/').Append(ctx.PlayerMaxHp)
              .Append(" at ").Append(FormatPos(ctx.PlayerPos)).Append('\n');

            if (ctx.Enemies != null && ctx.Enemies.Count > 0)
            {
                sb.Append("enemies: ");
                for (int i = 0; i < ctx.Enemies.Count; i++)
                {
                    if (i > 0) sb.Append("; ");
                    var e = ctx.Enemies[i];
                    sb.Append('#').Append(e.Id).Append(" at ").Append(FormatPos(e.Pos))
                      .Append(e.Hunting ? " hunting" : " idle");
                }
                sb.Append('\n');
            }
            if (ctx.Loot != null && ctx.Loot.Count > 0)
            {
                sb.Append("loot: ");
                for (int i = 0; i < ctx.Loot.Count; i++)
                {
                    if (i > 0) sb.Append("; ");
                    sb.Append('#').Append(ctx.Loot[i].Id).Append(" at ").Append(FormatPos(ctx.Loot[i].Pos));
                }
                sb.Append('\n');
            }
            if (ctx.ExtractionPos.HasValue)
            {
                sb.Append("extraction: ").Append(FormatPos(ctx.ExtractionPos.Value)).Append('\n');
            }
            sb.Append("current_action: ").Append(ctx.CurrentAction ?? "follow").Append('\n');
            sb.Append("actions: ").Append(DecisionDriver.ControlActionWhitelist.Replace('|', '/'));
            return sb.ToString();
        }

        private static string FormatPos(Vector2 pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.0}, {1:0.0})", pos.x, pos.y);
        }

        // ── M6 自由对话通道：玩家直接对队友说话 ──

        /// <summary>
        /// 对话契约说明（复用 say/intent/mood，intent 只接移动类指令；
        /// memorable/memoryType 用于 LLM 打标记忆筛选，recall 用于两阶段记忆召回）。
        /// </summary>
        private const string ChatContractInstruction =
            "The player (your boss) is speaking directly to you over the radio. " +
            "Respond ONLY with a JSON object, no other text: " +
            "{\"say\": \"<your reply, in character, 80 characters max>\", " +
            "\"intent\": \"" + ChatIntentWhitelist + "\", " +
            "\"mood\": \"calm|tense|hurt\", " +
            "\"memorable\": <true if the boss's line reveals a personal fact, preference, or an opinion about you worth remembering; else false>, " +
            "\"memoryType\": \"chat_player|chat_about_ai|none\", " +
            "\"recall\": [<memory types you want to look up before answering, from \"gift\"|\"chat_player\"|\"chat_about_ai\"; empty array if none>]}. " +
            "Rules: actually answer what the boss asked or react to what they said; " +
            "use intent \"follow\" or \"hold\" only if the boss clearly told you to move with them or stay put; " +
            "otherwise intent is \"none\"; " +
            "memoryType is \"chat_about_ai\" when the boss evaluates or describes YOU, \"chat_player\" for facts about the boss, else \"none\"; " +
            "only request recall when the conversation genuinely touches past gifts or past talks.";

        /// <summary>召回二阶段契约：记忆已注入，禁止再次请求 recall（防循环）。</summary>
        private const string ChatRecallPhase2Instruction =
            "Your memories requested earlier are listed above under \"You recall\". " +
            "Answer using them naturally. In this response the \"recall\" field MUST be an empty array.";

        private const string ChatFewShot =
            "Examples:\n" +
            "boss says: \"how are you holding up?\"\n" +
            "Output: {\"say\": \"Still breathing, boss. That's the whole job.\", \"intent\": \"none\", \"mood\": \"calm\", \"memorable\": false, \"memoryType\": \"none\", \"recall\": []}\n" +
            "boss says: \"stay here and watch the door\"\n" +
            "Output: {\"say\": \"Holding this spot. Yell if it gets loud.\", \"intent\": \"hold\", \"mood\": \"calm\", \"memorable\": false, \"memoryType\": \"none\", \"recall\": []}\n" +
            "boss says: \"stick with me\"\n" +
            "Output: {\"say\": \"On your six, boss.\", \"intent\": \"follow\", \"mood\": \"calm\", \"memorable\": false, \"memoryType\": \"none\", \"recall\": []}\n" +
            "boss says: \"I grew up in sector nine, you know\"\n" +
            "Output: {\"say\": \"Sector nine? Rough soil, boss. Explains a lot.\", \"intent\": \"none\", \"mood\": \"calm\", \"memorable\": true, \"memoryType\": \"chat_player\", \"recall\": []}\n" +
            "boss says: \"did you like the gift I gave you?\"\n" +
            "Output: {\"say\": \"Gifts? Let me think...\", \"intent\": \"none\", \"mood\": \"calm\", \"memorable\": false, \"memoryType\": \"none\", \"recall\": [\"gift\"]}";

        /// <summary>自由对话 system prompt：人设 + 档位姿态 + 世界观 + 对话契约 + 示例。</summary>
        public static string BuildChatSystem(Companion persona)
        {
            var sb = new StringBuilder(2048);
            sb.Append(persona?.PersonaPrompt ?? "You are a calm battlefield companion.");
            AppendTierPersona(sb, persona);
            sb.Append('\n');
            sb.Append(WorldSummary);
            sb.Append('\n');
            sb.Append(ChatContractInstruction);
            sb.Append('\n');
            sb.Append(ChatFewShot);
            AppendLanguageInstruction(sb);
            return sb.ToString();
        }

        /// <summary>
        /// 召回二阶段 system prompt：在 BuildChatSystem 基础上注入召回的记忆段落 + 禁再次召回指令。
        /// memories 为空时注入"无相关记忆"说明，让模型自然带过。
        /// </summary>
        public static string BuildChatSystemWithRecall(Companion persona, List<CompanionMemoryEntry> memories)
        {
            var sb = new StringBuilder(2560);
            sb.Append(persona?.PersonaPrompt ?? "You are a calm battlefield companion.");
            AppendTierPersona(sb, persona);
            sb.Append('\n');
            sb.Append(WorldSummary);
            sb.Append('\n');
            if (memories != null && memories.Count > 0)
            {
                sb.Append("You recall:\n");
                for (int i = 0; i < memories.Count; i++)
                {
                    sb.Append("- ").Append(CompanionMemorySystem.FormatForPrompt(memories[i])).Append('\n');
                }
            }
            else
            {
                sb.Append("You searched your memory but found nothing relevant.\n");
            }
            sb.Append(ChatRecallPhase2Instruction);
            sb.Append('\n');
            sb.Append(ChatContractInstruction);
            sb.Append('\n');
            sb.Append(ChatFewShot);
            AppendLanguageInstruction(sb);
            return sb.ToString();
        }

        /// <summary>自由对话 user prompt：状态快照 + 最近对话历史 + 玩家这句话。</summary>
        public static string BuildChatUser(in CompanionPromptContext ctx, string playerText, List<string> history)
        {
            var sb = new StringBuilder(384);
            sb.Append("state: ").Append(ctx.GameState);
            if (ctx.PlayerMaxHp > 0)
            {
                sb.Append("; player_hp: ").Append(ctx.PlayerHp).Append('/').Append(ctx.PlayerMaxHp);
            }
            sb.Append("; your_hp: ").Append(ctx.CompanionHp).Append('/').Append(ctx.CompanionMaxHp);
            sb.Append("; threats: ").Append(ctx.ThreatCount).Append('\n');
            if (history != null && history.Count > 0)
            {
                sb.Append("recent conversation:\n");
                for (int i = 0; i < history.Count; i++)
                {
                    sb.Append(history[i]).Append('\n');
                }
            }
            sb.Append("boss says: \"").Append(playerText).Append('"');
            return sb.ToString();
        }
    }
}
