# 提案：逻辑子弹与视觉表现分离方案

> 提案状态：待评审  
> 提出时间：2026-06-30  
> 提案路径：`docs/Proposal/combat/bullet-logic-visual-separation.md`  
> 关联模块：`combat/projectile-system`、`combat/ballistic-system`、`infra/effect-system`  
> 关联文档：`docs/射击模块实现文档.md`、`docs/modules/combat/projectile-system/README.md`、`docs/modules/combat/projectile-system/progress.md`

---

## 1. 背景

当前 `ProjectileSystem` 采用 **逻辑数据 + 实体 GameObject 一一对应** 的实现：

```csharp
private readonly Dictionary<int, ProjectileData> _activeProjectiles = new Dictionary<int, ProjectileData>();
private readonly Dictionary<int, ProjectileEntity> _entityMap = new Dictionary<int, ProjectileEntity>();
```

每发子弹都会实例化一个 `GameObject`，挂载 `SpriteRenderer` 与 `ProjectileEntity`，由 `ProjectileSystem.Update()` 同时驱动逻辑更新与视觉更新。

该方案在 M1「战斗闭环」阶段足够直观、调试方便，但在后续引入**弹幕玩法**时会遇到明显瓶颈：

- 每个子弹一个 `GameObject` + `Transform` + `SpriteRenderer`，Draw Call 与 Transform 层级更新开销随数量线性增长。
- 当前 `ProjectileSystem.Update()` 每帧分配 `new List<int>(_activeProjectiles.Keys)`，密集弹幕下 GC 压力大。
- `CircleCast` 与 `OverlapCircleAll` 在大量子弹与敌人场景下，物理查询开销显著。

---

## 2. 目标

将子弹系统拆分为**逻辑层**与**视觉层**：

- **逻辑层**（`ProjectileSystem`）：只维护子弹运行时数据，负责飞行、碰撞、命中判定、生命周期管理。
- **视觉层**（新增 `ProjectileVisualSystem`）：根据逻辑数据批量渲染子弹视觉，普通弹使用 Particle System / Instanced Sprite，关键弹保留实体 GameObject。

这样可以在不牺牲逻辑精确性的前提下，支撑更高密度的弹幕表现。

---

## 3. 详细设计

### 3.1 职责划分

| 层级 | 类 | 职责 |
|---|---|---|
| 逻辑层 | `ProjectileSystem` | 维护 `ProjectileData` 列表；Tick 飞行、碰撞、命中、回收；发布 `IProjectileEvent` |
| 视觉层 | `ProjectileVisualSystem` | 从 `ProjectileSystem` 读取数据；普通弹批量渲染；关键弹创建/回收 `ProjectileEntity` |
| 表现实体 | `ProjectileEntity` | 仅用于火箭、榴弹等关键子弹；负责复杂动画、特效、多帧状态 |

### 3.2 子弹分类

在 `WeaponConfig` / `ProjectileData` 中新增视觉类型字段：

```csharp
public enum ProjectileVisualType
{
    Particle,   // 普通小弹丸，使用 Particle System 批量渲染
    Instanced,  // 大量同材质弹丸，使用 GPU Instancing 批量渲染
    Entity,     // 关键子弹（火箭、榴弹、追踪弹），使用实体 GameObject
}
```

分类建议：

| 类型 | 示例 | 视觉方案 | 原因 |
|---|---|---|---|
| `Entity` | 火箭、榴弹、追踪弹、分裂弹 | GameObject + `ProjectileEntity` | 数量少、行为复杂、需要精确碰撞反馈 |
| `Particle` |  enemy 散射弹幕、玩家霰弹枪弹丸 | Particle System | 数量中等、外观统一、运动简单 |
| `Instanced` |  超密集固定轨迹弹幕（数千发） | GPU Instancing / Compute Shader | 数量极大、同材质、轨迹规则 |

### 3.3 逻辑层改造

`ProjectileSystem` 移除对 `GameObject` / `ProjectileEntity` 的直接管理：

