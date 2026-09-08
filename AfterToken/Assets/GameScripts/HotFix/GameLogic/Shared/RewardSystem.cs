using System.Collections.Generic;
using System.Text;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 单个物品奖励条目。
    /// </summary>
    public struct RewardItem
    {
        public int ItemId;
        public int Count;

        public RewardItem(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    /// <summary>
    /// 统一奖励数据：金币 / 经验 / 物品。
    /// 仅描述"账号级/meta 奖励"（撤离结算、任务、订单等直接发放到账的奖励）；
    /// 局内拾取/掉落走 RunInventory → 撤离入库链路，不经过本结构。
    /// </summary>
    public sealed class RewardData
    {
        public int Gold;
        public int Exp;
        public List<RewardItem> Items;

        public bool IsEmpty => Gold <= 0 && Exp <= 0 && (Items == null || Items.Count == 0);

        public RewardData AddGold(int gold)
        {
            if (gold > 0) Gold += gold;
            return this;
        }

        public RewardData AddExp(int exp)
        {
            if (exp > 0) Exp += exp;
            return this;
        }

        public RewardData AddItem(int itemId, int count)
        {
            if (itemId <= 0 || count <= 0) return this;
            Items ??= new List<RewardItem>();
            Items.Add(new RewardItem(itemId, count));
            return this;
        }
    }

    /// <summary>
    /// 奖励发放结果。供日志核对与后续结算 UI 汇总使用。
    /// </summary>
    public sealed class RewardResult
    {
        /// <summary>奖励来源标识（如 quest:1001 / order:3 / level:101:extract）。</summary>
        public string Source;
        public int Gold;
        public int Exp;
        /// <summary>成功入库的物品。</summary>
        public List<RewardItem> ItemsGranted = new List<RewardItem>();
        /// <summary>因仓库溢出等原因丢失的物品。</summary>
        public List<RewardItem> ItemsLost = new List<RewardItem>();

        public bool HasOverflow => ItemsLost.Count > 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Gold > 0) sb.Append($"+{Gold}G ");
            if (Exp > 0) sb.Append($"+{Exp}EXP ");
            foreach (var item in ItemsGranted)
            {
                sb.Append($"+{ItemConfigMgr.Instance.GetName(item.ItemId)}x{item.Count} ");
            }
            foreach (var item in ItemsLost)
            {
                sb.Append($"[丢失]{ItemConfigMgr.Instance.GetName(item.ItemId)}x{item.Count} ");
            }
            return sb.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// 奖励系统：meta 奖励的统一分发入口。
    /// 所有"直接发放到账"的奖励（撤离结算/任务/订单等）都必须经 <see cref="Grant"/> 发放，
    /// 由本类统一对接 CurrencySystem / PlayerProfileSystem / InventorySystem，并统一物品溢出策略。
    /// </summary>
    public static class RewardSystem
    {
        /// <summary>
        /// 发放奖励。物品经 InventorySystem 窄口入仓库，入库失败（如仓库满）计入 <see cref="RewardResult.ItemsLost"/> 并告警。
        /// </summary>
        /// <param name="reward">奖励内容；为空或全零时直接返回空结果，不打日志。</param>
        /// <param name="source">来源标识，用于日志与结算汇总，约定格式 "系统: id[:子场景]"。</param>
        public static RewardResult Grant(RewardData reward, string source)
        {
            var result = new RewardResult { Source = source };
            if (reward == null || reward.IsEmpty)
            {
                return result;
            }

            if (reward.Gold > 0)
            {
                CurrencySystem.AddGold(reward.Gold);
                result.Gold = reward.Gold;
            }
            if (reward.Exp > 0)
            {
                PlayerProfileSystem.AddExp(reward.Exp);
                result.Exp = reward.Exp;
            }
            if (reward.Items != null)
            {
                foreach (var item in reward.Items)
                {
                    if (InventorySystem.AddItem(item.ItemId, item.Count))
                    {
                        result.ItemsGranted.Add(item);
                    }
                    else
                    {
                        result.ItemsLost.Add(item);
                        Log.Warning($"[RewardSystem] 奖励物品入库失败（{source}）：itemId={item.ItemId} x{item.Count}");
                    }
                }
            }

            Log.Info($"[RewardSystem] 发放({source})：{result}");
            return result;
        }

        /// <summary>
        /// 解析配置表奖励串："gold:200|exp:50|item:10001:2"。
        /// 无法解析的段记告警并跳过。
        /// </summary>
        public static RewardData Parse(string rewards)
        {
            var data = new RewardData();
            if (string.IsNullOrWhiteSpace(rewards))
            {
                return data;
            }

            foreach (var term in rewards.Split('|'))
            {
                var parts = term.Split(':');
                switch (parts[0].Trim())
                {
                    case "gold" when parts.Length == 2 && int.TryParse(parts[1], out int gold):
                        data.AddGold(gold);
                        break;
                    case "exp" when parts.Length == 2 && int.TryParse(parts[1], out int exp):
                        data.AddExp(exp);
                        break;
                    case "item" when parts.Length == 3 && int.TryParse(parts[1], out int itemId) && int.TryParse(parts[2], out int count):
                        data.AddItem(itemId, count);
                        break;
                    default:
                        Log.Warning($"[RewardSystem] 无法解析奖励段: {term}");
                        break;
                }
            }
            return data;
        }
    }
}
