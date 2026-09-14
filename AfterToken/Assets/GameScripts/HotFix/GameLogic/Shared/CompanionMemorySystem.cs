using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友记忆系统：记忆内容与记忆功能解耦——能否记/记多少/怎么淘汰全走
    /// TbCompanionMemoryRule（按当前好感档位取生效规则行），代码只执行规则。
    /// content 只存事实文本（礼物=物品名×数量；聊天=玩家原句），不存 LLM 生成文本。
    /// 持久化由 SaveSystem 接管（变动即存）。记忆默认不进 LLM prompt，
    /// 仅召回链路（PromptBuilder.AppendRecalledMemories）主动读取。
    /// </summary>
    public static class CompanionMemorySystem
    {
        /// <summary>companionId → 记忆列表（按写入时间升序）。</summary>
        private static readonly Dictionary<int, List<CompanionMemoryEntry>> _memories
            = new Dictionary<int, List<CompanionMemoryEntry>>();
        private static bool _loaded;

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.companionMemory;
            if (!d.initialized) return;

            _memories.Clear();
            foreach (var b in d.buckets)
            {
                if (b != null && b.memories != null)
                {
                    _memories[b.companionId] = new List<CompanionMemoryEntry>(b.memories);
                }
            }
        }

        private static void Persist()
        {
            var d = SaveSystem.Data.companionMemory;
            d.initialized = true;
            d.buckets.Clear();
            foreach (var kv in _memories)
            {
                d.buckets.Add(new CompanionMemoryBucket
                {
                    companionId = kv.Key,
                    memories = new List<CompanionMemoryEntry>(kv.Value),
                });
            }
            SaveSystem.Flush();
        }

        /// <summary>
        /// 尝试写入一条记忆。规则由当前档位决定：
        /// 该档位不可记此类型 → 返回 false；writeChance 判定失败 → 返回 false（随机遗忘）；
        /// 容量满按 sampleRule 淘汰（fifo 去最旧 / random_evict 随机踢 / random_forget 丢弃新条目）。
        /// 注意：无论写入成败，调用方才决定要不要显示"好奇"提示（不暴露判定结果）。
        /// </summary>
        public static bool TryRecord(int companionId, string memoryType, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            EnsureLoaded();
            int tier = CompanionAffinitySystem.GetTier(companionId);
            var rule = CompanionMemoryRuleConfigMgr.Instance.GetRule(memoryType, tier);
            if (rule == null)
            {
                return false;
            }

            if (rule.WriteChance < 1f && Random.value > rule.WriteChance)
            {
                return false;
            }

            if (!_memories.TryGetValue(companionId, out var list))
            {
                list = new List<CompanionMemoryEntry>();
                _memories[companionId] = list;
            }

            // 容量检查与淘汰
            int typeCount = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == memoryType) typeCount++;
            }

            if (typeCount >= rule.MaxCount)
            {
                switch (rule.SampleRule)
                {
                    case CompanionMemoryRuleConfigMgr.RuleRandomForget:
                        return false;
                    case CompanionMemoryRuleConfigMgr.RuleRandomEvict:
                        RemoveRandomOfType(list, memoryType);
                        break;
                    default: // fifo
                        RemoveOldestOfType(list, memoryType);
                        break;
                }
            }

            list.Add(new CompanionMemoryEntry
            {
                type = memoryType,
                content = content,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            });
            Persist();
            return true;
        }

        /// <summary>取某队友某类型的全部记忆（召回注入与信息面板用）。</summary>
        public static List<CompanionMemoryEntry> GetMemories(int companionId, string memoryType)
        {
            EnsureLoaded();
            var result = new List<CompanionMemoryEntry>();
            if (_memories.TryGetValue(companionId, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].type == memoryType)
                    {
                        result.Add(list[i]);
                    }
                }
            }
            return result;
        }

        /// <summary>取某队友全部记忆（信息面板用）。</summary>
        public static IReadOnlyList<CompanionMemoryEntry> GetAllMemories(int companionId)
        {
            EnsureLoaded();
            return _memories.TryGetValue(companionId, out var list)
                ? list
                : (IReadOnlyList<CompanionMemoryEntry>)System.Array.Empty<CompanionMemoryEntry>();
        }

        private static void RemoveOldestOfType(List<CompanionMemoryEntry> list, string memoryType)
        {
            int oldest = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == memoryType && (oldest < 0 || list[i].timestamp < list[oldest].timestamp))
                {
                    oldest = i;
                }
            }
            if (oldest >= 0) list.RemoveAt(oldest);
        }

        private static void RemoveRandomOfType(List<CompanionMemoryEntry> list, string memoryType)
        {
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == memoryType) indices.Add(i);
            }
            if (indices.Count > 0)
            {
                list.RemoveAt(indices[Random.Range(0, indices.Count)]);
            }
        }

        /// <summary>失效缓存（存档槽位切换时由 SaveSystem 调用）。</summary>
        public static void InvalidateCache()
        {
            _loaded = false;
            _memories.Clear();
        }

        // ── 礼物记忆的结构化存储：存档存机读 token（gift:{itemId}x{count}），展示/注入时格式化 ──

        /// <summary>生成礼物记忆内容（赠礼流程调用）。</summary>
        public static string MakeGiftContent(int itemId, int count)
        {
            return $"gift:{itemId}x{count}";
        }

        /// <summary>解析礼物记忆 token。</summary>
        public static bool TryParseGiftContent(string content, out int itemId, out int count)
        {
            itemId = 0;
            count = 0;
            if (string.IsNullOrEmpty(content) || !content.StartsWith("gift:"))
            {
                return false;
            }
            var parts = content.Substring(5).Split('x');
            return parts.Length == 2
                && int.TryParse(parts[0], out itemId)
                && int.TryParse(parts[1], out count);
        }

        /// <summary>记忆内容格式化给 LLM prompt（礼物转成自然语言，聊天原句直出）。</summary>
        public static string FormatForPrompt(CompanionMemoryEntry entry)
        {
            if (entry != null && entry.type == CompanionMemoryRuleConfigMgr.TypeGift
                && TryParseGiftContent(entry.content, out int itemId, out int count))
            {
                return $"The boss gave you a gift: {ItemConfigMgr.Instance.GetName(itemId)} x{count}.";
            }
            return entry?.content;
        }

        /// <summary>记忆内容格式化给信息面板（本地化；礼物按词条渲染，聊天原句直出）。</summary>
        public static string FormatForDisplay(CompanionMemoryEntry entry)
        {
            if (entry != null && entry.type == CompanionMemoryRuleConfigMgr.TypeGift
                && TryParseGiftContent(entry.content, out int itemId, out int count))
            {
                return Loc.Get("companion.memory.gift_line", ItemConfigMgr.Instance.GetName(itemId), count);
            }
            return entry?.content;
        }
    }
}
