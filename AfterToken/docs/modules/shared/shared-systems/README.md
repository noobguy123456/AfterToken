# 共享系统

## 职责

为射击和模拟经营两个玩法提供共享数据与服务，包括玩家档案、货币、背包、解锁和奖励。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `PlayerProfileSystem` | 玩家等级、经验、解锁 |
| `CurrencySystem` | 金币、钻石、能量 |
| `InventorySystem` | 背包/仓库物品管理 |
| `UnlockSystem` | 内容解锁判定 |
| `RewardSystem` | 奖励分发 |

## 设计要点

- 共享数据层使用单例或 Service Locator 模式。
- 数据变更通过事件通知 UI 和其他系统。
