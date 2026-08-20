using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 解锁系统：内容（关卡/武器）解锁条件校验与解锁记录。
    /// 规则：
    /// - TbUnlock 中没有配置的内容默认开放（不影响未配置的既有内容）；
    /// - 免费项（costGold=0）满足条件即视为已解锁，不落存档；
    /// - 付费项需通过 <see cref="TryUnlock"/> 消费金币后写入存档（变动即存）。
    /// </summary>
    public static class UnlockSystem
    {
        private static bool _loaded;
        private static readonly HashSet<int> _unlockedIds = new HashSet<int>();

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.unlock;
            if (!d.initialized) return;

            foreach (var id in d.unlockedIds)
            {
                _unlockedIds.Add(id);
            }
        }

        private static void Persist()
        {
            var d = SaveSystem.Data.unlock;
            d.initialized = true;
            d.unlockedIds.Clear();
            d.unlockedIds.AddRange(_unlockedIds);
            SaveSystem.Flush();
        }

        /// <summary>
        /// 内容是否已解锁。未配置解锁条件的内容一律返回 true。
        /// </summary>
        public static bool IsUnlocked(UnlockContentType contentType, int targetId)
        {
            var cfg = GetConfig(contentType, targetId);
            if (cfg == null) return true;
            return IsUnlocked(cfg);
        }

        public static bool IsUnlocked(Unlock cfg)
        {
            EnsureLoaded();
            if (_unlockedIds.Contains(cfg.Id)) return true;
            // 免费项条件满足即视为已解锁
            return cfg.CostGold <= 0 && ConditionsMet(cfg);
        }

        /// <summary>
        /// 条件是否满足（不含金币；金币在 TryUnlock 中消费）。
        /// </summary>
        public static bool ConditionsMet(Unlock cfg)
        {
            if (cfg.RequirePlayerLevel > 0 && PlayerProfileSystem.Level < cfg.RequirePlayerLevel)
            {
                return false;
            }
            if (cfg.RequireCompleteLevelId > 0 && !PlayerProfileSystem.IsLevelCompleted(cfg.RequireCompleteLevelId))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 付费解锁。条件不足或金币不够时返回 false 并给出英文原因。
        /// </summary>
        public static bool TryUnlock(int unlockId, out string failReason)
        {
            EnsureLoaded();
            failReason = null;

            var cfg = ConfigSystem.Instance.Tables.TbUnlock.GetOrDefault(unlockId);
            if (cfg == null)
            {
                failReason = "Unknown unlock id";
                return false;
            }
            if (_unlockedIds.Contains(unlockId))
            {
                failReason = "Already unlocked";
                return false;
            }
            if (!ConditionsMet(cfg))
            {
                failReason = GetLockHint(cfg);
                return false;
            }
            if (cfg.CostGold > 0 && !CurrencySystem.TryConsumeGold(cfg.CostGold))
            {
                failReason = $"Not enough gold (need {cfg.CostGold}G)";
                return false;
            }

            _unlockedIds.Add(unlockId);
            Persist();
            GameEvent.Get<IUnlockEvent>()?.OnContentUnlocked(unlockId);
            return true;
        }

        /// <summary>
        /// 锁定提示（英文），供 UI 显示。已解锁时返回 null。
        /// </summary>
        public static string GetLockHint(UnlockContentType contentType, int targetId)
        {
            var cfg = GetConfig(contentType, targetId);
            if (cfg == null || IsUnlocked(cfg)) return null;
            return GetLockHint(cfg);
        }

        private static string GetLockHint(Unlock cfg)
        {
            var parts = new List<string>();
            if (cfg.RequireCompleteLevelId > 0 && !PlayerProfileSystem.IsLevelCompleted(cfg.RequireCompleteLevelId))
            {
                parts.Add($"Clear Stage {cfg.RequireCompleteLevelId}");
            }
            if (cfg.RequirePlayerLevel > 0 && PlayerProfileSystem.Level < cfg.RequirePlayerLevel)
            {
                parts.Add($"Player Lv{cfg.RequirePlayerLevel}");
            }
            if (cfg.CostGold > 0)
            {
                parts.Add($"{cfg.CostGold}G");
            }
            return parts.Count > 0 ? string.Join(" + ", parts) : "Locked";
        }

        /// <summary>
        /// 查找某内容的解锁配置（同一内容取第一条匹配记录；无配置返回 null）。
        /// </summary>
        public static Unlock GetConfig(UnlockContentType contentType, int targetId)
        {
            var list = ConfigSystem.Instance.Tables.TbUnlock.DataList;
            for (int i = 0; i < list.Count; i++)
            {
                var cfg = list[i];
                if (cfg.ContentType == contentType && cfg.TargetId == targetId)
                {
                    return cfg;
                }
            }
            return null;
        }

        /// <summary>
        /// 清空解锁记录（GM 用），立即落盘。
        /// </summary>
        public static void Reset()
        {
            _unlockedIds.Clear();
            _loaded = true;
            Persist();
        }
    }
}
