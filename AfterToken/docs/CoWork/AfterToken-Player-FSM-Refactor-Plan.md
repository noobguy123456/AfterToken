# AfterToken 玩家状态机重构方案

> 基于 BBB-Nexus 状态机切换思想，为 AfterToken 量身定制的一套玩家 FSM 方案。  
> 不引入骨骼分层、不替换动画/物理系统，只解决「状态切换逻辑分散、全局打断难维护」的问题。

---

## 0. 实施状态

| 项目 | 状态 |
|---|---|
| 基础框架（黑板、请求、拦截器、驱动器、状态基类） | ✅ 已完成 |
| 全局拦截器（Death / Dodge / ReloadStart） | ✅ 已完成 |
| 各状态改造（Idle / Move / Dodge / Reload / Dead） | ✅ 已完成 |
| `PlayerSystem` 接入 | ✅ 已完成 |
| 调试状态 UI | ✅ 已完成（临时） |
| 编译验证 | ✅ 已通过 |
| 体力系统 + HUD 血条/体力条 | ✅ 已完成 |
| `TbPlayer` 配置表接入 | ✅ 已完成（已通过 `gen_code_bin_to_project.bat` 重新生成 Player.cs 与 JSON） |
| Play Mode 验证 | 🟡 基础切换已初步确认，待补充更多状态调试 |
| 删除调试 UI | ⏳ 待进行 |
| 后续状态扩展调试（如瞄准、交互、受击硬直等） | ⏳ 待进行 |

### 0.1 实际落地与初稿的差异

- **Driver 不主动驱动 FSM**：实际实现与初稿一致，由 TEngine `FsmModule` 自动轮询 FSM；`PlayerStateMachineDriver` 只负责维护拦截器与刷新黑板意图（`UpdateContext`）。
- **拦截器优先级**：实际代码中各拦截器通过重写 `PlayerStateInterceptor.Priority` 属性指定优先级（`DeathInterceptor` 1000、`DodgeInterceptor` 100、`ReloadStartInterceptor` 50），与初稿示例数值一致。
- **死亡拦截器**：实际 `DeathInterceptor` 直接判断 `IsDead` 与当前状态是否已是 `PlayerDeadState`。
- **闪避拦截器**：实际 `DodgeInterceptor` 使用 `Context.CanMove` 判断能否移动，不会主动取消换弹；换弹中断由 `WeaponSystem` 切枪或后续扩展负责。
- **换弹拦截器**：`ReloadStartInterceptor` 除判断武器弹药是否已满外，还额外判断 `weapon.IsReloading`，避免重复进入。
- **状态基类 `ChangeState` 调用**：`PlayerStateBase` 内部使用从 `FsmState<PlayerEntity>` 继承的 `protected ChangeState(IFsm<PlayerEntity>, Type)` 方法完成切换；`PlayerStateMachineDriver` 通过 `TryGetInterruptRequest` 返回命中结果，实际切换仍由 `PlayerStateBase.OnUpdate` 执行。
- **输入消费**：`PlayerSystem` 在输入事件中设置一次性标志 `DodgePressed` / `ReloadPressed`；`PlayerStateMachineDriver.UpdateContext` 在每帧开始时 `ResetIntent()`，再将其转为 `WantsToDodge` / `WantsToReload`，同时清空一次性标志，避免重复触发。`FirePressed` / `WantsToFire` 已在黑板上预留，但当前开火仍由 `InputSystem` 直接触发 `WeaponSystem`，未经过玩家状态机。
- **动画与状态事件**：`PlayerStateBase.OnEnter` 统一播放动画并广播 `OnPlayerStateChanged`，子类通过 `OnEnterState` / `OnLeaveState` 扩展，无需各自处理。

---

## 1. 当前问题分析

当前 AfterToken 玩家状态机位于 `Assets/GameScripts/HotFix/GameLogic/FSM/Player/`，基于 TEngine 的 `FsmState<PlayerEntity>`。

### 1.1 现状代码

