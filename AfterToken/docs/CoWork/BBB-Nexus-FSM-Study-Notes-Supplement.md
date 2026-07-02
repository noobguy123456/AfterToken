# BBB-Nexus 状态机切换学习笔记 — 补充调研

> 本报告是对 `BBB-Nexus-FSM-Study-Notes.md` 的补充，重点覆盖仲裁器系统、装备系统、输入系统、动画外观层以及更多状态示例。

---

## 1. 仲裁器系统（ArbiterPipeline）

`ArbiterPipeline` 与状态机并行运行，负责处理**冲突请求、优先级仲裁和全局状态变更**。它在 `BBBCharacterController.Update()` 的最开始执行，确保状态机读取的是本帧已仲裁后的数据。

```csharp
public class ArbiterPipeline
{
    public LODArbiter LOD { get; private set; }
    public HealthArbiter Health { get; private set; }
    public ActionArbiter Action { get; private set; }
    public StaminaArbiter Stamina { get; private set; }

    public void ProcessUpdateArbiters()
    {
        Action.Arbitrate();
        Health.Arbitrate();
        Stamina.Arbitrate();
        if (_player.EnableLODArbiter) LOD.Arbitrate();
    }
}
```

### 1.1 ActionArbiter — 动作优先级仲裁

核心作用：决定是否有高优先级动作需要切入 `OverrideState`。

```csharp
public class ActionArbiter
{
    public void Arbitrate()
    {
        if (!_data.ActionArbitration.HasRequest) return;

        var request = _data.ActionArbitration.HighestPriorityRequest;
        int currentResistance = GetCurrentOverrideResistance();
        bool isInOverride = _player.StateMachine.CurrentState is OverrideState;

        if (!isInOverride)
        {
            if (request.Priority <= currentResistance) return;

            _data.Override.IsActive = true;
            _data.Override.Request = request;
            _data.Override.ReturnState = _player.StateMachine.CurrentState;
            _player.StateMachine.ChangeState(_player.StateRegistry.GetState<OverrideState>());
            return;
        }

        // 已在 Override 中：同优先级刷新，更高优先级覆盖
        if (request.Priority < currentResistance) return;
        if (_data.Override.Request.Clip == request.Clip) return;

        _data.Override.Request = request;
        ((OverrideState)_player.StateMachine.CurrentState).ForceReapply();
    }

    private int GetCurrentOverrideResistance()
    {
        if (current is OverrideState s) return s.CurrentPriority;
        if (current is PlayerRollState) return 100;
        if (current is PlayerDodgeState) return 80;
        return 0;
    }
}
```

**设计亮点**：
- 普通状态可以被 Override 切入，但 Roll/Dodge 有较高抵抗力（100/80）。
- 同优先级、同 Clip 不重复触发，避免动作重播。
- 已在 Override 中时，更高优先级请求会调用 `ForceReapply()` 无缝切换。

### 1.2 HealthArbiter — 生命值与死亡

```csharp
public class HealthArbiter
{
    private DamageRequest[] _damageQueue = new DamageRequest[16];
    private int _head = 0;
    private int _tail = 0;

    internal void Enqueue(in DamageRequest request)
    {
        if (_data.IsDead) return;
        _damageQueue[_tail] = request;
        _tail = (_tail + 1) % _damageQueue.Length;
    }

    public void Arbitrate()
    {
        if (_data.IsDead || _head == _tail) return;

        while (_head != _tail)
        {
            ref var req = ref _damageQueue[_head];
            _data.CurrentHealth -= req.Amount;
            _head = (_head + 1) % _damageQueue.Length;
        }

        if (_data.CurrentHealth <= 0)
        {
            _data.CurrentHealth = 0;
            _data.IsDead = true;
            _data.Arbitration.BlockInput = true;
            _data.Arbitration.BlockUpperBody = true;
            _data.Arbitration.BlockFacial = true;
            _data.Arbitration.BlockIK = true;
            _data.Arbitration.BlockInventory = true;

            _player.StateMachine.ChangeState(_player.StateRegistry.GetState<PlayerDeathState>());
        }
    }
}
```

