# BBB-Nexus 状态机切换学习笔记

> 仓库：https://github.com/bunkerboy258/BBB-Nexus.git  
> 学习方式：本地 `git clone --depth 1` 后精读核心源码  
> 目标：理解其状态机切换机制，并评估向 AfterToken 迁移的可行性。

---

## 1. 整体印象

BBB-Nexus 不是一套简单的 FSM，而是一套**面向复杂动作游戏的角色控制器框架**。状态机只是其中一环，周围还有输入管线、黑板、动画外观层、运动驱动器、IK、音频、装备、仲裁器等子系统。

其核心思想：
- **数据至上**：所有子系统通过 `PlayerRuntimeData` 黑板共享状态，不直接互相引用。
- **意图与参数分离**：输入先被翻译成瞬时意图（Intent），再被数学运算成连续参数（Parameter），最后状态机读取意图做切换。
- **配置驱动**：状态名单、全局拦截器、上半身状态全部挂在 `PlayerBrainSO` 上，可视化配置。
- **单入口 Tick**：`BBBCharacterController` 统一驱动所有子系统，避免零散 Update。

---

## 2. 状态机核心 API（极简）

```csharp
// Core/StateMachine/StateMachine.cs
public class StateMachine
{
    public BaseState CurrentState { get; private set; }

    public void Initialize(BaseState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(BaseState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}
```

```csharp
// Core/StateMachine/BaseState.cs
public abstract class BaseState
{
    public abstract void Enter();
    public abstract void LogicUpdate();
    public abstract void PhysicsUpdate();
    public abstract void Exit();
}
```

**点评**：状态机核心不到 30 行，没有任何多余设计。真正的复杂度被推到上层架构（拦截器、黑板、管线）。

---

## 3. 单入口 Tick 时序

```csharp
// BBBCharacterController.Update()
ArbiterPipeline.ProcessUpdateArbiters();
InputPipeline.Update();
MainProcessorPipeline.UpdateIntentProcessors();   // 输入 -> 意图 -> 黑板
InventoryController.Update();
MainProcessorPipeline.UpdateParameterProcessors(); // 黑板意图 -> 动画参数
StateMachine.CurrentState.LogicUpdate();           // 全身状态机
UpperBodyController.Update();                      // 上半身状态机
FacialController.Update();
ActionController.Update();
AudioController.Update();

// BBBCharacterController.LateUpdate()
StateMachine.CurrentState.PhysicsUpdate();         // 运动/物理
IkController.Update();
ArbiterPipeline.ProcessLateUpdateArbiters();
RuntimeData.ResetIntetnt();                        // 每帧清理意图
```

**关键设计**：物理位移放在 `LateUpdate`，因为 `Animator` 骨骼结算在 `Update` 之后、`LateUpdate` 之前，这样可以拿到本帧动画时间再算位移，避免滑步/抽搐。

---

## 4. 意图驱动 + 黑板模式

### 4.1 黑板：PlayerRuntimeData

```csharp
public class PlayerRuntimeData
{
    // 输入状态
    public Vector2 LookInput;
    public Vector2 MoveInput;

    // 运动状态
    public LocomotionState LastLocomotionState;
    public LocomotionState CurrentLocomotionState;
    public Vector3 DesiredWorldMoveDir;

    // 帧级意图（每帧清理）
    public bool WantToRun;
    public bool WantsToDodge;
    public bool WantsToRoll;
    public bool WantsToJump;
    public bool WantsToFire;
    public bool WantsToAction;

    // 动画播放选项覆写
    public AnimPlayOptions? NextStatePlayOptions;
}
```

### 4.2 输入 -> 意图 -> 参数

