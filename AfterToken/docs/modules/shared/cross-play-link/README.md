# 跨玩法联动

## 职责

连接射击战斗与模拟经营两个玩法，实现资源、奖励和成长的互通。

## 实现（2026-08-20）

| 类/文件 | 说明 |
|---|---|
| `Shared/CrossPlayLink.cs` | 联动入口。`OnBattleExtracted(levelId)`：成功撤离（RETURN_TO_LOBBY 传送门）时按 `TbLevel.rewardGold/rewardExp` 发金币/经验，并 `MarkLevelCompleted` 记录通关（驱动关卡链解锁） |

## 关键流转

- ✅ 战斗撤离 → 货币/经验/通关记录（`CrossPlayLink` → `CurrencySystem` / `PlayerProfileSystem`）
- ✅ 通关记录 → 关卡解锁（`PlayerProfileSystem.IsLevelCompleted` → `UnlockSystem` → `LobbyUI`）
- ⏳ 战斗奖励 → 物品（临时背包 → 仓库已有，正式 `RewardSystem` 按 `TbDrop` 分发待做）
- ⏳ 经营产出 → 武器强化 / 角色训练（待强化/训练系统立项）

## 设计要点

- 跨玩法数据交换必须通过共享系统，禁止直接引用玩法内部系统；
- 撤离奖励配置在 `level.xlsx` 的 `rewardGold`/`rewardExp` 列。
