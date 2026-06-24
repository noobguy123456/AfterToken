# 命中反馈系统

## 职责

在战斗中向玩家提供即时命中反馈，包括伤害飘字、受击方向指示和命中标记。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `DamageNumberUI` | `Assets/GameScripts/HotFix/GameLogic/UI/DamageNumberUI/DamageNumberUI.cs` | 伤害飘字 UI |
| `HitFeedbackUI` | `Assets/GameScripts/HotFix/GameLogic/UI/HitFeedbackUI/HitFeedbackUI.cs` | 8 方向受击指示与命中标记 |
| `HitFeedbackSystem` | `Assets/GameScripts/HotFix/GameLogic/UI/HitFeedback/HitFeedbackSystem.cs` | 纯逻辑层，调用 UI |

## 设计要点

- `DamageNumberUI` 使用对象池管理飘字实例。
- `HitFeedbackSystem` 监听 `BattleSystem.OnEntityDamaged` 等事件。
- `ProcedureBattle` 进入战斗后打开 `HitFeedbackUI`。