```csharp
// PlayerStateBase.cs
public abstract class PlayerStateBase : FsmState<PlayerEntity>
{
    protected override void OnUpdate(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(fsm, elapseSeconds, realElapseSeconds);

        string nextState = fsm.GetData<string>("NextState");
        if (string.IsNullOrEmpty(nextState)) return;

        fsm.SetData<string>("NextState", null);

        switch (nextState)
        {
            case "Dodge": ChangeState<PlayerDodgeState>(fsm); break;
            case "Reload": ChangeState<PlayerReloadState>(fsm); break;
            case "Dead": ChangeState<PlayerDeadState>(fsm); break;
        }
    }
}
```

```csharp
// PlayerIdleState.cs
protected override void OnUpdate(IFsm<PlayerEntity> fsm, float elapse, float real)
{
    base.OnUpdate(fsm, elapse, real);

    var owner = fsm.Owner;
    if (owner.MoveDirection.sqrMagnitude > 0.001f)
    {
        ChangeState<PlayerMoveState>(fsm);
    }
}
```

### 1.2 主要问题

| 问题 | 说明 |
|---|---|
| 切换逻辑分散 | `PlayerStateBase` 里有 `switch case`，各状态里也有 `ChangeState`，全局打断条件散在各处。 |
| 全局打断难维护 | 死亡/闪避/换弹中断等逻辑需要每个状态自己处理或监听事件，新增状态容易遗漏。 |
| 输入与状态耦合 | 状态直接读 `PlayerEntity.MoveDirection`，输入系统与状态机没有清晰边界。 |
| 切换请求硬编码 | `"NextState"` 是字符串，编译期无法检查，新增状态要改多处。 |
| 状态职责不纯 | `PlayerReloadState` 里监听武器切换事件、自己管理 Timer，逻辑越界。 |

---

## 2. 目标架构

借鉴 BBB-Nexus 的「黑板 + 拦截器 + 统一驱动」思想，但保留 TEngine FSM 和现有动画/物理系统。

```
InputSystem                    PlayerEntity
     │                              │
     ▼                              ▼
PlayerStateContext ◄────── PlayerStateMachineDriver
(黑板/意图/参数)                (统一驱动/拦截器管理)
     │                              │
     └──────────► TEngine FSM ◄─────┘
                     │
        ┌────────────┼────────────┐
        ▼            ▼            ▼
   PlayerIdleState  PlayerMoveState  ...
        │            │
        ▼            ▼
   只写 OnUpdateLogic   只写 OnUpdateLogic
   不处理全局打断       不处理全局打断
```

### 2.1 核心原则

1. **保留 TEngine FSM**：不改底层状态机，降低迁移成本。
2. **单一驱动源**：由 `PlayerStateMachineDriver` 统一调用 FSM 更新、统一处理切换请求。
3. **黑板解耦**：`PlayerStateContext` 承载输入、意图、运行时参数，状态机只读黑板。
4. **拦截器机制**：全局切换条件（死亡、闪避、换弹中断等）统一成 `PlayerStateInterceptor`。
5. **状态职责单一**：派生状态只实现自身逻辑，不处理全局打断，不直接调用 `ChangeState`。

---

## 3. 核心类设计

### 3.1 PlayerStateContext（黑板）

挂载在 `PlayerEntity` 上，每帧由 `InputSystem` 和 `PlayerStateMachineDriver` 更新。

```csharp
public class PlayerStateContext
{
    // 输入（InputSystem 每帧写入）
    public Vector2 MoveInput;
    public Vector2 AimInput;
    public bool FirePressed;
    public bool ReloadPressed;
    public bool DodgePressed;

    // 意图（InputSystem / WeaponSystem / BattleSystem 写入）
    public bool WantsToDodge;
    public bool WantsToReload;
    public bool WantsToFire;

    // 运行时状态（各系统写入）
    public bool IsDead;
    public bool IsReloading;
    public bool IsDodging;
    public bool IsAiming;
    public bool CanMove => !IsDead && !IsDodging;
    public bool CanFire => !IsDead && !IsReloading && !IsDodging;

    // 当前武器引用
    public WeaponInstance CurrentWeapon;

    // 请求：由状态写入，Driver 统一消费
    public StateTransitionRequest PendingRequest { get; set; }

    public void ResetIntent()
    {
        WantsToDodge = false;
        WantsToReload = false;
        WantsToFire = false;
    }
}
```

### 3.2 StateTransitionRequest（切换请求）