```csharp
public class ProjectileSystem : MonoBehaviour
{
    private readonly List<ProjectileData> _activeProjectiles = new List<ProjectileData>();
    private readonly Queue<ProjectileData> _projectileDataPool = new Queue<ProjectileData>();
    private int _nextProjectileId = 1;

    public IReadOnlyList<ProjectileData> ActiveProjectiles => _activeProjectiles;

    public void CreateProjectile(...)
    {
        var data = AcquireData();
        // 填充数据...
        _activeProjectiles.Add(data);
        GameEvent.Get<IProjectileEvent>().OnProjectileCreated(data.Id, data.Position, data.VisualType);
    }

    private void Update()
    {
        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            var data = _activeProjectiles[i];
            if (!Tick(data))
            {
                DestroyProjectileAt(i);
            }
        }
    }
}
```

关键改动：

- 用 `List<ProjectileData>` 替代 `Dictionary<int, ProjectileData>`，避免每帧 `Keys` 分配。
- 移除 `_entityMap` 与内部 `GameObject` 池。
- 命中、销毁事件新增 `VisualType` 参数，供视觉层决策。

### 3.4 视觉层设计

#### 3.4.1 普通弹：Particle System 方案

使用**一个全局 Particle System** 管理所有普通子弹：

```csharp
public class ProjectileVisualSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem _bulletParticles;
    private ParticleSystem.Particle[] _particleArray;
    private readonly Dictionary<int, int> _projectileToParticleIndex = new Dictionary<int, int>();
}
```

工作方式：

1. 子弹创建时，`Emit(1)` 在枪口位置发射一个粒子。
2. 每帧 `GetParticles()` 读取当前粒子数组。
3. 根据 `ProjectileSystem.ActiveProjectiles` 中对应子弹的逻辑位置，覆盖粒子位置与旋转。
4. 子弹销毁时，将该粒子 `remainingLifetime = 0` 或隐藏。
5. 调用 `SetParticles()` 写回。

> 注意：Particle System 只负责"看起来像子弹飞过去"，真正的命中判定完全由逻辑层 `CircleCast` 完成。

#### 3.4.2 关键弹：保留实体 GameObject

```csharp
private readonly Dictionary<int, ProjectileEntity> _criticalEntities = new Dictionary<int, ProjectileEntity>();
private readonly Queue<ProjectileEntity> _criticalEntityPool = new Queue<ProjectileEntity>();
```

- 火箭、榴弹等数量少但行为复杂的子弹继续走对象池 + GameObject。
- `ProjectileVisualSystem` 负责创建、回收、位置同步。

### 3.5 事件接口扩展

`IProjectileEvent` 新增视觉类型参数：

```csharp
void OnProjectileCreated(int projectileId, Vector3 position, ProjectileVisualType visualType);
void OnProjectileDestroyed(int projectileId, ProjectileVisualType visualType);
```

视觉层订阅这些事件，决定是否生成 Particle 或 Entity。

### 3.6 与当前系统的兼容

- `BallisticSystem` 无需大改，仍负责 Raycast / Projectile 分发。
- `BattleSystem` 仍通过 `IBattleEvent.OnEntityDamaged` 接收伤害，不受视觉层影响。
- `ProjectileEntity` 保留，但仅作为关键弹的视觉/碰撞回调组件。

---

## 4. 渲染压力分析

### 4.1 当前方案的渲染压力

| 压力来源 | 当前方案表现 | 瓶颈阈值 |
|---|---|---|
| Draw Call | 每个子弹一个 `SpriteRenderer`，除非启用了 Dynamic Batching 或 GPU Instancing，否则 Draw Call ≈ 子弹数 | 50~100 发开始明显 |
| Transform 更新 | 每个子弹每帧更新 `Transform.position` 与 `Transform.rotation` | 100 发以上 CPU 占用上升 |
| GameObject 生命周期 | 创建/销毁/激活/失活开销大 | 高频射击时 GC 与 CPU 双高 |
| 物理查询 | 每发子弹 `CircleCast` | 与敌人数量成正比 |

