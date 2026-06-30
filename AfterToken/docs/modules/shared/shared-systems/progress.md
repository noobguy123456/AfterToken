# Shared Systems 进度

> 共享系统负责跨玩法（战斗 ↔ 经营）的玩家持久数据与通用能力。

## 待办
- [ ] `PlayerProfileSystem`：等级、经验、解锁
- [ ] `CurrencySystem`：金币、钻石、能量等货币
- [ ] `InventorySystem`：背包/仓库物品管理
- [ ] `UnlockSystem`：内容解锁判定（已独立模块，见 `docs/modules/shared/unlock-system/`）
- [ ] `RewardSystem`：战斗/任务奖励分发（已独立模块，见 `docs/modules/combat/reward-system/`）
- [ ] 存档持久化（见 `docs/modules/shared/save-system/` 规划）

## 阻塞
- 共享层事件接口（`IPlayerProfileEvent`、`ICurrencyEvent`、`IInventoryEvent`、`IUnlockEvent`）尚未定义。
- 奖励系统、跨玩法联动、经营系统均依赖共享系统落地。

---

> 状态说明：
> - 当前总状态：⏳
> - 每次更新后同步 `docs/TODO.md`