**设计亮点**：
- 伤害入队 + 帧末统一结算，避免同一帧多段伤害触发多次受击/死亡逻辑。
- 死亡时直接设置仲裁标志，阻断输入/上半身/表情/IK/装备，然后强制切入 `PlayerDeathState`。
- 16 槽位环形队列，零 GC。

### 1.3 StaminaArbiter — 体力仲裁

```csharp
public sealed class StaminaArbiter
{
    public void Arbitrate()
    {
        float drainRate = GetStaminaDrainRateForState(_data.CurrentLocomotionState);

        if (drainRate > 0f)
        {
            _data.CurrentStamina -= drainRate * Time.deltaTime;
            if (_data.CurrentStamina <= 0f) _data.IsStaminaDepleted = true;
        }
        else if (drainRate < 0f)
        {
            _data.CurrentStamina += (-drainRate) * Time.deltaTime;
            if (_data.CurrentStamina > _config.Core.MaxStamina * _config.Core.StaminaRecoverThreshold)
                _data.IsStaminaDepleted = false;
        }

        _data.CurrentStamina = Mathf.Clamp(_data.CurrentStamina, 0f, _config.Core.MaxStamina);
    }

    private float GetStaminaDrainRateForState(LocomotionState state)
    {
        return state switch
        {
            LocomotionState.Sprint => _config.Core.StaminaDrainRate,
            LocomotionState.Walk => -_config.Core.StaminaRegenRate * _config.Core.WalkStaminaRegenMult,
            LocomotionState.Jog => -_config.Core.StaminaRegenRate,
            LocomotionState.Idle => -_config.Core.StaminaRegenRate,
            _ => 0f
        };
    }
}
```

**设计亮点**：
- 体力的消耗/恢复与 LocomotionState 绑定，状态机只改变状态，不直接改体力。
- `IsStaminaDepleted` 标志可被拦截器读取，用于阻止冲刺/翻滚。

### 1.4 LODArbiter — 性能降级

```csharp
public class LODArbiter
{
    public void Arbitrate()
    {
        _timeSinceLastArbitration += Time.deltaTime;
        if (_timeSinceLastArbitration < _config.Core.LODCheckInterval) return;
        _timeSinceLastArbitration = 0f;

        float sqrDist = (_player.transform.position - _data.CameraTransform.position).sqrMagnitude;
        // ... 根据距离切换 High / Medium / Low

        _player.Animator.enabled = (lod == CharacterLOD.High);
        _data.Arbitration.BlockIK = isDegraded;
        _data.Arbitration.BlockFacial = isDegraded;
    }
}
```

**设计亮点**：
- 远距离角色禁用 Animator，近距离恢复，省 CPU。
- 通过仲裁标志 `_data.Arbitration.BlockIK / BlockFacial` 让 IK/表情系统自行降级，不侵入各子系统代码。

---

## 2. 装备与物品系统

### 2.1 EquipmentDriver

```csharp
public class EquipmentDriver
{
    public EquippableItemSO CurrentItemData { get; private set; }
    public ItemInstance CurrentItemInstance { get; private set; }
    public IHoldableItem CurrentItemDirector { get; private set; }
    private GameObject _currentWeaponInstance;

    public void EquipItem(ItemInstance itemInstance)
    {
        UnequipCurrentItem();
        CurrentItemInstance = itemInstance;
        CurrentItemData = itemInstance?.GetSODataAs<EquippableItemSO>();
        if (CurrentItemData == null) return;

        if (CurrentItemData.Prefab != null && _player.WeaponContainer != null)
        {
            _currentWeaponInstance = SimpleObjectPoolSystem.Shared != null
                ? SimpleObjectPoolSystem.Shared.Spawn(CurrentItemData.Prefab)
                : Object.Instantiate(CurrentItemData.Prefab, _player.WeaponContainer);

            _currentWeaponInstance.transform.SetParent(_player.WeaponContainer, false);
            _currentWeaponInstance.transform.localScale = Vector3.one;
            _currentWeaponInstance.transform.localPosition = CurrentItemData.HoldPositionOffset;
            _currentWeaponInstance.transform.localRotation = CurrentItemData.HoldRotationOffset;

            CurrentItemDirector = _currentWeaponInstance.GetComponent<IHoldableItem>();
            CurrentItemDirector?.Initialize(CurrentItemInstance);
        }

        _player?.NotifyEquipmentChanged();
    }

    public void UnequipCurrentItem()
    {
        if (_currentWeaponInstance != null)
        {
            if (SimpleObjectPoolSystem.Shared != null)
                SimpleObjectPoolSystem.Shared.Despawn(_currentWeaponInstance);
            else
                Object.Destroy(_currentWeaponInstance);
            _currentWeaponInstance = null;
        }

        ClearAllIKReferencesFromRuntimeData();
        _player.NotifyEquipmentChanged();
        CurrentItemData = null;
        CurrentItemInstance = null;
        CurrentItemDirector = null;
    }
}
```

