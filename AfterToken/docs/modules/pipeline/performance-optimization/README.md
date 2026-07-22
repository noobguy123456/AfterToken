# 性能优化

> 本模块记录 AfterToken 项目的运行时性能治理方案与关键优化点。
> 当前聚焦：M1 战斗闭环阶段的 CPU / GC / 渲染 / 编译时长问题。

---

## 职责

- 收集并分析运行时性能瓶颈（CPU、内存、GC、Draw Call、物理等）。
- 制定并落地优化方案，避免在主循环中产生高频分配或重计算。
- 为低端机型提供可落地的降配与构建加速建议。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `AStarNavigationSystem` | `Assets/GameScripts/HotFix/GameLogic/Navigation/AStarNavigationSystem.cs` | 自研 2D 网格 A*，路径结果池化、原地平滑 |
| `NavigationSystem` | `Assets/GameScripts/HotFix/GameLogic/System/NavigationSystem.cs` | 路径缓存与缓存失效，失效时释放 `PathResult` |
| `EnemyChaseState` | `Assets/GameScripts/HotFix/GameLogic/FSM/Enemy/EnemyChaseState.cs` | 路径刷新间隔可配置 + 动态缩放 |
| `EnemySpawnSystem` | `Assets/GameScripts/HotFix/GameLogic/System/EnemySpawnSystem.cs` | 敌人使用 `PoolSystem` 预加载/回池 |
| `ProjectileSystem` | `Assets/GameScripts/HotFix/GameLogic/System/ProjectileSystem.cs` | 飞行物对象池，爆炸查询非分配化 |
| `DamageNumberUI` | `Assets/GameScripts/HotFix/GameLogic/UI/DamageNumberUI/DamageNumberUI.cs` | 飘字对象池 + 字符串缓存 |

## 已落地优化

### 1. A* 寻路分配治理

- `PathResult` 接入对象池（`Acquire / Release`）。
- `AStarNavigationSystem.ReconstructPath` 复用池化 `List<Vector2>`，不再每 path 新建 List。
- `SmoothPath` 改为 `SmoothPathInPlace`，原地优化路径点，避免额外 List 分配。
- `LayerMask.GetMask("Obstacle")` 缓存为静态字段。

### 2. 敌人系统优化

- **对象池**：`EnemySpawnSystem` 加载一次 Prefab 后使用 `PoolSystem` 预加载并复用；`EnemyDeadState` 死亡后回池。
- **复用状态**：`EnemyEntity` 初始化时自动恢复刚体/碰撞体，避免重新创建组件。
- **路径刷新频率**：`TbEnemy.pathRefreshInterval` 配置基础间隔，`EnemyChaseState` 按玩家距离动态缩放（近快远慢），降低成群敌人时的 A* 调用次数。

### 3. 物理查询非分配化

- `EnemyChaseState`：分离检测使用 `OverlapCircle` + `ContactFilter2D` + 静态 `Collider2D[]`。
- `ProjectileSystem`：爆炸范围伤害从 `OverlapCircleAll` 改为 `OverlapCircle` + `ContactFilter2D` + 静态 `Collider2D[]`。
- `AStarNavigationSystem`：视线检测使用缓存 `LayerMask`。

### 4. 对象池扩展

- `PoolSystem` 新增 `Preload` 接口，支持批量预创建对象。
- `ProjectileSystem` 与 `EnemySpawnSystem` 已接入预加载。

## 待办优化

- [ ] 画质等级：默认 `Standalone` 质量从 `Ultra` 降至 `Medium/Low`，关闭对 2D 无意义的阴影/软粒子/MSAA。
- [ ] 血条 Draw Call：当前每个敌人仍带 2 个 SpriteRenderer 血条；后续可升级为统一 World Space UI 或动态 Mesh 血条。
- [ ] 敌人数量大时：A* 分帧调度、路径共享/FlowField、导航网格动态更新。
- [ ] 构建加速：开发构建可切回 Mono 后端；开启 `Enter Play Mode Options` 关闭 Domain/Scene Reload。
- [ ] 包体清理：移除未使用的重型 Package（ProBuilder、Visual Effect Graph 等）。

## 参考文档

- 敌人系统：`docs/modules/combat/enemy-system/`
- 飞行物系统：`docs/modules/combat/projectile-system/`
- 对象池：`docs/modules/infra/pool-system/`
- Luban 配置表：`docs/modules/pipeline/luban-config-system/`
