using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 撤离结算数据：一局战斗成功撤离后的汇总快照。
    /// 由 <see cref="SettlementSystem.CaptureAndSettle"/> 生成，经 UserDatas 传给 SettlementUI 展示。
    /// </summary>
    public sealed class SettlementData
    {
        /// <summary>撤离的关卡 ID。</summary>
        public int LevelId;

        /// <summary>撤离带入仓库的战利品快照（RunInventory 副本，与入库后状态无关）。</summary>
        public List<ItemStack> LootItems = new List<ItemStack>();

        /// <summary>meta 奖励发放结果（金币/经验/物品，来自 CrossPlayLink.OnBattleExtracted）。</summary>
        public RewardResult MetaReward;

        /// <summary>战利品估值合计（ItemConfigMgr.GetPrice × 数量求和）。</summary>
        public int LootValueTotal;
    }

    /// <summary>
    /// 撤离结算系统：撤离收口的"快照 → 入库 → meta 奖励"三段式编排。
    /// 时序要求：必须先于 ProcedureSimulation.EnterAsync 的 RunInventory.Clear() 执行。
    /// </summary>
    public static class SettlementSystem
    {
        /// <summary>
        /// 快照临时背包 → 整体转入仓库 → 发放撤离 meta 奖励，组装结算数据返回。
        /// </summary>
        public static SettlementData CaptureAndSettle(int levelId)
        {
            var data = new SettlementData { LevelId = levelId };

            // 快照（ItemStack 为值类型，AddRange 即深拷贝），入库失败不丢数据（仓库满时 Warehouse 记告警）
            data.LootItems.AddRange(RunInventory.Items);

            Warehouse.AddAll(RunInventory.Items);

            data.MetaReward = CrossPlayLink.OnBattleExtracted(levelId);

            int total = 0;
            for (int i = 0; i < data.LootItems.Count; i++)
            {
                var stack = data.LootItems[i];
                total += ItemConfigMgr.Instance.GetPrice(stack.ItemId) * stack.Count;
            }
            data.LootValueTotal = total;

            return data;
        }
    }
}