```csharp
// MainProcessorPipeline.UpdateIntentProcessors()
ref readonly ProcessedInputData inputSnapshot = ref _inputPipeline.Current.currentFrameData.Processed;
_viewRotationProcessor.Update(in inputSnapshot);
_aimIntentProcessor.Update(in inputSnapshot);
_aimIntentProcessor.Update(in inputSnapshot);
_locomotionIntentProcessor.Update(in inputSnapshot);
_jumpOrVaultIntentProcessor.Update(in inputSnapshot);
// ...

// MainProcessorPipeline.UpdateParameterProcessors()
_movementParameterProcessor.Update();
```

**点评**：状态机不需要知道输入从哪里来，只需要读 `WantsToRoll`、`CurrentLocomotionState` 等意图/状态。AI 和玩家可以共享同一套状态机。

---

## 5. 拦截器驱动的全局打断

### 5.1 PlayerBaseState 封印 LogicUpdate

```csharp
public abstract class PlayerBaseState : BaseState
{
    public sealed override void LogicUpdate()
    {
        if (CheckInterrupts()) return;   // 先走全局/上半身拦截器
        UpdateStateLogic();              // 再执行状态自身逻辑
    }

    protected virtual bool CheckInterrupts()
    {
        return player.InterruptProcessor.TryProcessInterrupts(this);
    }

    protected abstract void UpdateStateLogic();
}
```

### 5.2 全局拦截处理器

```csharp
public class GlobalInterruptProcessor
{
    public bool TryProcessInterrupts(PlayerBaseState currentState)
    {
        var pipeline = _player.Config.Brain.GlobalInterceptors;
        for (int i = 0; i < pipeline.Count; i++)
        {
            var interceptor = pipeline[i];
            if (interceptor != null && interceptor.TryIntercept(_player, currentState, out var nextState))
            {
                _player.StateMachine.ChangeState(nextState);
                return true;
            }
        }
        return false;
    }
}
```

### 5.3 拦截器接口

```csharp
public abstract class StateInterceptorSO : ScriptableObject
{
    public abstract bool TryIntercept(
        BBBCharacterController player,
        PlayerBaseState currentState,
        out PlayerBaseState nextState);
}
```

### 5.4 Roll 拦截器示例

```csharp
[CreateAssetMenu(fileName = "RollInterceptor", menuName = "BBBNexus/Player/Interceptors/Roll")]
public class RollInterceptorSO : StateInterceptorSO
{
    public override bool TryIntercept(BBBCharacterController player, PlayerBaseState currentState, out PlayerBaseState nextState)
    {
        nextState = null;
        var data = player.RuntimeData;

        if (data.WantsToRoll)
        {
            data.NextStatePlayOptions = data.LastLocomotionState == LocomotionState.Sprint
                ? player.Config.LocomotionAnims.FadeInMoveDodgeOptions
                : player.Config.LocomotionAnims.FadeInQuickDodgeOptions;

            nextState = player.StateRegistry.GetState<PlayerRollState>();
            return true;
        }

        return false;
    }
}
```

**点评**：传统 FSM 中每个状态都要判断「是否翻滚/跳跃/死亡」，BBB-Nexus 把这些全局条件抽出成可配置的 SO，避免代码重复。但代价是切换条件的可读性被分散到多个拦截器文件中。

---

## 6. 状态注册表 + BrainSO 配置

### 6.1 PlayerBrainSO

```csharp
[CreateAssetMenu(fileName = "PlayerBrain_Default", menuName = "BBBNexus/Player/Modules/Player Brain")]
public class PlayerBrainSO : ScriptableObject
{
    public List<PlayerStateType> AvailableStates = new List<PlayerStateType>();
    public List<StateInterceptorSO> GlobalInterceptors = new List<StateInterceptorSO>();
    public List<UpperBodyStateType> UpperBodyStates = new List<UpperBodyStateType>();
    public List<UpperBodyInterceptorSO> UpperBodyInterceptors = new List<UpperBodyInterceptorSO>();
}
```

### 6.2 PlayerStateRegistry

