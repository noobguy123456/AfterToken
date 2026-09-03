# 敌人系统

## 职责

管理敌人的生成、AI 行为、状态机、死亡与掉落。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `EnemyEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Enemy/EnemyEntity.cs` | 敌人表现实体（含头顶血条） |
| `EnemySpawnSystem` | `Assets/GameScripts/HotFix/GameLogic/System/EnemySpawnSystem.cs` | 敌人生成 |
| `IEnemyEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IEnemyEvent.cs` | 敌人事件接口 |

## 已完成

- `EnemyEntity` 血量、受伤、死亡。
- 敌人头顶血条：默认位于敌人头顶上方 `0.6` 单位处，使用 `SpriteRenderer` 实现。
  - 背景（黑色半透明）+ 填充（绿/黄/红根据血量比例变色）。
  - 支持 Prefab 预配置血条节点，也支持运行时动态创建占位血条。
  - 血条节点已纳入 `Assets/AssetRaw/Prefabs/Enemy.prefab`，前端可直接在 Prefab 内调整。
- `EnemySpawnSystem` 占位生成。
- 敌人死亡掉落：`DropSystem` 监听 `IEnemyEvent.OnEnemyDied` 按 `TbDrop` 掷点生成 `PickupEntity`（详见 `../pickup-system/`）。
- **敌人对象池**：`EnemySpawnSystem` 使用 `PoolSystem` 预加载并复用敌人 Prefab，死亡后由 `EnemyDeadState` 回池而非销毁。
- **A* 寻路优化**：`PathResult` 接入对象池；`AStarNavigationSystem` 路径平滑改为原地优化，减少 List 分配；`EnemyChaseState` 路径刷新间隔通过 `TbEnemy.pathRefreshInterval` 配置，并按距离动态调整（近快远慢）。
- **复杂地形寻路（2026-09-01）**：障碍按代理半径膨胀（`ColliderGridBuilder.AgentRadius`）、LOS 平滑改 SphereCast、stuck 检测恢复、寻路失败退避收敛、分离可行走校验、生成点可行走校验与吸附；场景障碍统一落 Obstacle（10）层（trigger 交互区物体除外）。
- **敌人半径配置化 + 动态障碍局部更新（2026-09-01）**：①`TbEnemy` 新增 `radius` 字段（碰撞/寻路膨胀半径，米），`ColliderGridBuilder.AgentRadius` 由硬编码 0.3 改为运行时取全表最大 `radius`（表不可用兜底 0.3 并告警一次），LOS SphereCast 半径同步为 `AgentRadius*0.9`，烘焙日志输出实际生效值；②导航网格支持局部更新——`INavigationSystem.UpdateRegion(Bounds)`（`ColliderGridBuilder` 复用 CheckSphere 重扫覆盖格、越界 clamp，`NavigationSystem` 门面更新后清空路径缓存），新增 `NavObstacle` 组件挂到运行时生成/销毁的动态障碍上（OnEnable/OnDisable 以自身 Collider bounds 外扩代理半径触发更新；OnDisable 延迟一帧执行，等销毁的 Collider 移出物理场景；非 Play/导航未初始化/场景切换静默跳过）。场景烘焙前已存在的静态障碍无需挂载。注意：项目关闭了 `Physics.autoSyncTransforms`，组件内部已 `Physics.SyncTransforms()` 兜底。
- **检测/追踪双距离 + 散步状态（2026-09-02）**：`TbEnemy` 新增 `pursuitRange` 字段（追踪距离，默认 10m），`chaseRange` 语义明确为检测距离（默认 5m）；Idle/Wander 中玩家进入 chaseRange → Chase（`EnemyChaseInterceptor`），Chase 中玩家超出 pursuitRange → Wander（新增 `EnemyWanderInterceptor`，优先级 150 介于 Attack 200 与 Chase 100 之间，丢失判定基于 context 新增的 `PlayerOutOfPursuit`，5~10m 滞回区间保持追击不抖动）；新增 `EnemyWanderState`（以进入点为锚点，4m 半径内随机选可走点，`TryGetNearestWalkable` 吸附，移速 ×0.4，到达停 1~2s，3s 走不到换点；攻击/重追仍由拦截器触发）；`EnemyAttackState` 攻击结束时玩家在追踪距离内改为回 Chase 而非 Idle（修复"闪避拉开 5m 后敌人傻站"）；`EnemyChaseState` 删除自身的 `!WantsToChase→Idle` 退出（丢失统一切 Wander）。

## 待完成

- 敌人 AI 类型与配置
- 寻路系统性能调优：分帧调度、缓存失效策略、格子大小调优（A* 分配与频率已优化）

## 设计要点

- 敌人逻辑由系统层驱动，Entity 只负责表现与碰撞回调。
- 生成由 `ProcedureBattle` 按 `TbLevel` 配置（敌人数量/半径/配置 ID）驱动 `EnemySpawnSystem` 完成（环带随机散射）；波次生成（`TbWave`）尚未接入。
- 敌人本体已 3D 化（胶囊占位）；血条仍使用 `SpriteRenderer`，`LateUpdate` 中钉住头顶偏移并做屏幕对齐 billboard（`rotation = Camera.main.transform.rotation`），`sortingOrder` 高于敌人本体以保证可见。
- 死亡回收时 `EnemyEntity` 会自动恢复刚体与碰撞体，保证对象池复用正确。
