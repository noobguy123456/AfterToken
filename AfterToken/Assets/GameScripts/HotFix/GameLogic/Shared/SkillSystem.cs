using System.Collections.Generic;
using GameConfig.cfg;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 技能树系统：三分支（战斗/生存/经营）技能升级与运行时加成聚合。
    /// 升级消耗 = 材料（仓库）+ 金币；只能在经营场景通过技能操作台升级（UI 层限制入口）。
    /// 持久化由 SaveSystem 接管（变动即存），首次访问时从存档懒加载。
    /// 加成读取方：PlayerSystem（生命/耐力/移速/闪避）、伤害结算、WeaponInstance（换弹/弹匣）、
    /// RewardSystem（金币）、PlayerProfileSystem（经验）、ProductionSystem（生产速度）。
    /// </summary>
    public static class SkillSystem
    {
        /// <summary>技能ID → 当前等级（未学为 0，不存条目）。</summary>
        private static readonly Dictionary<int, int> _levels = new Dictionary<int, int>();
        private static bool _loaded;

        /// <summary>
        /// 首次访问时从存档恢复；无存档时全部 0 级。
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var d = SaveSystem.Data.skill;
            if (!d.initialized) return;

            _levels.Clear();
            foreach (var e in d.skills)
            {
                if (e != null && e.level > 0)
                {
                    _levels[e.skillId] = e.level;
                }
            }
        }

        /// <summary>
        /// 变动即存：写回数据段并立即落盘。
        /// </summary>
        private static void Persist()
        {
            var d = SaveSystem.Data.skill;
            d.initialized = true;
            d.skills.Clear();
            foreach (var kv in _levels)
            {
                d.skills.Add(new SkillEntry { skillId = kv.Key, level = kv.Value });
            }
            SaveSystem.Flush();
        }

        /// <summary>技能当前等级（未学返回 0）。</summary>
        public static int GetLevel(int skillId)
        {
            EnsureLoaded();
            return _levels.TryGetValue(skillId, out var lv) ? lv : 0;
        }

        /// <summary>
        /// 聚合某类效果的总加成：Σ（等级 × 每级效果值）。
        /// 百分比类效果返回小数（如 5% = 0.05），加算类返回绝对值。无加成时返回 0。
        /// </summary>
        public static float GetEffect(ESkillEffect effectType)
        {
            EnsureLoaded();
            float total = 0f;
            foreach (var kv in _levels)
            {
                var cfg = SkillConfigMgr.Instance.Get(kv.Key);
                if (cfg != null && cfg.EffectType == effectType)
                {
                    total += kv.Value * cfg.EffectValue;
                }
            }
            return total;
        }

        /// <summary>
        /// 是否可升级。不可升级时 reason 为已本地化的失败原因（UI 直接显示）。
        /// </summary>
        public static bool CanUpgrade(int skillId, out string reason)
        {
            reason = null;
            var cfg = SkillConfigMgr.Instance.Get(skillId);
            if (cfg == null)
            {
                reason = $"unknown skill {skillId}";
                return false;
            }

            int level = GetLevel(skillId);
            if (level >= cfg.MaxLevel)
            {
                reason = Loc.Get("ui.sim.max_level");
                return false;
            }

            if (cfg.PrereqSkillId > 0)
            {
                var prereq = SkillConfigMgr.Instance.Get(cfg.PrereqSkillId);
                if (prereq != null && GetLevel(prereq.Id) < prereq.MaxLevel)
                {
                    reason = Loc.Get("ui.skill.err.prereq", Loc.Get(prereq.NameKey));
                    return false;
                }
            }

            if (!InventorySystem.HasItems(cfg.CostItems))
            {
                reason = Loc.Get("ui.sim.err.no_materials");
                return false;
            }

            if (!CurrencySystem.HasGold(cfg.CostGold))
            {
                reason = Loc.Get("ui.sim.err.no_gold");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 尝试升级：先校验，再扣材料与金币（都够才扣，不会产生部分扣除），成功后落盘并发事件。
        /// </summary>
        public static bool TryUpgrade(int skillId, out string reason)
        {
            if (!CanUpgrade(skillId, out reason))
            {
                return false;
            }

            var cfg = SkillConfigMgr.Instance.Get(skillId);
            if (!InventorySystem.TryConsumeItems(cfg.CostItems))
            {
                reason = Loc.Get("ui.sim.err.no_materials");
                return false;
            }
            if (!CurrencySystem.TryConsumeGold(cfg.CostGold))
            {
                // 理论上不会发生（CanUpgrade 已校验），兜底把材料退回去
                InventorySystem.AddItems(cfg.CostItems);
                reason = Loc.Get("ui.sim.err.no_gold");
                return false;
            }

            int newLevel = GetLevel(skillId) + 1;
            _levels[skillId] = newLevel;
            Persist();
            GameEvent.Get<ISkillEvent>()?.OnSkillUpgraded(skillId, newLevel);
            Log.Info($"[SkillSystem] 技能升级：{skillId} → Lv.{newLevel}");
            return true;
        }

        /// <summary>
        /// 失效缓存（存档槽位切换时由 SaveSystem 调用）：清空等级，下次访问从新槽位重读。
        /// </summary>
        public static void InvalidateCache()
        {
            _loaded = false;
            _levels.Clear();
        }
    }
}