**设计亮点**：
- 装备切换只由 `PlayerRuntimeData.CurrentItem` 变化驱动。
- 武器模型走对象池，卸载时清理黑板 IK 引用，防止悬空引用。
- 武器逻辑接口 `IHoldableItem` 解耦，AK/炮/剑等各自实现。

### 2.2 PlayerInventoryController

```csharp
public class PlayerInventoryController
{
    public InventorySystem MainInventory { get; private set; }
    public InventorySystem HotbarInventory { get; private set; } // 5 个快捷栏槽位

    public void Update()
    {
        if (_data.Arbitration.BlockInventory) return;

        if (_data.WantsToEquipHotbarIndex != -1)
        {
            TryEquipSlot(_data.WantsToEquipHotbarIndex);
            _data.WantsToEquipHotbarIndex = -1;
        }
    }

    private void TryEquipSlot(int slotIndex)
    {
        if (_currentSlotIndex == slotIndex)
        {
            Unequip();
            ConsumeHotbarKey(slotIndex);
            return;
        }

        var targetInstance = HotbarInventory.GetAt(slotIndex);
        if (targetInstance == null) { Unequip(); ConsumeHotbarKey(slotIndex); return; }

        if (targetInstance.BaseData is EquippableItemSO)
        {
            _player.RuntimeData.CurrentItem = targetInstance;
            ConsumeHotbarKey(slotIndex);
        }
    }
}
```

**设计亮点**：
- 输入只产生「意图切换槽位」`WantsToEquipHotbarIndex`，实际切换由 `InventoryController.Update()` 执行。
- 切换后通过事件 `NotifyEquipmentChanged` 同步 `_currentSlotIndex`。
- 支持再次按同一数字键卸载武器。

---

## 3. 输入系统

### 3.1 抽象输入源

```csharp
public abstract class InputSourceBase : MonoBehaviour, IInputSource
{
    public float InputFlickerBuffer = 0.05f;   // 移动抖动缓冲
    public float ActionBufferTime = 0.2f;      // 动作输入缓冲（跳跃/翻滚等）

    public abstract void FetchRawInput(ref RawInputData rawData);
    public bool IsBlocked => _runtimeData != null && _runtimeData.Arbitration.BlockInput;
}
```

**设计亮点**：
- 输入源可替换：玩家输入、AI 输入、回放输入都实现同一接口。
- `IsBlocked` 由仲裁标志控制，死亡/受击/Override 时自动阻断输入。

### 3.2 InputPipeline — 输入后处理

```csharp
public class InputPipeline
{
    private readonly InputSourceBase _inputSource;
    private InputData _inputData;
    private RawInputData _rawData;
    private Vector2 _bufferedMove;
    private float _lastNonZeroMoveTime;
    private ulong _frameIndex;

    public InputData Current => _inputData;

    public void Update()
    {
        _inputData.lastFrameData = _inputData.currentFrameData;
        _inputSource.FetchRawInput(ref _rawData);
        ProcessRawInput();
        _frameIndex++;
    }
}
```

