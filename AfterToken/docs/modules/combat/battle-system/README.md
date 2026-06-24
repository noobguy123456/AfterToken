# 战斗系统

## 职责

处理伤害计算、暴击、Buff、死亡判定以及战斗结果分发。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `BattleSystem` | `Assets/GameScripts/HotFix/GameLogic/System/BattleSystem.cs` | 战斗核心逻辑 |
| `IBattleEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IBattleEvent.cs` | 战斗事件接口 |
| `DamageInfo` | `Assets/GameScripts/HotFix/GameLogic/Core/MemoryPool/DamageInfo.cs` | 伤害信息对象池 |

## 待完成

- 暴击、闪避计算
- Buff/Debuff 系统
- 战斗结果 `IBattleResultEvent`

## 设计要点

- `BattleSystem` 订阅 `IProjectileEvent.OnProjectileHit` 等事件进行伤害计算。
- 伤害信息使用 `DamageInfo` 对象池，避免 GC。