```csharp
public class StateTransitionRequest
{
    public Type TargetStateType;
    public int Priority;
    public object UserData;

    public StateTransitionRequest(Type targetStateType, int priority = 0, object userData = null)
    {
        TargetStateType = targetStateType;
        Priority = priority;
        UserData = userData;
    }
}
```

### 3.3 PlayerStateInterceptor（拦截器接口）

```csharp
public abstract class PlayerStateInterceptor
{
    /// <summary>
    /// 拦截器优先级，数值越大越先执行。
    /// </summary>
    public virtual int Priority => 0;

    /// <summary>
    /// 尝试拦截当前状态切换。
    /// </summary>
    /// <param name="context">玩家黑板</param>
    /// <param name="currentStateType">当前状态类型</param>
    /// <param name="request">输出：希望切换到的目标状态请求</param>
    /// <returns>是否拦截成功</returns>
    public abstract bool TryIntercept(
        PlayerStateContext context,
        Type currentStateType,
        out StateTransitionRequest request);
}
```

### 3.4 PlayerStateBase（改造后基类）

```csharp
public abstract class PlayerStateBase : FsmState<PlayerEntity>
{
    public abstract string StateName { get; }

    protected PlayerStateContext Context => Owner.Context;
    protected PlayerEntity Owner { get; private set; }

    protected override void OnEnter(IFsm<PlayerEntity> fsm)
    {
        Owner = fsm.Owner;
        Owner.PlayAnimation(StateName);

        string prev = fsm.GetData<string>("PrevState") ?? "None";
        GameEvent.Get<IPlayerEvent>().OnPlayerStateChanged(StateName, prev);

        OnEnterState(fsm);
    }

    protected override void OnLeave(IFsm<PlayerEntity> fsm, bool isShutdown)
    {
        fsm.SetData("PrevState", StateName);
        OnLeaveState(fsm, isShutdown);
    }

    /// <summary>
    /// 密封 Update：先处理拦截器/请求，再执行状态自身逻辑。
    /// </summary>
    protected sealed override void OnUpdate(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds)
    {
        Owner = fsm.Owner;

        // 1. 检查拦截器（全局打断）
        if (PlayerStateMachineDriver.Instance.TryGetInterruptRequest(Owner.Context, GetType(), out var interruptRequest))
        {
            if (interruptRequest?.TargetStateType != null && interruptRequest.TargetStateType != GetType())
            {
                ChangeState(fsm, interruptRequest.TargetStateType);
                return;
            }
        }

        // 2. 消费状态自己发出的请求
        if (TryConsumePendingRequest(fsm))
            return;

        // 3. 执行状态自身逻辑
        OnUpdateState(fsm, elapseSeconds, realElapseSeconds);
    }

    private bool TryConsumePendingRequest(IFsm<PlayerEntity> fsm)
    {
        var req = Context.PendingRequest;
        if (req == null) return false;

        Context.PendingRequest = null;

        if (req.TargetStateType == GetType())
            return false; // 已经是目标状态

        ChangeState(fsm, req.TargetStateType);
        return true;
    }

    /// <summary>
    /// 请求切换到指定状态。由 Driver 在本帧统一处理。
    /// </summary>
    protected void RequestState<T>(int priority = 0, object userData = null) where T : PlayerStateBase
    {
        Context.PendingRequest = new StateTransitionRequest(typeof(T), priority, userData);
    }

    protected virtual void OnEnterState(IFsm<PlayerEntity> fsm) { }
    protected virtual void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown) { }
    protected virtual void OnUpdateState(IFsm<PlayerEntity> fsm, float elapseSeconds, float realElapseSeconds) { }
}
```

### 3.5 PlayerStateMachineDriver（统一驱动）