```csharp
public class PlayerStateRegistry
{
    private readonly Dictionary<Type, PlayerBaseState> _states = new Dictionary<Type, PlayerBaseState>();
    public PlayerBaseState InitialState { get; private set; }

    public void InitializeFromBrain(PlayerBrainSO brain, BBBCharacterController player)
    {
        for (int i = 0; i < brain.AvailableStates.Count; i++)
        {
            PlayerBaseState newState = brain.AvailableStates[i] switch
            {
                PlayerStateType.Idle => new PlayerIdleState(player),
                PlayerStateType.Roll => new PlayerRollState(player),
                PlayerStateType.Override => new OverrideState(player),
                PlayerStateType.Death => new PlayerDeathState(player),
                // ...
                _ => null
            };

            if (newState != null)
            {
                _states.Add(newState.GetType(), newState);
                if (InitialState == null) InitialState = newState;
            }
        }
    }

    public T GetState<T>() where T : PlayerBaseState
    {
        if (_states.TryGetValue(typeof(T), out var state))
            return state as T;
        return null;
    }
}
```

**点评**：状态实例在 `Awake` 时预分配，运行时只切换引用，零 GC。但 `switch` 硬编码了枚举到类型的映射，新增状态需要改注册表。

---

## 7. 具体状态示例

### 7.1 PlayerIdleState

```csharp
public class PlayerIdleState : PlayerBaseState
{
    public override void Enter()
    {
        ChooseOptionsAndPlay(config.LocomotionAnims.IdleAnim);
    }

    protected override void UpdateStateLogic()
    {
        // 跳跃等由全局拦截器处理，这里只处理移动
        if (data.CurrentLocomotionState != LocomotionState.Idle)
        {
            data.NextStatePlayOptions = ...; // 根据 Walk/Jog/Sprint 选过渡
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerMoveStartState>());
        }
    }

    public override void PhysicsUpdate()
    {
        player.MotionDriver.UpdateMotion();
    }

    public override void Exit() { }
}
```

### 7.2 PlayerRollState（禁用全局打断）

```csharp
public class PlayerRollState : PlayerBaseState
{
    protected override bool CheckInterrupts() => false; // 翻滚中不可被打断

    public override void Enter()
    {
        data.WantsToRoll = false;
        data.SfxQueue.Enqueue(PlayerSfxEvent.Roll);

        _selectedData = GetRollData(); // 8 方向选择
        player.MotionDriver.InitializeWarpData(_selectedData);
        ChooseOptionsAndPlay(_selectedData.Clip);
    }

    public override void PhysicsUpdate()
    {
        float normalizedTime = player.AnimFacade.CurrentNormalizedTime;
        player.MotionDriver.UpdateWarpMotion(normalizedTime);

        if (!_endTimeTriggered && _stateDuration >= _selectedData.EndTime)
        {
            _endTimeTriggered = true;
            HandleRollEnd();
        }
    }

    public override void Exit()
    {
        data.WantsToRoll = false;
        player.MotionDriver.ClearWarpData();
        player.AnimFacade.ClearOnEndCallback();
    }
}
```

**点评**：翻滚这种需要精确位移的状态，通过 `MotionDriver.InitializeWarpData` + `UpdateWarpMotion` 实现动画时间驱动的 Warp 运动。

---

## 8. Override 强制覆盖层

```csharp
public sealed class OverrideState : PlayerBaseState
{
    protected override bool CheckInterrupts() => false; // Override 状态内不可被打断

    public override void Enter()
    {
        data.Arbitration.BlockInventory = true;
        Apply();
    }

    protected override void UpdateStateLogic()
    {
        if (!_applied) Apply();
    }

    private void Apply()
    {
        if (!data.Override.IsActive) return;
        var req = data.Override.Request;
        AnimFacade.PlayFullBodyAction(req.Clip, req.FadeDuration);
        AnimFacade.SetOverrideOnEndCallback(OnClipEnd);
    }

    private void OnClipEnd()
    {
        if (data.Override.ReturnState != null)
        {
            player.StateMachine.ChangeState(data.Override.ReturnState);
            return;
        }

        if (data.CurrentLocomotionState != LocomotionState.Idle)
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerMoveLoopState>());
        else
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerIdleState>());
    }
}
```

