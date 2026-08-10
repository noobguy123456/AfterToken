# 提案：纯 C# 数据导向架构演进路线（ECS 思维，不引入 DOTS）

> 提案状态：待评审（用户考虑中，未启动实施）  
> 提出时间：2026-08-08  
> 提案路径：`docs/Proposal/combat/pure-csharp-data-oriented-roadmap.md`  
> 关联模块：`combat/projectile-system`、`combat/ballistic-system`、`combat/enemy-system`、`player/player-system`  
> 关联文档：`docs/Proposal/combat/bullet-logic-visual-separation.md`

---

## 1. 背景

2026-08-08 对玩家、敌人、子弹三个系统做了一轮 code review，结论如下：

- **规模**：玩家 1 个实体；敌人 10/15/20 个、开局一波无波次；子弹峰值 tracer ≈ 4、飞行弹 1~2 发、池容量 5。
- **健康状况**：三个系统稳态均近零 GC，无性能问题。
- **已落地整改**（本提案前置工作，已完成并编译验证）：
  1. 子弹系统修复 3 个存量 bug：双重命中（Tick SphereCast 与 `OnTriggerEnter` 事件重复结算）、火箭追踪失效（`Transform.GetInstanceID()` 与 `_enemyMap` 键语义不一致，统一为 `EnemyEntity` 组件 InstanceID）、飞行中视觉不跟随（`ProjectileSystem.Tick` 每帧驱动 `UpdateVisual`）。
  2. 玩家状态收敛：IsDead / IsDodging / MoveInput / AimInput / IsAiming 五份状态统一以 `PlayerStateContext` 黑板为唯一 owner，`PlayerEntity` / `WeaponSystem` 内重复字段改为转发属性；HP/体力 owner 保持 `PlayerSystem`，弹药 owner 保持 `WeaponInstance`，均确认无第二副本。

### 为什么不直接用 Unity DOTS（ECS + Burst + Job）

1. **数量级不支撑**：ECS 性能收益从数百~数千实体开始，当前规模下固定成本纯亏。
2. **与 HybridCLR 根本冲突**：DOTS 性能来自 Burst 在构建期将代码离线编译为原生机器码；HybridCLR 热更代码以 IL 形式运行，无法经过 Burst。上 DOTS = 战斗系统退出热更层；保热更 = ECS 跑在解释器里反而更慢。两条路物理上只能选一条。
3. **深耦合改造成本高**：现有代码是"表现即逻辑"的 Mono 写法（Rigidbody/OverlapSphere/Animator/TEngine FSM 与数据混在一起），改 DOTS 等于全重写。

**但 ECS 的架构思维（实体=ID、组件=纯数据、System 批处理、SoA 数据布局）可以用纯 C# 在热更层落地**，与 Burst/Job 无关。本提案评估这条路线。

---

## 2. 目标

在**不引入 DOTS/Burst、不影响热更**的前提下，按 ECS 思维逐步演进战斗代码框架：

- 数据与表现分离（GameObject 退化为纯视图）；
- 逻辑集中到 System 批量处理（消灭每实体各自 Update）；
- 密集批量数据用 SoA 数组，稀疏状态交换用受约束的黑板。

---

## 3. 核心思想对齐（评审前置阅读）

### 3.1 ECS 四条规矩 + 一个思维方式

| 规矩 | 含义 | 项目现状对照 |
|---|---|---|
| Entity 只是 ID | 实体无字段无行为，只是数据表中的索引 | `EnemyEntity` 是数据+行为+表现混合体（反例） |
| Component 只是数据 | struct，不允许有方法 | `ProjectileData` 已符合（纯 POCO + MemoryPool） |
| System 是全部逻辑 | 每帧遍历一批数据统一处理 | `ProjectileSystem.Tick` 已符合；`EnemyEntity` 各自 Update 是反例 |
| 组合优于继承 | 新类型 = 组件排列组合 | 当前为挂组件式 Mono 组合，部分符合 |

思维方式：**面向数据设计**——先想"每帧哪些数据变成哪些数据"，再想"谁来处理"。

### 3.2 三条实现原理（玩家状态收敛中已验证）

1. **数据只能有一个家，引用可以有无数个**——每份状态指定唯一 owner，其他位置只持有访问路径（转发属性），不持有副本。
2. **收敛不等于集中**——单一数据源是"按字段"说的，每份状态归属最懂它的系统；全堆进一个类会变成上帝类。
3. **用访问器隔离变化**——数据搬家时保持属性签名不变，只改属性体，调用方无感。

### 3.3 黑板模式（Blackboard Pattern）

`PlayerStateContext` 即黑板模式：多个互相无引用的系统通过共享工作内存交换信息（WeaponSystem 写 IsAiming，AimAssistSystem / CameraSystem3D 读）。