```csharp
public class PlayerStateMachineDriver
{
    public static PlayerStateMachineDriver Instance { get; } = new PlayerStateMachineDriver();

    private readonly List<PlayerStateInterceptor> _interceptors = new List<PlayerStateInterceptor>();

    public PlayerStateMachineDriver()
    {
        // 按优先级注册全局拦截器
        RegisterInterceptor(new DeathInterceptor());      // 最高优先级
        RegisterInterceptor(new DodgeInterceptor());      // 闪避打断
        RegisterInterceptor(new ReloadStartInterceptor());// 换弹请求
    }

    public void RegisterInterceptor(PlayerStateInterceptor interceptor)
    {
        _interceptors.Add(interceptor);
        _interceptors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// 每帧由 PlayerSystem 调用，更新黑板意图。
    /// 注意：TEngine FsmModule 会自动轮询 FSM，本类不驱动 FSM Update。
    /// </summary>
    public void UpdateContext(PlayerStateContext context)
    {
        if (context == null) return;

        // 更新黑板意图
        context.ResetIntent();

        context.WantsToDodge = context.DodgePressed;
        context.WantsToReload = context.ReloadPressed &&
                                context.CurrentWeapon != null &&
                                context.CurrentWeapon.CurrentAmmo < context.CurrentWeapon.Config.clipSize &&
                                !context.CurrentWeapon.IsReloading;
        context.WantsToFire = context.FirePressed;

        // 消费一次性输入标志，避免重复触发
        context.DodgePressed = false;
        context.ReloadPressed = false;
        context.FirePressed = false;
    }

    /// <summary>
    /// 由 PlayerStateBase.OnUpdate 调用，尝试获取全局拦截器产生的切换请求。
    /// 实际切换由 PlayerStateBase 内部执行，因为 FsmState.ChangeState 是受保护方法。
    /// </summary>
    public bool TryGetInterruptRequest(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
    {
        request = null;
        if (context == null || currentStateType == null) return false;

        foreach (var interceptor in _interceptors)
        {
            if (interceptor == null) continue;

            if (interceptor.TryIntercept(context, currentStateType, out request))
            {
                if (request?.TargetStateType == currentStateType)
                {
                    request = null;
                    continue;
                }

                return true;
            }
        }

        return false;
    }
}
```

**注意**：TEngine 的 `IFsm<T>` 泛型 `ChangeState<TState>()` 需要用反射调用。如果 TEngine 提供非泛型接口则更好。若不支持，可在 Driver 中维护一个 `Dictionary<Type, Action<IFsm<PlayerEntity>>>` 的委托表来避免反射开销。

---

## 4. 拦截器实现示例

### 4.1 DeathInterceptor（死亡强制打断）

```csharp
public class DeathInterceptor : PlayerStateInterceptor
{
    public override int Priority => 1000;

    public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
    {
        request = null;

        if (!context.IsDead) return false;
        if (currentStateType == typeof(PlayerDeadState)) return false;

        request = new StateTransitionRequest(typeof(PlayerDeadState), Priority);
        return true;
    }
}
```

### 4.2 DodgeInterceptor（闪避请求）

```csharp
public class DodgeInterceptor : PlayerStateInterceptor
{
    public override int Priority => 100;

    public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
    {
        request = null;

        if (!context.WantsToDodge) return false;
        if (!context.CanMove) return false; // 死亡时不能闪避
        if (currentStateType == typeof(PlayerDodgeState)) return false;

        request = new StateTransitionRequest(typeof(PlayerDodgeState), Priority);
        return true;
    }
}
```

### 4.3 ReloadStartInterceptor（换弹请求）

```csharp
public class ReloadStartInterceptor : PlayerStateInterceptor
{
    public override int Priority => 50;

    public override bool TryIntercept(PlayerStateContext context, Type currentStateType, out StateTransitionRequest request)
    {
        request = null;

        if (!context.WantsToReload) return false;
        if (currentStateType == typeof(PlayerReloadState)) return false;
        if (currentStateType == typeof(PlayerDeadState)) return false;
        if (currentStateType == typeof(PlayerDodgeState)) return false;

        // 这里也可以判断当前武器是否需要换弹
        if (context.CurrentWeapon == null || context.CurrentWeapon.CurrentAmmo >= context.CurrentWeapon.Config.magazineSize)
            return false;

        request = new StateTransitionRequest(typeof(PlayerReloadState), Priority);
        return true;
    }
}
```

---

## 5. 改造后的状态示例

### 5.1 PlayerIdleState

```csharp
public class PlayerIdleState : PlayerStateBase
{
    public override string StateName => "Idle";

    protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
    {
        if (Context.MoveInput.sqrMagnitude > 0.001f)
        {
            RequestState<PlayerMoveState>();
        }
    }
}
```

### 5.2 PlayerMoveState

