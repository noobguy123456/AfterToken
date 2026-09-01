using System;
using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 任务状态（四态，无失败态——搜打撤里"没完成"就是没完成）。
    /// </summary>
    public enum QuestState
    {
        /// <summary>未接取。</summary>
        Inactive,
        /// <summary>进行中。</summary>
        Active,
        /// <summary>全部目标达成，可交付。</summary>
        ReadyToTurnIn,
        /// <summary>已完成。</summary>
        Completed,
    }

    /// <summary>
    /// 任务系统（纯 C# 静态类，跨场景常驻）：
    /// 任务进度跨场景累计（基地接 → 战斗做 → 撤离回基地交），
    /// 订阅全局 GameEvent 总线而非场景内系统实例，场景销毁不影响订阅。
    /// 数据定义见 TbQuest/TbQuestObjective，设计文档 docs/Proposal/narrative/quest-system.md。
    /// </summary>
    public static class QuestSystem
    {
        private static bool _loaded;
        private static bool _subscribed;

        /// <summary>questId → 状态。</summary>
        private static readonly Dictionary<int, QuestState> _states = new Dictionary<int, QuestState>();

        /// <summary>objectiveId → 当前计数。</summary>
        private static readonly Dictionary<int, int> _progress = new Dictionary<int, int>();

        private static void EnsureLoaded()
        {
            if (!_subscribed)
            {
                _subscribed = true;
                // 全局事件总线常驻，静态类订阅一次即可（先例：PlayerAttrStore）
                GameEvent.AddEventListener<int, int>(IEnemyEvent_Event.OnEnemyDied, OnEnemyDied);
                GameEvent.AddEventListener(IItemEvent_Event.OnWarehouseChanged, OnWarehouseChanged);
                DialogueFlagSystem.OnFlagSet += OnFlagSet;
            }

            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.quest;
            if (!d.initialized) return;

            foreach (var entry in d.quests)
            {
                if (Enum.TryParse(entry.state, out QuestState state))
                {
                    _states[entry.questId] = state;
                }
                foreach (var p in entry.objectiveProgress)
                {
                    _progress[p.objectiveId] = p.count;
                }
            }
        }

        private static void Persist()
        {
            var d = SaveSystem.Data.quest;
            d.initialized = true;
            d.quests.Clear();
            foreach (var kv in _states)
            {
                var entry = new QuestEntry { questId = kv.Key, state = kv.Value.ToString() };
                var objectives = QuestConfigMgr.Instance.GetObjectives(kv.Key);
                foreach (var obj in objectives)
                {
                    if (_progress.TryGetValue(obj.Id, out int count))
                    {
                        entry.objectiveProgress.Add(new QuestObjectiveProgress { objectiveId = obj.Id, count = count });
                    }
                }
                d.quests.Add(entry);
            }
            SaveSystem.Flush();
        }

        // ---- 查询 ----

        /// <summary>
        /// 任务状态。未接取/未知任务返回 Inactive。
        /// </summary>
        public static QuestState GetState(int questId)
        {
            EnsureLoaded();
            return _states.TryGetValue(questId, out var s) ? s : QuestState.Inactive;
        }

        /// <summary>
        /// 目标当前计数（未知目标返回 0）。
        /// </summary>
        public static int GetProgress(int objectiveId)
        {
            EnsureLoaded();
            return _progress.TryGetValue(objectiveId, out var c) ? c : 0;
        }

        /// <summary>
        /// 满足接取条件（prereq 表达式求值）且尚未接取/完成。
        /// </summary>
        public static bool CanAccept(int questId)
        {
            EnsureLoaded();
            var cfg = QuestConfigMgr.Instance.Get(questId);
            if (cfg == null) return false;
            if (GetState(questId) != QuestState.Inactive) return false;
            return string.IsNullOrEmpty(cfg.Prereq) || NarrativeCondition.Evaluate(cfg.Prereq);
        }

        /// <summary>
        /// 进行中的任务 ID 列表（Active + ReadyToTurnIn）。
        /// </summary>
        public static List<int> GetActiveQuests()
        {
            EnsureLoaded();
            var result = new List<int>();
            foreach (var kv in _states)
            {
                if (kv.Value == QuestState.Active || kv.Value == QuestState.ReadyToTurnIn)
                {
                    result.Add(kv.Key);
                }
            }
            return result;
        }

        // ---- 状态机 ----

        /// <summary>
        /// 接取任务：Inactive → Active。条件不满足或重复接取返回 false。
        /// </summary>
        public static bool Accept(int questId)
        {
            EnsureLoaded();
            if (!CanAccept(questId)) return false;

            _states[questId] = QuestState.Active;
            // collect/flag 类目标可能在接取前已满足（仓库里已有货），接取后立即重算
            RecalcHoldObjectives(questId);
            Persist();
            GameEvent.Get<IQuestEvent>()?.OnQuestAccepted(questId);
            CheckReadyToTurnIn(questId);
            return true;
        }

        /// <summary>
        /// 交付任务：ReadyToTurnIn → Completed，发放奖励。
        /// </summary>
        public static bool TryTurnIn(int questId)
        {
            EnsureLoaded();
            if (GetState(questId) != QuestState.ReadyToTurnIn) return false;

            var cfg = QuestConfigMgr.Instance.Get(questId);
            if (cfg == null) return false;

            _states[questId] = QuestState.Completed;
            GrantRewards(cfg.Rewards);
            Persist();
            GameEvent.Get<IQuestEvent>()?.OnQuestCompleted(questId);
            Log.Info($"[QuestSystem] 任务完成 id={questId} rewards={cfg.Rewards}");
            return true;
        }

        /// <summary>
        /// 奖励串发放：gold:200|exp:50|item:3001:2。
        /// 复用窄口：CurrencySystem / PlayerProfileSystem / InventorySystem，不碰内部结构。
        /// </summary>
        private static void GrantRewards(string rewards)
        {
            if (string.IsNullOrWhiteSpace(rewards)) return;
            foreach (var term in rewards.Split('|'))
            {
                var parts = term.Split(':');
                switch (parts[0].Trim())
                {
                    case "gold" when parts.Length == 2 && int.TryParse(parts[1], out int gold):
                        CurrencySystem.AddGold(gold);
                        break;
                    case "exp" when parts.Length == 2 && int.TryParse(parts[1], out int exp):
                        PlayerProfileSystem.AddExp(exp);
                        break;
                    case "item" when parts.Length == 3 && int.TryParse(parts[1], out int itemId) && int.TryParse(parts[2], out int count):
                        InventorySystem.AddItem(itemId, count);
                        break;
                    default:
                        Log.Warning($"[QuestSystem] 无法解析奖励: {term}");
                        break;
                }
            }
        }

        // ---- 事件推进 ----

        private static void OnEnemyDied(int enemyId, int configId)
        {
            EnsureLoaded();
            foreach (var questId in GetActiveQuests())
            {
                if (GetState(questId) != QuestState.Active) continue;
                foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
                {
                    if (obj.Type == "kill" && TryParseTarget(obj, out int targetConfigId) && targetConfigId == configId)
                    {
                        AddProgress(questId, obj);
                    }
                }
            }
        }

        /// <summary>
        /// 撤离结算钩子（CrossPlayLink.OnBattleExtracted 末尾调用）。
        /// </summary>
        public static void OnBattleExtracted(int levelId)
        {
            EnsureLoaded();
            foreach (var questId in GetActiveQuests())
            {
                if (GetState(questId) != QuestState.Active) continue;
                foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
                {
                    if (obj.Type == "extract" && TryParseTarget(obj, out int targetLevelId) && targetLevelId == levelId)
                    {
                        AddProgress(questId, obj);
                    }
                }
            }
        }

        /// <summary>
        /// 仓库变化：重算所有进行中任务的 collect 目标（塔科夫式"带回来才算"，按当前持有数而非累计拾取）。
        /// </summary>
        private static void OnWarehouseChanged()
        {
            EnsureLoaded();
            foreach (var questId in GetActiveQuests())
            {
                if (GetState(questId) == QuestState.Active)
                {
                    RecalcHoldObjectives(questId);
                }
            }
        }

        /// <summary>
        /// 对话标志位写入（DialogueFlagSystem.Set 通知）：重算 flag 目标。
        /// </summary>
        private static void OnFlagSet(string key)
        {
            EnsureLoaded();
            foreach (var questId in GetActiveQuests())
            {
                if (GetState(questId) != QuestState.Active) continue;
                foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
                {
                    if (obj.Type == "flag" && obj.TargetId == key && DialogueFlagSystem.Has(key))
                    {
                        SetProgress(questId, obj, obj.Count);
                    }
                }
            }
        }

        /// <summary>
        /// 重算"持有型"目标（collect 按仓库持有数、flag 按标志位）。
        /// 接取时与仓库/标志变化时调用。
        /// </summary>
        private static void RecalcHoldObjectives(int questId)
        {
            bool dirty = false;
            foreach (var obj in QuestConfigMgr.Instance.GetObjectives(questId))
            {
                switch (obj.Type)
                {
                    case "collect" when TryParseTarget(obj, out int itemId):
                        dirty |= SetProgress(questId, obj, Math.Min(obj.Count, InventorySystem.GetItemCount(itemId)));
                        break;
                    case "flag":
                        dirty |= SetProgress(questId, obj, DialogueFlagSystem.Has(obj.TargetId) ? obj.Count : 0);
                        break;
                }
            }
            if (dirty)
            {
                Persist();
            }
        }

        private static void AddProgress(int questId, QuestObjective obj)
        {
            int cur = GetProgress(obj.Id);
            if (cur >= obj.Count) return;
            SetProgress(questId, obj, Math.Min(obj.Count, cur + 1));
            Persist();
        }

        /// <summary>
        /// 设置目标进度并广播事件，返回是否有变化。
        /// </summary>
        private static bool SetProgress(int questId, QuestObjective obj, int value)
        {
            int old = GetProgress(obj.Id);
            if (old == value) return false;
            _progress[obj.Id] = value;
            GameEvent.Get<IQuestEvent>()?.OnObjectiveProgress(questId, obj.Id, value, obj.Count);
            if (value >= obj.Count)
            {
                CheckReadyToTurnIn(questId);
            }
            return true;
        }

        /// <summary>
        /// 全部目标达成则 Active → ReadyToTurnIn 并广播。
        /// </summary>
        private static void CheckReadyToTurnIn(int questId)
        {
            if (GetState(questId) != QuestState.Active) return;
            var objectives = QuestConfigMgr.Instance.GetObjectives(questId);
            if (objectives.Count == 0) return;
            foreach (var obj in objectives)
            {
                if (GetProgress(obj.Id) < obj.Count) return;
            }
            _states[questId] = QuestState.ReadyToTurnIn;
            Persist();
            GameEvent.Get<IQuestEvent>()?.OnQuestReadyToTurnIn(questId);
        }

        private static bool TryParseTarget(QuestObjective obj, out int target)
        {
            if (int.TryParse(obj.TargetId, out target)) return true;
            Log.Warning($"[QuestSystem] 目标 {obj.Id}（{obj.Type}）的 targetId 不是数字: {obj.TargetId}");
            return false;
        }

        /// <summary>
        /// 清空任务记录（GM 用），立即落盘。
        /// </summary>
        public static void Reset()
        {
            _states.Clear();
            _progress.Clear();
            _loaded = true;
            EnsureLoaded();
            Persist();
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用），下次访问从新槽位重读。
        /// 事件订阅（_subscribed）保留——总线常驻，不重复订阅。
        /// </summary>
        public static void InvalidateCache()
        {
            _states.Clear();
            _progress.Clear();
            _loaded = false;
        }
    }
}
