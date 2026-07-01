# BBB-Nexus 状态机切换学习方案

> 目标仓库：https://github.com/bunkerboy258/BBB-Nexus.git  
> 本方案用于系统学习该项目的角色状态机切换机制，并评估可迁移到 AfterToken 项目的设计点。

---

## 1. 学习目标

1. 理解 BBB-Nexus 中状态机的整体架构（分层 FSM、意图驱动、拦截器机制）。
2. 掌握其核心状态切换流程：`Enter / LogicUpdate / PhysicsUpdate / Exit`。
3. 学习其全局打断、Override 强制覆盖层、上半身独立状态机等高级机制。
4. 评估哪些设计可直接/间接迁移到 AfterToken 的玩家 FSM、武器系统、敌人 AI。
5. 形成一份可落地的迁移建议文档（或原型代码）。

---

## 2. BBB-Nexus 状态机核心特点

| 特性 | 说明 |
|---|---|
| 极简 FSM 核心 | `StateMachine` + `BaseState` 只有基础四个生命周期，代码量极小。 |
| 分层状态机 | 全身状态机（FullBody）+ 上半身状态机（UpperBody）独立运行，通过 Avatar Mask 混合。 |
| 意图驱动 | 状态不直接读输入，而是读取 `PlayerRuntimeData` 黑板上的意图标志（如 `WantsToJump`）。 |
| 拦截器机制 | 全局切换条件（跳跃、翻滚、下落等）以 `StateInterceptorSO` 配置，避免在每个状态里重复写 `if`。 |
| Override 覆盖层 | 高优先级动作（受击、处决、表情）可强制切入 `OverrideState`，阻断普通打断。 |
| 单入口 Tick | `BBBCharacterController` 统一驱动所有子系统更新，时序清晰。 |
| 数据驱动配置 | `PlayerBrainSO` 作为状态机装配表 + 拦截器列表，可视化配置。 |
| 低 GC 设计 | 状态实例预分配、栈上输入快照、固定槽位音效队列。 |

---

## 3. 学习范围

### 3.1 必读代码

| 路径 | 学习重点 |
|---|---|
| `Core/StateMachine/StateMachine.cs` | 状态机核心 API：Initialize / ChangeState / CurrentState。 |
| `Core/StateMachine/BaseState.cs` | 生命周期定义。 |
| `Character/BBBCharacterController.cs` | 单入口 Tick、子系统初始化、整体架构。 |
| `Character/States/FullBody/PlayerBaseState.cs` | 封印 `LogicUpdate` + `CheckInterrupts` 机制。 |
| `Character/States/Core/PlayerBrainSO.cs` | Brain 配置资产、拦截器列表、初始状态。 |
| `Character/States/Core/GlobalInterruptProcessor.cs` | 全局打断处理。 |
| `Character/States/Core/Base/StateInterceptorSO.cs` | 拦截器基类与接口。 |
| `Character/Core/States/PlayerStateRegistry.cs` | 状态注册表初始化。 |
| `Character/expression/UpperBodyController.cs` | 上半身独立状态机。 |
| `Character/ProcessingPipelines/MainProcessorPipeline.cs` | 意图处理器与参数处理器管线。 |
| `Character/Core/Driver/MotionDriver.cs` | 动画与物理位移时序对齐。 |
| `Character/Core/Animation/AnimationFacadeBase.cs` | 动画外观层。 |
| `Character/States/FullBody/PlayerIdleState.cs` | 状态切换示例。 |
| `Character/States/FullBody/PlayerRollState.cs` | Warp 运动示例。 |
| `Character/States/Override/OverrideState.cs` | 强制覆盖层示例。 |

### 3.2 选读代码

- `Character/States/Core/Enums/PlayerStateType.cs` / `UpperBodyStateType.cs`
- `Character/States/Core/UpperBodyInterruptProcessor.cs`
- `Character/States/UpperBody/UpperBodyBaseState.cs`
- `Character/ArbiterPipeline/` 下的仲裁器
- `README.md` 中的架构说明

---

## 4. 学习阶段

### 阶段 1：整体架构速览（预计 0.5 天）

- [ ] 阅读仓库 `README.md`，了解项目定位。
- [ ] 浏览目录结构，定位状态机相关文件。
- [ ] 画出 BBB-Nexus 角色架构草图：Controller → StateMachine → Registry → BrainSO → Interceptors。
- [ ] 记录与 AfterToken 当前 FSM（GameFramework FSM + PlayerReloadState 等）的差异。

### 阶段 2：核心状态机机制精读（预计 1 天）