```csharp
public class PlayerMoveState : PlayerStateBase
{
    public override string StateName => "Move";

    protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
    {
        if (Context.MoveInput.sqrMagnitude <= 0.001f)
        {
            RequestState<PlayerIdleState>();
        }
    }
}
```

### 5.3 PlayerDodgeState

```csharp
public class PlayerDodgeState : PlayerStateBase
{
    public override string StateName => "Dodge";
    private float _elapsed;

    protected override void OnEnterState(IFsm<PlayerEntity> fsm)
    {
        _elapsed = 0f;
        Owner.StartDodge();
        Context.IsDodging = true;
    }

    protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
    {
        _elapsed += elapse;
        if (_elapsed >= Owner.DodgeDuration)
        {
            RequestState<PlayerIdleState>();
        }
    }

    protected override void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown)
    {
        Owner.EndDodge();
        Context.IsDodging = false;
    }
}
```

### 5.4 PlayerReloadState

```csharp
public class PlayerReloadState : PlayerStateBase
{
    public override string StateName => "Reload";

    private IFsm<PlayerEntity> _fsm;

    protected override void OnEnterState(IFsm<PlayerEntity> fsm)
    {
        _fsm = fsm;

        GameEvent.AddEventListener<int, bool>(IWeaponEvent_Event.OnReloadStateChanged, OnReloadStateChanged);

        Context.IsReloading = true;
        Context.CurrentWeapon?.Reload(Owner.GetInstanceID());
    }

    protected override void OnUpdateState(IFsm<PlayerEntity> fsm, float elapse, float real)
    {
        // 武器实例负责计时，状态机只等待完成事件
    }

    protected override void OnLeaveState(IFsm<PlayerEntity> fsm, bool isShutdown)
    {
        GameEvent.RemoveEventListener<int, bool>(IWeaponEvent_Event.OnReloadStateChanged, OnReloadStateChanged);
        _fsm = null;
        Context.IsReloading = false;
    }

    private void OnReloadStateChanged(int ownerId, bool isReloading)
    {
        if (isReloading) return;
        if (_fsm != null && _fsm.IsRunning && _fsm.CurrentState == this)
        {
            RequestState<PlayerIdleState>();
        }
    }
}
```

**注意**：换弹中切枪中断换弹的逻辑，由 `WeaponSystem.SwitchToSlot` 调用 `CancelReload`，武器实例会广播 `OnReloadStateChanged(false)`，`PlayerReloadState` 收到后切回 Idle。

### 5.5 PlayerDeadState

```csharp
public class PlayerDeadState : PlayerStateBase
{
    public override string StateName => "Dead";

    protected override void OnEnterState(IFsm<PlayerEntity> fsm)
    {
        Owner.SetDead();
        GameEvent.Get<IPlayerEvent>().OnPlayerDied();
    }
}
```

---

## 6. PlayerSystem 改造

