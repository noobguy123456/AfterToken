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
        /// </summary>
        public static void OnBattleExtracted(int levelId)
        {
            var cfg = LevelConfigMgr.Instance.Get(levelId);
            if (cfg == null)
            {
                Log.Warning($"[CrossPlayLink] 找不到关卡配置 id={levelId}，跳过撤离奖励");
                return;
            }

            if (cfg.rewardGold > 0)
            {
                CurrencySystem.AddGold(cfg.rewardGold);
            }
            if (cfg.rewardExp > 0)
            {
                PlayerProfileSystem.AddExp(cfg.rewardExp);
            }
            PlayerProfileSystem.MarkLevelCompleted(levelId);

            Log.Info($"[CrossPlayLink] 关卡 {levelId} 撤离结算：+{cfg.rewardGold}G +{cfg.rewardExp}EXP");
        }
    }
}
