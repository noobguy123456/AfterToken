# AfterToken 玩家状态机重构实施记录

> 实施时间：2026-06-30  
> 目标：将基于 `switch case` 的简陋状态机改造为「黑板 + 拦截器 + 统一驱动」的现代 FSM 架构。  
> 设计文档：`docs/CoWork/AfterToken-Player-FSM-Refactor-Plan.md`

---

## 1. 新增文件

| 文件 | 说明 |
|---|---|
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Context/PlayerStateContext.cs` | 玩家状态黑板 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Context/StateTransitionRequest.cs` | 状态切换请求 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/PlayerStateInterceptor.cs` | 拦截器基类 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/DeathInterceptor.cs` | 死亡强制打断 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/DodgeInterceptor.cs` | 闪避请求拦截 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/ReloadStartInterceptor.cs` | 换弹请求拦截 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Driver/PlayerStateMachineDriver.cs` | 统一驱动器 |

---

## 2. 修改文件

| 文件 | 修改内容 |
|---|---|
| `Assets/GameScripts/HotFix/GameLogic/Entity/Player/PlayerEntity.cs` | 新增 `PlayerStateContext Context` 字段 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerStateBase.cs` | 密封 `OnUpdate`，加入拦截器检查与请求消费；拆分 `OnEnterState/OnLeaveState/OnUpdateState` |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerIdleState.cs` | 改为读取 `Context.MoveInput`，用 `RequestState<PlayerMoveState>()` |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerMoveState.cs` | 改为读取 `Context.MoveInput`，用 `RequestState<PlayerIdleState>()` |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDodgeState.cs` | 移除 Timer，改为在 `OnUpdateState` 中累加时间；通过黑板设置 `IsDodging` |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerReloadState.cs` | 由武器实例管理计时，监听 `OnReloadStateChanged(false)` 切回 Idle |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDeadState.cs` | 改为继承新的基类生命周期 |
| `Assets/GameScripts/HotFix/GameLogic/System/PlayerSystem.cs` | 创建黑板、同步武器引用、每帧调用 `UpdateContext`、死亡时设置 `Context.IsDead` |
| `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs` | 新增调试状态文字 UI；新增 HP 条与体力条 Slider |
| `Assets/GameScripts/HotFix/GameLogic/System/PlayerSystem.cs` | 新增体力系统：恢复、闪避消耗、`CanDodge` 同步 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Context/PlayerStateContext.cs` | 新增 `CanDodge` 黑板标志 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/Interceptors/DodgeInterceptor.cs` | 使用 `Context.CanDodge` 判断能否闪避 |
| `Assets/GameScripts/HotFix/GameLogic/FSM/Player/PlayerDodgeState.cs` | 进入时消耗体力 |
| `Assets/GameScripts/HotFix/GameLogic/IEvent/IPlayerEvent.cs` | 新增 `OnStaminaChanged` 事件 |
| `Configs/GameConfig/Datas/player.xlsx` | 新增玩家属性列：maxStamina / staminaRecoveryRate / dodgeStaminaCost / dodgeSpeed / dodgeDuration |
| `Assets/GameScripts/HotFix/GameProto/GameConfig/cfg/Player.cs` | 同步新增配置字段（因当前 Luban 工具链不完整，已手动同步；后续需重新生成） |
| `Assets/AssetRaw/Configs/json/cfg_tbplayer.json` | 同步新增字段数据（需与 xlsx 保持一致） |

---

## 3. 关键设计决策

### 3.1 保留 TEngine FSM

没有替换 TEngine 的 `FsmState<T>`，而是基于它做封装。原因是：
- 降低迁移成本和风险。
- TEngine FSM 的生命周期和内存管理已经成熟。

### 3.2 FsmModule 自动驱动

`FsmModule` 每帧自动调用所有 FSM 的 `Update`，所以 `PlayerStateMachineDriver` 不驱动 FSM，只负责：
- 维护拦截器列表。
- 每帧更新 `PlayerStateContext` 意图。
- 供 `PlayerStateBase` 查询拦截器结果。

### 3.3 输入系统保持不变

`InputSystem` 仍通过事件广播输入，`PlayerSystem` 订阅事件后写入黑板。这样保持了解耦。

### 3.4 拦截器触发切换

由于 `FsmState<T>.ChangeState` 是受保护方法，`PlayerStateMachineDriver` 无法直接调用。因此：
- Driver 的 `TryGetInterruptRequest` 只返回切换请求。
- `PlayerStateBase.OnUpdate` 内部调用 `ChangeState(fsm, request.TargetStateType)` 执行切换。

### 3.5 换弹状态与武器实例协作

`PlayerReloadState` 不自己计时，而是：
- 进入时调用 `Context.CurrentWeapon?.Reload(ownerId)`。
- 监听 `OnReloadStateChanged(false)`，在武器实例完成或取消换弹时切回 Idle。

这样武器实例仍是换弹的权威来源，状态机只响应结果。

---

## 4. 调试状态 UI

在 `BattleMainUI` 中动态创建了黄色状态文字：
- 节点名：`m_text_State_Debug`
- 位置：HUD 左下角，HP 信息上方
- 更新方式：监听 `IPlayerEvent.OnPlayerStateChanged`

该 UI 为临时调试用途，后续按 `AfterToken-Player-FSM-Refactor-Plan.md` 第 13 章步骤删除。

---

## 5. 编译结果

- 编译通过，无错误。
- Domain reload 正常完成。

---

## 6. 验证清单

### 基础状态切换（已初步确认，🟡 待更多状态加入后回归验证）

- [x] 进入战斗后状态显示为 `Idle`。
- [x] 按 WASD 后状态切换为 `Move`，停止后回到 `Idle`。
- [x] 按 Space 后状态切换为 `Dodge`， DodgeDuration 后回到 `Idle`。
- [x] 按 R 后状态切换为 `Reload`，换弹完成后回到 `Idle`。
- [x] 换弹中按 Space 闪避，应切换到 `Dodge`（`DodgeInterceptor` 优先级高于 `ReloadStartInterceptor`，在 Reload 状态中仍会被 Dodge 打断）。
- [x] 死亡后状态切换为 `Dead`，且无法再移动/闪避/换弹。
- [ ] 打开设置面板（Time.timeScale=0）时状态机不推进。

### 后续状态扩展调试（⏳ 待新增状态后验证）

- [ ] 瞄准状态（如 AimDownSights / 狙击镜）与 Idle / Move / Reload / Dodge 的切换。
- [ ] 交互状态（如拾取、开门）不与其他动作冲突。
- [ ] 受击硬直 / 眩晕状态的打断与恢复。
- [ ] 技能 / 道具使用状态与现有全局拦截器的优先级协调。