### 4.2 分离方案后的渲染压力

| 渲染方案 | 优势 | 新的压力点 |
|---|---|---|
| **Particle System** | 大量粒子一次 Draw Call；CPU 只更新少量粒子属性 | Particle System 本身有 CPU 开销；`GetParticles/SetParticles` 每帧拷贝数组 |
| **GPU Instancing** | 同材质大量对象一次 Draw Call；CPU 几乎不更新 Transform | 需要统一材质和 Mesh；动态数据需通过 MaterialPropertyBlock 或 ComputeBuffer 写入 |
| **保留 Entity** | 复杂子弹行为直观、调试方便 | 数量必须严格控制 |

总体判断：

- 普通弹走 Particle System 后，**Draw Call 从 N 降到 1~2**，可以支撑 **数百发同屏**。
- 若弹幕规模达到 **数千发**，Particle System 的 CPU 更新也会成为瓶颈，此时需要 GPU Instancing 或 Compute Shader 方案。
- 逻辑层的 `CircleCast` 开销不会因为视觉层改变而减少，需要单独优化（见第 5 节）。

---

## 5. 优化策略

### 5.1 视觉层优化

#### A. Particle System 优化

1. **限制最大粒子数**
   ```csharp
   _bulletParticles.main.maxParticles = 500;
   ```
   超过上限时，按规则回收最旧的子弹（或限制新子弹生成）。

2. **减少粒子属性更新**
   - 只更新 `position` 和 `rotation`，不更新 `size`、`color` 等可变属性。
   - 关闭不需要的模块：Color over Lifetime、Size over Lifetime、Collision 等。

3. **使用 GPU Sprite 模式**
   - 在 Particle System 的 `Renderer` 中设置 `Render Mode = Mesh` 或 `Billboard`。
   - 启用 `GPU Instancing`（如果材质支持）。

4. **分帧 Emit**
   - 单帧创建大量子弹时，分多帧 `Emit`，避免瞬时 CPU 峰值。

#### B. GPU Instancing 优化（超大规模）

1. **统一材质与 Mesh**
   - 所有同类型子弹使用同一个 Material 和 Sprite/Mesh。
   - 材质启用 `Enable GPU Instancing`。

2. **使用 MaterialPropertyBlock**
   ```csharp
   private MaterialPropertyBlock _mpb;
   private Matrix4x4[] _matrices;
   private Vector4[] _colors;
   ```
   每帧批量设置变换矩阵和颜色，调用 `Graphics.DrawMeshInstanced` 或 `Graphics.DrawMeshInstancedIndirect`。

3. **Compute Shader 驱动（终极方案）**
   - 子弹逻辑也部分移到 GPU（位置、旋转）。
   - CPU 只负责初始发射参数和命中结果回读。
   - 适合固定轨迹、不依赖复杂物理的弹幕（如东方式弹幕）。
   - 实现复杂，建议作为 M3+ 的专项优化。

### 5.2 逻辑层优化

#### A. 移除 Dictionary 遍历分配

当前代码：
```csharp
var ids = new List<int>(_activeProjectiles.Keys);  // 每帧分配
```

改为直接遍历 `List<ProjectileData>`：
```csharp
for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
{
    var data = _activeProjectiles[i];
    // ...
}
```

#### B. 数量上限与分帧

```csharp
public const int MAX_PROJECTILES = 500;
```

- 超过上限时拒绝创建或回收最远/最旧的子弹。
- 弹幕 BOSS 的技能可以分多帧发射，避免单帧峰值。

#### C. 空间划分减少碰撞查询

大量子弹 vs 大量敌人时，每发子弹都做 `CircleCast` 会很重。建议：

1. **Spatial Hash / Grid**
   - 将战场划分为固定大小的格子。
   - 每帧更新敌人所在的格子。
   - 子弹只查询自己所在格子及相邻格子的敌人。

2. **敌人被动检测**
   - 子弹只做简单移动，不主动检测敌人。
   - 敌人或敌人的受击盒每帧查询进入范围的子弹。
   - 适合 "子弹多、敌人少" 的场景。