- [ ] 精读 `StateMachine.cs` + `BaseState.cs`，理解状态切换最小闭环。
- [ ] 精读 `PlayerBaseState.cs`，理解 `CheckInterrupts` 如何统一处理全局打断。
- [ ] 精读 `StateInterceptorSO.cs` 及 2-3 个具体拦截器（如跳跃、翻滚、下落）。
- [ ] 跟踪一次完整切换：Idle → MoveStart → MoveLoop → Jump → Fall → Land → Idle。
- [ ] 记录：
  - 状态切换条件由谁决定？
  - 动画由谁触发？
  - 物理位移由谁更新？
  - 退出时如何清理？

### 阶段 3：高级机制学习（预计 1 天）

- [ ] 上半身独立状态机：为什么需要？如何与全身状态机并行？
- [ ] Override 覆盖层：ActionRequest / Arbiter / 优先级 / 同优先级刷新。
- [ ] 意图与参数管线：输入如何变成意图？意图如何变成动画参数？
- [ ] MotionDriver：LateUpdate 读取动画 NormalizedTime 计算位移，避免滑步。
- [ ] 对象池友好：IPoolable、OnSpawned / OnDespawned 清理。

### 阶段 4：可迁移性评估（预计 0.5 天）

- [ ] 对比 AfterToken 当前 `PlayerIdleState` / `PlayerReloadState` 等实现。
- [ ] 列出可借鉴点：
  - 拦截器机制是否适合处理闪避/换弹/死亡打断？
  - 黑板模式是否适合统一玩家输入意图？
  - 上半身状态机是否适合持枪/瞄准/投掷？
  - Override 是否适合受击/处决/复活？
- [ ] 列出不适用点：
  - Animancer 依赖（AfterToken 使用 Animator）
  - 复杂度与项目阶段是否匹配
  - 热更域兼容性（AfterToken 热更域为 GameLogic / GameProto）

### 阶段 5：输出落地建议（预计 0.5 天）

- [ ] 撰写《BBB-Nexus FSM 迁移评估报告》。
- [ ] 给出可选方案：
  - 方案 A：完整迁移 BBB-Nexus 架构。
  - 方案 B：只引入拦截器 + 黑板模式，保留现有 GameFramework FSM。
  - 方案 C：只借鉴 Override 覆盖层概念。
  - 方案 D：暂不迁移，保持现状。
- [ ] 如选方案 B/C，给出最小原型代码与接入步骤。

---

## 5. 预期输出

1. **学习笔记**：每个阶段的关键代码摘录 + 个人理解。
2. **架构对比图**：BBB-Nexus FSM vs AfterToken 当前 FSM。
3. **迁移评估报告**：可借鉴点、风险、推荐方案。
4. **可选原型代码**：若用户决定迁移，提供最小可运行示例。

---

## 6. 与 AfterToken 当前架构的结合点

| AfterToken 模块 | 可借鉴的 BBB-Nexus 设计 |
|---|---|
| `PlayerIdleState` / `PlayerMoveState` / `PlayerDodgeState` / `PlayerReloadState` | 拦截器机制统一处理「闪避打断移动/换弹」、「死亡打断一切」等。 |
| `InputSystem` → `IBattleInputEvent` | 黑板模式：把输入意图写入 `PlayerRuntimeData`，状态机只读意图。 |
| `WeaponSystem` 的持枪/瞄准/开火 | 上半身状态机：持枪、瞄准、换弹可做成上半身 Layer，不影响下半身移动。 |
| 受击/死亡/处决 | Override 覆盖层：高优先级动作强制切入并阻断普通状态。 |
| 动画与移动同步 | `MotionDriver` 思想：在 LateUpdate 根据动画时间更新位置，减少滑步。 |

---

## 7. 风险提示

1. **依赖 Animancer**：BBB-Nexus 动画层基于 Animancer 插件，AfterToken 当前使用 Animator，迁移需重新实现动画外观层。
2. **复杂度提升**：拦截器 + 黑板 + 分层状态机对小型项目可能过度设计。
3. **热更域限制**：AfterToken 热更域为 `GameLogic` / `GameProto`，需确认 BBB-Nexus 的 `ScriptableObject` 配置资产是否适合放在热更域。
4. **学习成本**：至少需要 3 天才能完整理解并评估。

---

## 8. 建议的下一步

如果认可本方案，建议按以下顺序推进：

1. 先完成阶段 1-2，确认 BBB-Nexus 的核心切换机制是否值得深入。
2. 完成阶段 4 的可迁移性评估，决定采用哪个方案。
3. 若采用方案 B/C，先在 AfterToken 中做一个最小原型（例如只给玩家移动状态加拦截器）。
4. 原型验证通过后再逐步替换现有 FSM。
