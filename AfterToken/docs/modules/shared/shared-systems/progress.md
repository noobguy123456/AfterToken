# Shared Systems 进度

> 共享系统负责跨玩法（战斗 ↔ 经营）的玩家持久数据与通用能力。

## 已完成
- [x] `InventorySystem`：临时背包 + 仓库（槽位制、容量配置、B 键面板、悬浮提示）
- [x] `ItemSystem`：道具配置、4 档稀有度、稀有度框 prefab

## 待办
- [ ] `SaveSystem`：本地 JSON/PlayerPrefs 存档持久化
- [ ] `PlayerProfileSystem`：等级、经验、解锁
- [ ] `CurrencySystem`：金币、钻石、能量等货币
- [ ] `UnlockSystem`：内容解锁判定（已独立模块，见 `docs/modules/shared/unlock-system/`）
- [ ] `RewardSystem`：战斗/任务奖励分发（已独立模块，见 `docs/modules/combat/reward-system/`）
- [ ] `SettingsSystem`：音量、画质、操作设置持久化（已独立模块，见 `docs/modules/shared/settings-system/`）

## 阻塞
- 共享层事件接口（`IPlayerProfileEvent`、`ICurrencyEvent`、`IInventoryEvent`、`IUnlockEvent`）尚未定义。
- 奖励系统、跨玩法联动、经营系统均依赖 `SaveSystem`/`CurrencySystem`/`PlayerProfileSystem` 落地。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
