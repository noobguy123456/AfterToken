# 奖励系统

## 职责

处理战斗结算、任务与成就奖励的分发。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `BattleReward` | 战斗奖励数据结构 |
| `RewardSystem` | 奖励分发入口 |

## 设计要点

- 战斗胜利后由 `BattleSystem` 触发奖励计算。
- 奖励可流向 `CurrencySystem` / `InventorySystem`。
