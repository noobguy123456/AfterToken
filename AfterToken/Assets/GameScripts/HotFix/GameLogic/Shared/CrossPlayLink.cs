using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 跨玩法联动：战斗 → 共享层的奖励落地。
    /// 目前实现「成功撤离（通关）→ 金币/经验 + 通关记录」；
    /// 经营产出 → 战斗强化方向待武器强化/角色训练系统立项后接入。
    /// </summary>
    public static class CrossPlayLink
    {
        /// <summary>
        /// 战斗成功撤离（RETURN_BASE 传送门）时调用：
        /// 按 TbLevel 配置发放金币/经验，并标记该关卡已通关（驱动关卡链解锁）。
        /// 返回奖励发放结果（供撤离结算画面展示）；找不到关卡配置时返回空结果。
        /// </summary>
        public static RewardResult OnBattleExtracted(int levelId)
        {
            string source = $"level:{levelId}:extract";
            var cfg = LevelConfigMgr.Instance.Get(levelId);
            if (cfg == null)
            {
                Log.Warning($"[CrossPlayLink] 找不到关卡配置 id={levelId}，跳过撤离奖励");
                return new RewardResult { Source = source };
            }

            // meta 奖励统一走 RewardSystem 发放（局内战利品仍走 RunInventory → 入库链路）
            var result = RewardSystem.Grant(new RewardData().AddGold(cfg.rewardGold).AddExp(cfg.rewardExp), source);
            PlayerProfileSystem.MarkLevelCompleted(levelId);

            // 任务系统 extract 目标推进（与奖励发放同一时机）
            QuestSystem.OnBattleExtracted(levelId);

            // 队友好感：共同成功撤离（数值/每局上限走 TbCompanionAffinityGain.extract）
            CompanionAffinitySystem.AddFromSource("extract");
            return result;
        }
    }
}