**点评**：Override 用于播放高优先级全身动作（受击、处决、表情），它会阻断普通状态切换、阻断装备切换，并在动画结束后智能返回原状态或移动状态。

---

## 9. 上半身独立状态机

```csharp
public class UpperBodyController
{
    public StateMachine StateMachine { get; private set; }
    public UpperBodyStateRegistry StateRegistry { get; private set; }
    public UpperBodyInterruptProcessor InterruptProcessor { get; private set; }

    public UpperBodyController(BBBCharacterController player)
    {
        StateMachine = new StateMachine();
        StateRegistry = new UpperBodyStateRegistry();
        InterruptProcessor = new UpperBodyInterruptProcessor(player, this);
        StateRegistry.InitializeFromBrain(player.Config.Brain, player);
    }

    public void Update()
    {
        if (_player.RuntimeData.Arbitration.BlockUpperBody) return;
        StateMachine.CurrentState?.LogicUpdate();
    }
}
```

上半身状态与全身状态并行运行，通过 `Avatar Mask` 只影响上半身骨骼，适合持枪、瞄准、换弹、表情等。

---

## 10. 运动驱动器 MotionDriver

核心职责：把输入、动画曲线、Warp 数据转换成 `CharacterController.Move()` 调用。

关键设计：
- **输入驱动分支**：`CalculateInputDrivenVelocity()`，支持自由视角和瞄准模式两种移动。
- **动画曲线驱动分支**：`CalculateCurveVelocity()`，读取动画的旋转/速度曲线驱动角色。
- **Warp 运动分支**：`UpdateWarpMotion()`，用于翻滚/翻越等需要精确位移的动作。
- **重力缓存**：`GetGravityThisFrame()` 用 `Time.frameCount` 避免同帧重复积分。
- **LateUpdate 执行**：等 Animator 结算后再读取动画时间，避免滑步。

---

## 11. 与 AfterToken 当前架构的对比

| 维度 | BBB-Nexus | AfterToken（当前） |
|---|---|---|
| FSM 核心 | 自定义极简 `StateMachine` | GameFramework FSM |
| 状态组织 | 分层：全身 + 上半身 + Override | 单一玩家 FSM |
| 输入处理 | 输入管线 -> 意图 -> 黑板 -> 状态机 | `InputSystem` 直接广播事件 |
| 打断机制 | 配置化 `StateInterceptorSO` | 各状态自行判断 / 事件监听 |
| 动画系统 | Animancer + Avatar Mask | Animator + Sprite |
| 物理移动 | `MotionDriver` + `CharacterController` | `Rigidbody2D` |
| 配置方式 | `PlayerBrainSO` ScriptableObject | 代码硬编码 + Luban 表 |
| 项目阶段 | 复杂 3D 动作游戏 | 2D 俯视角射击原型 |

---

## 12. 可借鉴点与迁移建议

### 12.1 推荐借鉴（低风险、高收益）

1. **拦截器机制**
   - 把「死亡打断一切」、「闪避打断换弹」、「受击打断移动」等全局条件抽出成可配置拦截器。
   - 避免在每个状态的 `OnEnter/OnUpdate` 中重复写死亡判断。

2. **黑板模式（PlayerRuntimeData）**
   - 把玩家当前状态（是否换弹、是否闪避、是否瞄准、当前武器等）集中到一份运行时数据。
   - 输入系统写入意图，状态机读取意图，UI/武器/相机系统读取参数。

3. **状态切换时序统一**
   - 目前 AfterToken 的 `InputSystem`、`WeaponSystem`、`PlayerSystem` 各自 Update，可能出现时序问题。
   - 可考虑由单一 `PlayerController` 统一驱动，或至少明确各系统更新顺序。