核心后处理：
1. **移动抖动缓冲**：WASD 短暂归零时不立即清零，避免按键抖动导致动画反复切换 Idle/Move。
2. **动作缓冲**：跳跃/翻滚等输入在 `_actionBufferTime` 内保持有效，类似格斗游戏的输入缓冲。
3. **消费接口**：状态机或拦截器确认消费输入后，调用 `ConsumeJumpPressed()` 等清除缓冲。

### 3.3 意图处理器

`MainProcessorPipeline` 中的意图处理器将 `ProcessedInputData` 翻译成黑板意图：

```csharp
public void UpdateIntentProcessors()
{
    ref readonly ProcessedInputData inputSnapshot = ref _inputPipeline.Current.currentFrameData.Processed;

    _viewRotationProcessor.Update(in inputSnapshot);
    _aimIntentProcessor.Update(in inputSnapshot);
    _locomotionIntentProcessor.Update(in inputSnapshot);
    _jumpOrVaultIntentProcessor.Update(in inputSnapshot);
    _eojIntentProcessor.Update(in inputSnapshot);
    _hotbarIntentProcessor.Update(in inputSnapshot);
    _actionIntentProcessor.Update(in inputSnapshot);
}
```

**点评**：
- `ref readonly` 传递只读引用，避免结构体复制，同时编译器强制禁止修改。
- 每个处理器只干一件事，新增意图类型只需新增处理器，不修改状态机。

---

## 4. 动画外观层

### 4.1 AnimationFacadeBase

```csharp
public abstract class AnimationFacadeBase : MonoBehaviour, IAnimationFacade
{
    public abstract void PlayClip(AnimationClip clip, AnimPlayOptions options);
    public abstract void PlayTransition(object transitionObj, AnimPlayOptions options);
    public abstract void SetMixerParameter(Vector2 parameter, int layerIndex = 0);
    public abstract void SetOnEndCallback(Action onEndAction, int layerIndex = 0);
    public abstract void ClearOnEndCallback(int layerIndex = 0);
    public abstract void SetOverrideOnEndCallback(Action onEndAction);
    public abstract void ClearOverrideOnEndCallback();
    public abstract void SetLayerWeight(int layerIndex, float weight, float fadeDuration = 0f);
    public abstract void SetLayerMask(int layerIndex, AvatarMask mask);
    public abstract void AddCallback(float normalizedTime, Action callback, int layerIndex = 0);
    public abstract float CurrentTime { get; }
    public abstract float CurrentNormalizedTime { get; }
    public abstract void PlayFullBodyAction(AnimationClip clip, float fadeDuration = 0.2f);
    public abstract void StopFullBodyAction();
}
```

### 4.2 AnimancerFacade 实现要点

```csharp
public class AnimancerFacade : AnimationFacadeBase
{
    private AnimancerComponent _animancer;
    private Dictionary<int, Action> _layerOnEndActions = new Dictionary<int, Action>();
    private Stack<CallbackWrapper> _wrapperPool = new Stack<CallbackWrapper>();

    public override void PlayClip(AnimationClip clip, AnimPlayOptions options)
    {
        ClearOnEndCallback(options.Layer);
        var layer = GetLayerOrFallback(options.Layer);
        var state = options.FadeDuration >= 0
            ? layer.Play(clip, options.FadeDuration)
            : layer.Play(clip);
        ApplyOptions(state, options);
        RebindOnEndIfNeeded(options.Layer, state);
    }

    public override void SetLayerWeight(int layerIndex, float weight, float fadeDuration = 0f)
    {
        var layer = GetLayerOrFallback(layerIndex);
        if (fadeDuration > 0f) layer.StartFade(weight, fadeDuration);
        else layer.Weight = weight;
    }

    public override void SetLayerMask(int layerIndex, AvatarMask mask)
    {
        GetLayerOrFallback(layerIndex).Mask = mask;
    }
}
```

**设计亮点**：
- 外观层把 Animancer 细节隐藏，底层可替换为 Animator 或其他动画系统。
- 多层回调字典：每个动画层有独立回调槽，互不串线。
- CallbackWrapper 对象池：避免动画结束回调产生闭包 GC。
- `PlayFullBodyAction` 会临时关闭上半身层权重，确保 Override 动作不被上半身覆盖。

---

## 5. 更多状态示例

