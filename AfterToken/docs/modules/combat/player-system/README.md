
+# 玩家系统

## 职责

管理玩家实体的创建、销毁、状态切换与生命周期，连接输入、武器、相机等系统。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `PlayerSystem` | `Assets/GameScripts/HotFix/GameLogic/System/PlayerSystem.cs` | 玩家系统 |
| `PlayerEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Player/PlayerEntity.cs` | 玩家表现实体 |
| `PlayerStateContext` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Context/PlayerStateContext.cs` | 状态黑板（输入、意图、运行时状态） |
| `PlayerStateBase` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerStateBase.cs` | 玩家状态基类 |
| `PlayerStateMachineDriver` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Driver/PlayerStateMachineDriver.cs` | 单例驱动器，维护拦截器并刷新黑板意图 |
| `PlayerStateInterceptor` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/PlayerStateInterceptor.cs` | 拦截器基类 |
| `DeathInterceptor` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/DeathInterceptor.cs` | 死亡强制跳转 |
| `DodgeInterceptor` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/DodgeInterceptor.cs` | 闪避跳转 |
| `ReloadStartInterceptor` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/ReloadStartInterceptor.cs` | 换弹跳转 |
| `PlayerIdleState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerIdleState.cs` | 待机 |
| `PlayerMoveState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerMoveState.cs` | 移动 |
| `PlayerReloadState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerReloadState.cs` | 换弹 |
| `PlayerDodgeState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDodgeState.cs` | 闪避 |
| `PlayerDeadState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDeadState.cs` | 死亡 |
| `IPlayerEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IPlayerEvent.cs` | 玩家事件接口 |
| `BattleMainUI` | `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs` | 战斗 HUD（含 HP/体力条） |

## 设计要点

- 使用 FSM 管理玩家战斗状态，底层保留 TEngine FSM。
- 引入**黑板 + 拦截器**架构：
  - `PlayerStateContext` 集中保存输入、意图与运行时状态（移动/瞄准输入、闪避/换弹/开火意图、死亡/换弹/闪避运行时标志、当前武器引用、待处理请求）。
  - `PlayerStateMachineDriver` 维护拦截器列表；`PlayerSystem.Update` 每帧调用其 `UpdateContext` 刷新黑板意图。
  - `DeathInterceptor` / `DodgeInterceptor` / `ReloadStartInterceptor` 按优先级处理强制状态跳转；TEngine `FsmModule` 自动轮询 FSM，`PlayerStateBase.OnUpdate` 查询拦截器并消费 `PendingRequest`。
- 开火输入 `FirePressed` / `WantsToFire` 已在黑板上预留，但当前开火仍由 `InputSystem` 直接触发 `WeaponSystem`，未经过玩家状态机。
- 体力系统：最大体力默认 100，闪避消耗默认 25，未死亡/未闪避时每秒恢复 30。`DodgeInterceptor` 通过 `Context.CanDodge` 判断能否闪避。
- HUD：`BattleMainUI` 通过 `m_slider_Hp` / `m_slider_Stamina` 显示血条与体力条，Prefab 未配置时动态创建。
- 普通状态切换通过 `RequestState<T>()` 写入黑板，由 `PlayerStateBase` 统一消费。
- `PlayerEntity` 只负责表现，逻辑由 `PlayerSystem` 与状态机驱动。
- 已接入 Luban `TbPlayer`：最大血量、最大体力、体力恢复速率、闪避消耗、移动速度、闪避速度、闪避持续时间均从配置读取；关卡 `playerMaxHp` 可覆盖血量。

## 状态归属（单一数据源，2026-08-08 收敛）

每份状态有且只有一个 owner，其他位置只持有访问路径（转发属性），不持有副本：

| 状态 | 唯一 owner | 说明 |
|---|---|---|
| IsDead / IsDodging / MoveInput / AimInput / IsAiming | `PlayerStateContext` 黑板 | `PlayerEntity` 四个重复字段已改为转发属性；`WeaponSystem._isAiming` 改名 `_isAimingFallback` 仅作 owner 注入前兜底，`AimAssistSystem` 直读 `WeaponSystem.IsAiming` |
| IsReloading（玩家语义） | `PlayerStateContext` 黑板 | `WeaponInstance.IsReloading` 是武器换弹计时器，语义不同，勿混用 |
| HP / 体力 | `PlayerSystem` | 无第二副本 |
| 弹药 | `WeaponInstance` | 属武器实例数据，不属玩家状态 |
| 跨场景传送快照 | `PortalPlayerState`（静态类） | 仅跨场景暂存，不负责运行期实时状态 |

背后的三条实现原理：

1. **数据只能有一个家，引用可以有无数个**——副本是 bug 温床，同步不能靠人肉。
2. **收敛不等于集中**——单一数据源是"按字段"说的，每份状态归属最懂它的系统，避免上帝类。
3. **用访问器隔离变化**——数据搬家保持属性签名不变，调用方无感。

> 详细论述见 `docs/Proposal/combat/pure-csharp-data-oriented-roadmap.md` 第 3.2 节。
