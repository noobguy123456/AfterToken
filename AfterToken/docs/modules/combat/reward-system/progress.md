# Reward System 进度

## 已完成
- [x] `RewardData` / `RewardItem` / `RewardResult` 数据结构（金币/经验/物品 + 发放结果汇总）
- [x] `RewardSystem.Grant(RewardData, source)` 统一分发入口（对接 CurrencySystem / PlayerProfileSystem / InventorySystem，物品溢出统一计入 ItemsLost 并告警）
- [x] `RewardSystem.Parse` 配置奖励串解析（"gold:200|exp:50|item:10001:2"）
- [x] 三处调用方收口：`CrossPlayLink.OnBattleExtracted`（撤离结算）、`QuestSystem.TryTurnIn`（任务奖励）、`OrderSystem.TryDeliverOrder`（订单奖励）
- [x] Play 实测：Grant 金币/物品到账与日志汇总正确、Parse 非法段告警跳过、测试后存档还原；编译 0 error
- [x] 撤离结算画面（SettlementUI）：`CrossPlayLink.OnBattleExtracted` 改为返回 `RewardResult`；新增 `SettlementSystem.CaptureAndSettle`（快照 RunInventory → 入库 → meta 奖励 → 估值合计）；`PortalSystem.ExtractToBase` 收口改弹结算画面，切流程挪到确认按钮；Play 实测通过（详见 docs/TODO.md 2026-09-06 条目）

## 待办
- [ ] 成就/首通等新奖励来源接入（直接调 Grant 即可）

---

> 状态说明：
> - 当前总状态：🟢（统一入口 + 结算画面均已落地）
> - 每次更新后同步 `docs/TODO.md`