### 5.1 PlayerJumpState

```csharp
public class PlayerJumpState : PlayerBaseState
{
    public override void Enter()
    {
        _canCheckLand = false;
        data.SfxQueue.Enqueue(PlayerSfxEvent.Jump);
        data.FacialEventRequest = PlayerFacialEvent.Jump;

        SelectJumpAnimation(); // 根据 Walk/Jog/Sprint/空手/持械选不同动画和力度
        ChooseOptionsAndPlay(_clipData.Clip);

        AnimFacade.SetOnEndCallback(() =>
        {
            AnimFacade.ClearOnEndCallback();
            if (player.CharController.isGrounded)
                player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerLandState>());
            else
                player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerFallState>());
        });

        PerformJumpPhysics(); // data.VerticalVelocity = _jumpForce
        player.InputPipeline.ConsumeJumpPressed(); // 消费输入
    }

    protected override void UpdateStateLogic()
    {
        if (data.WantsDoubleJump && !data.IsGrounded)
        {
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerDoubleJumpState>());
            return;
        }

        if (_canCheckLand && data.VerticalVelocity <= 0 && player.CharController.isGrounded)
        {
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerLandState>());
        }
    }

    public override void PhysicsUpdate()
    {
        player.MotionDriver.UpdateMotion(null, 0f);
    }
}
```

**点评**：
- 状态内触发音效/表情意图，由对应 Controller 消费，不直接播放。
- 动画结束回调中切换状态，保证动作完整。
- 进入时消费输入，避免同帧重复触发。

### 5.2 PlayerFallState

```csharp
public class PlayerFallState : PlayerBaseState
{
    public override void Enter()
    {
        ChooseOptionsAndPlay(config.LocomotionAnims.FallAnim);
    }

    protected override void UpdateStateLogic()
    {
        if (data.IsGrounded)
        {
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerLandState>());
        }
    }

    public override void PhysicsUpdate()
    {
        player.MotionDriver.UpdateMotion();
    }
}
```

**点评**：Fall 状态逻辑极简，只检测接地，因为起跳/二段跳等复杂条件已由拦截器和其他状态处理。

### 5.3 PlayerAimIdleState

```csharp
public class PlayerAimIdleState : PlayerBaseState
{
    public override void Enter()
    {
        var options = AnimPlayOptions.Default;
        options.FadeDuration = 0.4f;
        AnimFacade.PlayTransition(config.LocomotionAnims.IdleAnim, options);
    }

    protected override void UpdateStateLogic()
    {
        if (!data.IsAiming)
        {
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerIdleState>());
            return;
        }

        if (data.WantsDoubleJump) { ... }
        if (data.WantsToJump) { ... }

        if (data.CurrentLocomotionState != LocomotionState.Idle)
        {
            player.StateMachine.ChangeState(player.StateRegistry.GetState<PlayerAimMoveState>());
        }
    }
}
```

**点评**：瞄准状态与普通移动状态分离（AimIdle / AimMove），而不是用一个 `IsAiming` 标志混入 Idle/Move，这样动画和移动参数可以独立配置。

### 5.4 UpperBodyHoldItemState

```csharp
public class UpperBodyHoldItemState : UpperBodyBaseState
{
    private IHoldableItem _currentItem;
    private ItemInstance _cachedInstance;

    public override void Enter()
    {
        player.AnimFacade.SetLayerWeight(1, 1f, 0.25f);
        SyncEquipmentFromBlackboard();
    }

    public override void Exit()
    {
        _currentItem?.OnForceUnequip();
    }

    protected override void UpdateStateLogic()
    {
        if (_cachedInstance != player.RuntimeData.CurrentItem)
        {
            SyncEquipmentFromBlackboard();
            return;
        }

        if (player.RuntimeData.CurrentItem == null)
        {
            player.UpperBodyController.StateMachine.ChangeState(
                player.UpperBodyController.StateRegistry.GetState<UpperBodyEmptyState>()
            );
            return;
        }

        _currentItem?.OnUpdateLogic(); // 武器自己处理开火/瞄准/换弹
    }
}
```