### 12.2 谨慎借鉴（需要评估）

1. **上半身独立状态机**
   - AfterToken 是 2D 俯视角，角色没有上下半身骨骼分层。
   - 但可以把「武器层」抽象出来：持枪、瞄准、换弹作为独立层，不影响移动状态。

2. **Override 覆盖层**
   - 适合受击、复活、处决等强制动作。
   - 但 2D 俯视角的受击可能只是一段动画 + 位移，不一定需要完整的 Override 机制。

### 12.3 不推荐直接迁移

1. **完整替换 GameFramework FSM**
   - AfterToken 已深度使用 GameFramework FSM，且当前需求不复杂，替换成本高、收益低。

2. **引入 Animancer**
   - AfterToken 使用 2D Sprite + Animator，Animancer 是 3D 动画方案，不直接适用。

3. **引入 CharacterController + MotionDriver**
   - AfterToken 使用 `Rigidbody2D` 物理，直接迁移 `MotionDriver` 不可行。

---

## 13. 落地建议（最小改动方案）

针对 AfterToken 当前阶段，建议采用**方案 B：保留 GameFramework FSM，引入拦截器 + 黑板模式**。

### 步骤 1：定义玩家运行时黑板

```csharp
public class PlayerRuntimeData
{
    public Vector2 MoveInput;
    public Vector2 AimInput;
    public bool WantsToFire;
    public bool WantsToReload;
    public bool WantsToDodge;
    public bool IsReloading;
    public bool IsAiming;
    public bool IsDead;
    public int CurrentWeaponSlot;
}
```

### 步骤 2：输入系统写入黑板

```csharp
// InputSystem.Update()
PlayerRuntimeData.Instance.MoveInput = ...;
PlayerRuntimeData.Instance.WantsToFire = ...;
PlayerRuntimeData.Instance.WantsToReload = ...;
```

### 步骤 3：定义状态拦截器

```csharp
public abstract class PlayerStateInterceptor : ScriptableObject
{
    public abstract bool TryIntercept(PlayerEntity player, FsmState<PlayerEntity> currentState, out FsmState<PlayerEntity> nextState);
}
```

### 步骤 4：在状态基类中统一检查拦截器

```csharp
public abstract class PlayerBaseState : FsmState<PlayerEntity>
{
    protected sealed override void OnUpdate(IFsm<PlayerEntity> fsm)
    {
        if (CheckInterrupts(fsm)) return;
        OnUpdateState(fsm);
    }

    protected abstract void OnUpdateState(IFsm<PlayerEntity> fsm);
}
```

### 步骤 5：配置全局拦截器

```csharp
[CreateAssetMenu(fileName = "DeathInterceptor", menuName = "AfterToken/Player/DeathInterceptor")]
public class DeathInterceptor : PlayerStateInterceptor
{
    public override bool TryIntercept(PlayerEntity player, FsmState<PlayerEntity> currentState, out FsmState<PlayerEntity> nextState)
    {
        nextState = null;
        if (!player.RuntimeData.IsDead) return false;
        if (currentState is PlayerDeadState) return false;
        nextState = ...; // PlayerDeadState
        return true;
    }
}
```

---

## 14. 结论

BBB-Nexus 的状态机切换机制非常值得学习，尤其是：

1. **拦截器模式**：把全局切换条件从各状态中抽离，提升可维护性。
2. **黑板 + 意图模式**：解耦输入、状态、表现。
3. **单入口 Tick + LateUpdate 物理**：避免时序混乱和滑步。

但对 AfterToken 来说，**不宜完整迁移**，因为项目维度（2D vs 3D）、物理方案（Rigidbody2D vs CharacterController）、动画方案（Animator Sprite vs Animancer）差异较大。

**建议下一步**：先在 AfterToken 中做一个最小原型，仅给玩家状态机增加「死亡/闪避打断换弹」的拦截器机制，验证效果后再决定是否扩大范围。
