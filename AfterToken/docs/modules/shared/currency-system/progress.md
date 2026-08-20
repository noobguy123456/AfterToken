# Currency System 进度

## 已完成
- [x] `CurrencySystem` 金币/钻石/体力 + 变化接口（2026-08-06 前）
- [x] `ICurrencyEvent` 事件接口（事件调用已加 `?.` 防空引用）
- [x] 与 `SaveSystem` 持久化对接（2026-08-06，变动即存，懒加载恢复）
- [x] `CurrencyType` 枚举 + 通用 `GetAmount/Has/Add/TryConsume`（2026-08-20，既有 Gold/Diamond 专用 API 保留）
- [x] 与经营系统消耗/产出对接（BuildingSystem 建造/升级消费金币，订单奖励金币）
- [x] 与战斗撤离奖励对接（2026-08-20，`CrossPlayLink.OnBattleExtracted` → AddGold）

## 待办
- [ ] 与正式 `RewardSystem` 奖励分发对接（TbDrop 结算）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