**点评**：
- 上半身状态只关心装备变化，开火/瞄准/换弹等逻辑下放给武器实体 `IHoldableItem`。
- 全身状态机无需知道当前持有何种武器，实现上下半身解耦。

---

## 6. 与 AfterToken 的进一步对比

| 维度 | BBB-Nexus | AfterToken | 可借鉴程度 |
|---|---|---|---|
| 仲裁器 | Action/Health/Stamina/LOD 集中仲裁 | 伤害在 BattleSystem 直接结算 | ⭐⭐⭐⭐ 高 |
| 装备切换 | 黑板 `CurrentItem` + 上半身状态机 + EquipmentDriver | WeaponSystem 直接管理武器槽 | ⭐⭐⭐ 中 |
| 输入缓冲 | 动作输入 0.2s 缓冲，移动抖动缓冲 | 无缓冲，即时响应 | ⭐⭐⭐ 中 |
| 动画外观层 | Facade 解耦 Animancer | 直接调用 Animator | ⭐⭐⭐⭐ 高（概念可迁移） |
| 状态内触发表现 | 写入 SfxQueue / FacialEvent，由 Controller 消费 | 状态内直接播放动画/触发事件 | ⭐⭐⭐⭐ 高 |
| 伤害队列 | HealthArbiter 帧末统一结算 | `IBattleEvent.OnEntityDamaged` 即时触发 | ⭐⭐⭐ 中 |
| LOD 降级 | 距离驱动 Animator/IK/表情开关 | 无 | ⭐⭐ 低（2D 项目需求弱） |

---

## 7. 更新后的迁移建议

基于补充调研，对原方案做以下更新：

### 7.1 优先推荐：仲裁器 + 黑板 + 输入缓冲

这是与 AfterToken 当前架构最契合、改动最小、收益最高的组合：

1. **伤害仲裁**：把 `BattleSystem` 的即时伤害改为入队，帧末统一结算，避免多段伤害同帧触发多次死亡/受击。
2. **死亡仲裁**：统一在仲裁器中设置 `IsDead`、`BlockInput`、`BlockMove`、`BlockFire` 等标志，然后强制切入 `PlayerDeadState`。
3. **玩家黑板**：把 `IsReloading`、`IsAiming`、`IsDodging`、`CurrentWeaponSlot`、`WantsToFire` 等集中到 `PlayerRuntimeData`。
4. **输入缓冲**：给翻滚/换弹/开火增加短暂输入缓冲，提升手感。

### 7.2 次推荐：动画/表现事件化

把状态内的「播放音效、触发特效、切换准星」改为写入事件队列/黑板意图，由对应 Controller 消费：

```csharp
// 不推荐：状态内直接播放
AudioSource.PlayOneShot(reloadSound);

// 推荐：写入意图
PlayerRuntimeData.Instance.SfxQueue.Enqueue(PlayerSfxEvent.Reload);
```

这能让状态机更纯粹，也便于后续做对象池和混音。

### 7.3 不建议迁移

1. **完整替换 GameFramework FSM**：成本过高。
2. **引入 Animancer / CharacterController**：AfterToken 是 2D Rigidbody2D 项目，不适用。
3. **上半身状态机（按骨骼分层）**：2D Sprite 没有骨骼分层，但可以把「武器行为层」抽象出来作为逻辑上的上层状态机。

---

## 8. 结论

BBB-Nexus 真正值得 AfterToken 学习的是其**架构思想**而非具体实现：

- 用**黑板**解耦输入、状态、表现。
- 用**仲裁器**集中处理冲突和全局状态变更。
- 用**拦截器**避免每个状态重复写全局切换条件。
- 用**外观层**隔离底层动画/物理/音频实现。

下一步如果要出代码，建议先做最小原型：

1. 新增 `PlayerRuntimeData` 黑板。
2. 把 `InputSystem` 的输入写入黑板意图。
3. 新增 `PlayerStateInterceptor` 基类 + `DeathInterceptor`。
4. 在 `PlayerBaseState` 的 `OnUpdate` 中统一检查拦截器。
5. 验证「死亡强制打断换弹/移动/闪避」的效果。
