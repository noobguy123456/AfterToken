# 玩家系统

## 职责

管理玩家实体的创建、销毁、状态切换与生命周期，连接输入、武器、相机等系统。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `PlayerSystem` | `Assets/GameScripts/HotFix/GameLogic/System/PlayerSystem.cs` | 玩家系统 |
| `PlayerEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Player/PlayerEntity.cs` | 玩家表现实体 |
| `PlayerStateBase` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerStateBase.cs` | 玩家状态基类 |
| `PlayerIdleState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerIdleState.cs` | 待机 |
| `PlayerMoveState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerMoveState.cs` | 移动 |
| `PlayerReloadState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerReloadState.cs` | 换弹 |
| `PlayerDodgeState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDodgeState.cs` | 闪避 |
| `PlayerDeadState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDeadState.cs` | 死亡 |
| `IPlayerEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IPlayerEvent.cs` | 玩家事件接口 |

## 设计要点

- 使用 FSM 管理玩家战斗状态，状态内部处理输入与切换。
- `PlayerEntity` 只负责表现，逻辑由 `PlayerSystem` 驱动。
- 待接入 Luban `TbPlayer` / `TbPlayerAttr` 配置表。