```csharp
public class PlayerSystem : MonoBehaviour
{
    private IFsm<PlayerEntity> _playerFsm;

    private void Awake()
    {
        // ... 订阅输入事件 ...
        _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnMoveInput, OnMoveInput);
        _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnAimInput, OnAimInput);
        _eventMgr.AddEvent(IBattleInputEvent_Event.OnReloadPressed, OnReloadPressed);
        _eventMgr.AddEvent(IBattleInputEvent_Event.OnDodgePressed, OnDodgePressed);
    }

    private async UniTask CreatePlayerAsync()
    {
        // ... 创建 PlayerEntity ...
        _playerEntity.ResetEntity();

        // 创建黑板
        _playerEntity.Context = new PlayerStateContext();

        // 创建 FSM
        _playerFsm = GameModule.Fsm.CreateFsm<PlayerEntity>(
            "PlayerFsm",
            _playerEntity,
            new PlayerIdleState(),
            new PlayerMoveState(),
            new PlayerDodgeState(),
            new PlayerReloadState(),
            new PlayerDeadState()
        );

        _playerFsm.Start<PlayerIdleState>();
    }

    private void OnMoveInput(Vector2 direction)
    {
        if (_playerEntity == null || _playerEntity.IsDead) return;
        _playerEntity.SetMoveDirection(direction);
        if (_playerEntity.Context != null)
        {
            _playerEntity.Context.MoveInput = direction;
        }
    }

    private void OnAimInput(Vector2 worldPosition)
    {
        if (_playerEntity == null || _playerEntity.IsDead) return;
        _playerEntity.SetAimPosition(worldPosition);
        if (_playerEntity.Context != null)
        {
            _playerEntity.Context.AimInput = worldPosition;
        }
    }

    private void OnReloadPressed()
    {
        if (_playerEntity?.Context == null) return;
        _playerEntity.Context.ReloadPressed = true;
    }

    private void OnDodgePressed()
    {
        if (_playerEntity?.Context == null) return;
        _playerEntity.Context.DodgePressed = true;
    }

    private void Update()
    {
        if (_playerEntity?.Context == null) return;

        // 同步当前武器引用到黑板
        _playerEntity.Context.CurrentWeapon = WeaponSystem.Instance?.CurrentWeapon;

        // 更新黑板意图
        PlayerStateMachineDriver.Instance.UpdateContext(_playerEntity.Context);

        // 更新移动速度
        if (!_playerEntity.IsDead && _playerFsm?.CurrentState is not PlayerDodgeState)
        {
            float multiplier = WeaponSystem.Instance?.GetCurrentMoveSpeedMultiplier() ?? 1f;
            _playerEntity.MoveSpeed = _playerEntity.BaseMoveSpeed * multiplier;
        }
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        // ... 扣血逻辑 ...
        if (_currentHp <= 0 && _playerEntity?.Context != null)
        {
            _playerEntity.Context.IsDead = true;
        }
    }
}
```

---

## 7. 输入系统

`InputSystem` 保持原有的事件广播机制不变。`PlayerSystem` 订阅输入事件后将输入写入 `PlayerStateContext`：

```csharp
// InputSystem.cs（保持不变）
private void HandleReloadInput()
{
    if (Input.GetKeyDown(_reloadKey))
    {
        BattleInputEvent?.OnReloadPressed();
    }
}
```

这种设计保持了解耦：`InputSystem` 不需要知道 `PlayerEntity` 或 `PlayerStateContext` 的存在。

---

## 8. 实施步骤

### 阶段 1：基础搭建（半天）

1. 新增 `PlayerStateContext.cs`。
2. 新增 `StateTransitionRequest.cs`。
3. 新增 `PlayerStateInterceptor.cs`。
4. 新增 `PlayerStateMachineDriver.cs`。
5. 在 `PlayerEntity` 上添加 `Context` 字段。

### 阶段 2：改造 PlayerStateBase（半天）

1. 密封 `OnUpdate`，加入拦截器检查和请求消费逻辑。
2. 新增 `RequestState<T>()` 方法。
3. 拆分 `OnEnter` / `OnLeave` 为 `OnEnterState` / `OnLeaveState`。

### 阶段 3：实现全局拦截器（半天）

1. 实现 `DeathInterceptor`。
2. 实现 `DodgeInterceptor`。
3. 实现 `ReloadStartInterceptor`。
4. 在 `PlayerStateMachineDriver` 中注册。

### 阶段 4：改造各状态（1 天）

1. `PlayerIdleState`：改为检测 `Context.MoveInput`，用 `RequestState<PlayerMoveState>()`。
2. `PlayerMoveState`：改为检测 `Context.MoveInput`，用 `RequestState<PlayerIdleState>()`。
3. `PlayerDodgeState`：移除 Timer，改为在 `OnUpdateState` 中累加时间。
4. `PlayerReloadState`：由武器实例管理换弹计时，状态机监听 `OnReloadStateChanged(false)` 后切回 Idle。
5. `PlayerDeadState`：保持不变。

### 阶段 5：接入输入和 PlayerSystem（半天）

1. `InputSystem` 保持事件广播不变。
2. `PlayerSystem` 订阅事件并将输入写入 `PlayerStateContext`。
3. `PlayerSystem.Update` 中调用 `PlayerStateMachineDriver.Instance.UpdateContext`。
4. 移除旧的 `NextState` 字符串机制。

### 阶段 6：验证（半天）

1. 验证 Idle ↔ Move 切换。
2. 验证死亡打断任何状态。
3. 验证闪避打断移动/换弹。
4. 验证换弹中切枪中断换弹。
5. 验证时间缩放为 0 时状态机不推进。

---