**必须受约束使用**：黑板只是聚合点，每个字段仍有唯一写方（owner），其他系统只读。无约束的黑板会退化为全局变量垃圾桶（隐式耦合、写入冲突、时序依赖），正是数据碎片化的另一种形态。

### 3.4 黑板与 SoA 的分工

| | 受约束的黑板 | SoA / 数据层 |
|---|---|---|
| 数据特征 | 稀疏、低频、状态类（瞄准中？死亡？警报？） | 密集、每帧、批量（20 个敌人的位置、所有子弹坐标） |
| 解决的问题 | 系统间解耦 | 遍历效率 + 逻辑集中 |

### 3.5 已知代价：调试工具从"免费"变"自建"

数据搬入纯 C# struct 数组后不走 Unity 序列化体系，Inspector 点 GameObject 将看不到运行期字段。需要自建调试视图：System 每帧用 Gizmos 绘制关键数据、Editor 窗口/调试面板列表显示数组内容。代价是前期写工具，收益是定制视图（如一张表格扫完所有敌人状态）比逐个点 Inspector 更好用。

---

## 4. 待评审的演进项（积压 backlog）

按性价比排序，**均未启动，逐项独立评审**。

### 4.1 子弹系统 SoA 化样板（建议最先做）

- 现状：`ProjectileData` 已是纯 POCO，`ProjectileSystem.Tick` 已是集中批处理，视觉已由 Tick 驱动——ECS 形态完成约 70%。
- 内容：`List<ProjectileData>`（引用类型、指针跳转）改为结构体数组或并行数组（位置/速度/状态分列）；与 `docs/Proposal/combat/bullet-logic-visual-separation.md` 的 List 化 + SpherecastCommand 批量 Job 方向合并。
- 定位：**作为"ECS 思维"的完整样板**，验证可行性与调试工作流后，后续系统照抄。
- 规模参考：弹幕 200~1000 发时才存在性能刚需，当前动机是架构统一而非性能。

### 4.2 敌人系统数据层抽取（子弹样板验证后再动）

- 现状：每个 `EnemyEntity` 各自跑 Update/LateUpdate，各算距离、各写 velocity；自研 A* 已做降频 + 量化缓存，稳态零 GC。
- 内容：抽 `EnemyTickSystem`，每帧遍历敌人数据数组，统一做感知 → 寻路节流 → 移动写入；`EnemyEntity` 只剩 Animator/SpriteRenderer/血条同步（保留 Mono 表现层，"穷人版 ECS"）。
- 收益：逻辑入口从 N 个变 1 个；敌人扩容到 100 个只是数组变长。
- 明确不做：**不改 TEngine FSM**（状态机保持现状，属地震式改动，可永远不做）。

### 4.3 敌人感知小黑板（候选，优先级最低）

- 现状：Chase 状态每帧自算距离、自判追击，感知结果不共享。
- 内容：抽 per-enemy 小黑板，感知结果（距离、是否丢失目标、警报等级）写一次，FSM 各状态、NavigationSystem、血条 UI 只读。
- 约束：遵循"受约束的黑板"——每个字段唯一写方。

### 4.4 玩家系统：不做

n=1 实体，任何批量化都是负收益。状态收敛已完成，即为终态。

---

## 5. 风险与决策

| 风险 | 说明 | 缓解 |
|---|---|---|
| 调试成本上升 | Inspector 对纯 C# 数据失效 | 每个 SoA 化系统同步交付 Gizmos/调试面板 |
| 代码量上涨 | 池化、索引管理需手写 | 以子弹样板验证工作量后再推广 |
| 心智转换 | 从"挂组件"变"设计数据布局"，前期慢 | 样板先行，文档沉淀模式 |
| 过度工程 | 当前规模无性能刚需 | 逐项评审，4.2/4.3 可被无限期推迟 |

**待决策点**：是否启动 4.1（子弹 SoA 化样板）。

---

## 6. 实施步骤（若 4.1 通过评审）

1. 合并本提案与 `bullet-logic-visual-separation.md` 的设计，确定 SoA 数据布局与视觉分层方案。
2. 改造 `ProjectileSystem` 数据结构（保持对外接口不变，参考原理三）。
3. 同步交付调试工具（Gizmos 绘制弹道/命中范围）。
4. 编译验证 + Play 模式回归（含狙击枪、火箭追踪、双命中场景回归）。
5. 样板复盘，决定是否启动 4.2。

---

## 7. 结论

ECS 的架构思维与 DOTS 实现可分离：架构收益（数据/表现分离、逻辑集中、可扩容）全拿，硬件压榨（Burst SIMD、Job 多线程）放弃——当前规模本就用不上。路线本身成立，但是否实施、何时实施，由评审决定。**在通过评审前不改任何代码。**
