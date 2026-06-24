# 事件系统

## 职责

为各系统提供解耦的通信机制。战斗、共享、经营等模块通过事件接口发送和监听事件。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `GameEvent` / `GameEventMgr` | `Assets/TEngine/Runtime/Core/GameEvent/` | TEngine 事件总线 |
| `EventInterfaceAttribute` | `Assets/TEngine/Runtime/Core/GameEvent/` | 事件接口标记 |
| `IEvent/` | `Assets/GameScripts/HotFix/GameLogic/IEvent/` | 项目事件接口定义 |

## 已有事件接口

- `IBattleInputEvent`、`IPlayerEvent`、`IProjectileEvent`、`IBattleEvent`、`IEnemyEvent`

## 待补充

- `ILevelEvent`、`IBattleResultEvent`
- `IPlayerProfileEvent`、`ICurrencyEvent`、`IInventoryEvent`、`IUnlockEvent`
- `ISimulationEvent`

## 设计要点

- 跨系统通信必须通过事件接口，禁止直接引用。
- UI 内部使用 `AddUIEvent` 监听业务事件。