## 9. 预期收益

| 收益 | 说明 |
|---|---|
| 切换逻辑集中 | 所有全局打断条件在拦截器中统一管理，新增状态无需重复写死亡/闪避判断。 |
| 输入与状态解耦 | 状态机只读 `PlayerStateContext`，不直接依赖 `Input` 或 `PlayerEntity` 的具体字段。 |
| 扩展性增强 | 新增状态只需继承 `PlayerStateBase` 并实现 `OnUpdateState`，注册拦截器即可。 |
| 调试更直观 | `PlayerStateMachineDriver` 统一输出状态切换日志，便于排查问题。 |
| 为敌人 AI 做准备 | 未来敌人也可以共享 `PlayerStateInterceptor` 和黑板思想，统一处理受击/死亡打断。 |

---

## 10. 风险提示

| 风险 | 缓解方案 |
|---|---|
| `FsmState.ChangeState` 是受保护方法，Driver 无法直接调用 | 由 Driver 返回切换请求，PlayerStateBase 内部调用 `ChangeState`。 |
| 时间缩放为 0 时状态机仍推进 | 输入系统已在 `Time.timeScale <= 0` 时跳过战斗输入；FsmModule 会自动跳过无 DeltaTime 的更新。 |
| 状态内 still 使用 Timer 的遗留代码 | 一次性迁移完成，不要保留两套机制。 |

---

## 11. 与 BBB-Nexus 的差异

| BBB-Nexus | AfterToken 方案 | 原因 |
|---|---|---|
| 自定义极简 StateMachine | 保留 TEngine FsmState | 不改底层，降低迁移成本 |
| 全身 + 上半身 + Override 分层 | 单一玩家 FSM | 2D 项目无骨骼分层需求 |
| Animancer + Avatar Mask | 继续使用 Animator Sprite | 2D 俯视角项目 |
| CharacterController + MotionDriver | 继续使用 Rigidbody2D | 2D 物理 |
| 意图处理器管线 | 简化为 InputSystem 写入 Context | 项目阶段不需要复杂管线 |
| ScriptableObject 拦截器 | 普通 C# 类拦截器 | 减少配置资产，先代码驱动 |

---

## 12. 结论

本方案在保留 AfterToken 现有 TEngine FSM、Animator、Rigidbody2D 的前提下，引入 BBB-Nexus 的**黑板 + 拦截器 + 统一驱动**思想，解决当前状态机切换逻辑分散、全局打断难维护的问题。

**推荐立即实施**。先从阶段 1-3 开始，验证 `DeathInterceptor` 和 `DodgeInterceptor` 的效果，再逐步迁移各状态。


---

## 13. 调试状态 UI 实施记录

> 以下内容用于记录本次重构期间添加的临时调试 UI，方便后续删除。

### 13.1 调试 UI 用途

在 `BattleMainUI` 中动态创建一个 `Text` 组件，实时显示玩家当前 FSM 状态（Idle / Move / Dodge / Reload / Dead），便于验证状态机切换是否正确。

### 13.2 代码位置

- `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs`
  - 字段：`private Text _textState;`（标记为 `[DEBUG]`）
  - 方法：`CreateDebugStateText()`（标记为 `[DEBUG]`）
  - 方法：`OnPlayerStateChanged(string currentState, string prevState)`（标记为 `[DEBUG]`）
  - 事件注册：`AddUIEvent<string, string>(IPlayerEvent_Event.OnPlayerStateChanged, OnPlayerStateChanged);`

### 13.3 运行时表现

- 在 HUD 左下角（HP 信息上方）显示黄色文字：`State: Idle`。
- 状态切换时文字实时刷新。

### 13.4 删除步骤

后续状态机稳定后，按以下步骤删除调试 UI：

1. 删除 `BattleMainUI.cs` 中的 `_textState` 字段。
2. 删除 `CreateDebugStateText()` 方法。
3. 删除 `OnPlayerStateChanged()` 方法。
4. 删除 `RegisterEvent()` 中的 `OnPlayerStateChanged` 事件注册。
5. 删除 `OnCreate()` 中对 `CreateDebugStateText()` 的调用。

> 注意：不要删除 `IPlayerEvent.OnPlayerStateChanged` 事件本身，该事件可能用于其他系统监听。
