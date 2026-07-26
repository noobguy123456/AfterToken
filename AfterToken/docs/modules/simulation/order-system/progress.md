# 订单系统进度

## 已完成
- [x] 订单生成
- [x] 订单交付与奖励
- [x] 订单刷新条件

## 实现说明
1. `TbOrder` 配置订单需求、金币/物品/经验奖励、权重、时限、等级限制。
2. `TbSimTimeConfig` 配置订单刷新间隔与订单板上限。
3. `OrderSystem` 定时生成随机订单，交付时校验 `InventorySystem` 库存，扣除物品并发放 `CurrencySystem` 金币与 `InventorySystem` 物品奖励。
4. 订单倒计时与超时移除已实现；超时惩罚与品质分级后续扩展。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
