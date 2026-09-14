using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// AI 队友好感度系统：累计好感值 → 4 档（阈值/增益数值全走 TbCompanionAffinityTier/Gain）。
    /// 来源：赠礼（item.affinityValue）、聊天（冷却防刷）、共同撤离、护驾——后三者为 source 增益表驱动。
    /// 持久化由 SaveSystem 接管（变动即存），首次访问懒加载；档位数值变化即时发事件。
    /// </summary>
    public static class CompanionAffinitySystem
    {
        /// <summary>当前唯一队友 id（多队友预留：接口都带 companionId）。</summary>
        public const int DefaultCompanionId = 1;

        private static readonly Dictionary<int, int> _expByCompanion = new Dictionary<int, int>();
        private static bool _loaded;

        /// <summary>来源冷却（source → Time.time 可用时刻）与每局计数，均为运行时状态不落盘。</summary>
        private static readonly Dictionary<string, float> _cooldownUntil = new Dictionary<string, float>();
        private static readonly Dictionary<string, int> _runCounts = new Dictionary<string, int>();

        /// <summary>
        /// 首次访问时从存档恢复。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.companionAffinity;
            if (!d.initialized) return;

            _expByCompanion.Clear();
            foreach (var e in d.entries)
            {
                if (e != null)
                {
                    _expByCompanion[e.companionId] = e.exp;
                }
            }
        }

        private static void Persist()
        {
            var d = SaveSystem.Data.companionAffinity;
            d.initialized = true;
            d.entries.Clear();
            foreach (var kv in _expByCompanion)
            {
                d.entries.Add(new CompanionAffinityEntry { companionId = kv.Key, exp = kv.Value });
            }
            SaveSystem.Flush();
        }

        /// <summary>队友当前累计好感值。</summary>
        public static int GetExp(int companionId = DefaultCompanionId)
        {
            EnsureLoaded();
            return _expByCompanion.TryGetValue(companionId, out var v) ? v : 0;
        }

        /// <summary>队友当前档位（1-4，按配置阈值）。</summary>
        public static int GetTier(int companionId = DefaultCompanionId)
        {
            var cfg = CompanionAffinityConfigMgr.Instance.GetTierForExp(GetExp(companionId));
            return cfg?.Tier ?? 1;
        }

        /// <summary>
        /// 直接加好感（内部用）：处理档位晋升事件与落盘。
        /// </summary>
        private static void AddRaw(int companionId, int amount)
        {
            if (amount <= 0) return;
            EnsureLoaded();

            int oldTier = GetTier(companionId);
            int total = GetExp(companionId) + amount;
            _expByCompanion[companionId] = total;

            GameEvent.Get<ICompanionAffinityEvent>()?.OnAffinityChanged(companionId, amount, total);

            int newTier = GetTier(companionId);
            if (newTier > oldTier)
            {
                GameEvent.Get<ICompanionAffinityEvent>()?.OnAffinityTierUp(companionId, newTier);
                Log.Info($"[CompanionAffinitySystem] 队友 {companionId} 好感升档：T{oldTier} → T{newTier}");
            }

            Persist();
        }

        /// <summary>
        /// 按来源加好感（chat/extract/protect）：数值、冷却、每局上限全部读 TbCompanionAffinityGain。
        /// 来源未配置/冷却中/超每局上限时不加，返回 false。
        /// </summary>
        public static bool AddFromSource(string source, int companionId = DefaultCompanionId)
        {
            var gain = CompanionAffinityConfigMgr.Instance.GetGain(source);
            if (gain == null || gain.Value <= 0)
            {
                return false;
            }

            if (gain.CooldownSec > 0f
                && _cooldownUntil.TryGetValue(source, out var until)
                && Time.time < until)
            {
                return false;
            }

            if (gain.PerRunCap > 0
                && _runCounts.TryGetValue(source, out var count)
                && count >= gain.PerRunCap)
            {
                return false;
            }

            if (gain.CooldownSec > 0f)
            {
                _cooldownUntil[source] = Time.time + gain.CooldownSec;
            }
            _runCounts[source] = _runCounts.TryGetValue(source, out var c) ? c + 1 : 1;

            AddRaw(companionId, gain.Value);
            return true;
        }

        /// <summary>
        /// 赠礼加好感：数值取物品 affinityValue。返回实际增加值（物品不可赠送/拒收返回 0）。
        /// </summary>
        public static int AddGift(int itemId, int companionId = DefaultCompanionId)
        {
            var item = ItemConfigMgr.Instance.Get(itemId);
            if (item == null || item.AffinityValue <= 0 || item.CompanionAccept == 0)
            {
                return 0;
            }

            AddRaw(companionId, item.AffinityValue);
            return item.AffinityValue;
        }

        /// <summary>
        /// 每局计数清零（进入战斗关卡时调用；冷却为现实时间不清）。
        /// </summary>
        public static void ResetRunCounters()
        {
            _runCounts.Clear();
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用）。
        /// </summary>
        public static void InvalidateCache()
        {
            _loaded = false;
            _expByCompanion.Clear();
            _cooldownUntil.Clear();
            _runCounts.Clear();
        }
    }
}