3. **简化碰撞形状**
   - 敌人使用统一的圆形受击盒，减少复杂 Collider 的物理查询开销。

### 5.3 内存优化

1. **ProjectileData 对象池**
   - 当前已使用 `MemoryPool<ProjectileData>`，继续保持。
   - 视觉层 Particle 不需要对象池，由 Particle System 内部管理。

2. **避免字符串拼接与装箱**
   - 日志使用结构化参数，避免 `string.Format`。
   - 事件参数尽量使用值类型。

### 5.4 调试与 Profiling

1. **运行时统计面板**
   - 在 TEngine `DebuggerModule` 中增加：
     - 当前活跃子弹数
     - 当前活跃粒子数
     - 本月 CircleCast 次数
     - Draw Call 估算

2. **性能测试基准**
   - 目标：同屏 200 发普通弹 + 10 发关键弹，稳定 60 FPS（PC 平台）。
   - 目标：同屏 1000 发普通弹，稳定 30 FPS（作为粒子方案上限）。

---

## 6. 实施步骤

建议分阶段实施，避免一次性改动过大：

### 阶段 1：逻辑层解耦（1~2 天）

1. 将 `ProjectileSystem` 中的 `_activeProjectiles` 从 `Dictionary` 改为 `List`。
2. 移除 `ProjectileSystem` 对 `_entityMap` 和内部 GameObject 池的依赖。
3. `ProjectileSystem` 只发布事件，不再直接创建/回收 `ProjectileEntity`。
4. 新增 `IProjectileEvent.OnProjectileCreated/Destroyed` 的视觉类型参数。

### 阶段 2：视觉层实现（2~3 天）

1. 新增 `ProjectileVisualSystem`。
2. 实现关键弹的 `ProjectileEntity` 对象池。
3. 实现普通弹的 Particle System 视觉：
   - 创建 `BulletParticles` Prefab。
   - 实现 `Emit → GetParticles → 覆盖位置 → SetParticles` 流程。
4. 在 `ProcedureBattle.InitializeBattleSystems` 中注册 `ProjectileVisualSystem`。

### 阶段 3：配置扩展（1 天）

1. 在 `WeaponConfig` 中新增 `projectileVisualType` 字段。
2. 在 Luban `TbWeapon` 表中增加对应列。
3. 更新 `BattleSceneSetup` 创建 `BulletParticles` Prefab。

### 阶段 4：优化迭代（持续）

1. 加入数量上限、分帧发射。
2. 接入 Spatial Hash 优化碰撞。
3. 根据 Profiling 结果决定是否迁移到 GPU Instancing。

---

## 7. 风险与决策

| 风险 | 影响 | 缓解措施 |
|---|---|---|
| Particle System 与逻辑位置不同步 | 玩家感觉"子弹打中了但粒子没碰到"或反之 | 明确告知玩家命中判定以逻辑层为准；视觉层只做近似 |
| 关键弹与普通弹边界不清 | 配置混乱 | 制定明确分类规则，优先把火箭/榴弹/追踪弹归为 Entity |
| GPU Instancing 实现复杂 | 开发周期延长 | 作为可选优化，先落地 Particle 方案 |
| 空间划分增加代码复杂度 | 维护成本上升 | 先验证 CircleCast 在真实弹幕场景下是否真成为瓶颈 |

---

## 8. 结论

推荐采用 **"逻辑子弹 + 视觉 Particle System 分离"** 方案，但**不必一步到位到 GPU Instancing**：

1. 先实现逻辑层解耦 + 关键弹保留 Entity + 普通弹走 Particle System。
2. 这套方案可以支撑 **数百发同屏弹幕**，足以覆盖 M2/M3 阶段的玩法需求。
3. 若后续出现 **数千发同屏** 的极端弹幕，再评估迁移到 GPU Instancing 或 Compute Shader。
4. 同时必须优化逻辑层碰撞查询（数量上限 + 空间划分），否则即使视觉层再快，CPU 物理查询也会成为瓶颈。
