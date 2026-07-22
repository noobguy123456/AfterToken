# Enemy System 进度

## 已完成
- [x] `EnemyEntity` 基础表现、血量、受伤、死亡
- [x] `EnemySpawnSystem` 占位生成（固定数量/半径）
- [x] `IEnemyEvent` 事件接口
- [x] 敌人头顶血条：背景 + 填充，随血量变色，Prefab/运行时双支持
- [x] `BattleSceneSetup` 创建带血条结构的 Enemy Prefab
- [x] 增加战斗场景敌人数量：Level 1 从 3 提升到 15，Level 2 从 5 提升到 20
- [x] 敌人 FSM 框架：`EnemyStateContext` / `EnemyStateMachineDriver` / `EnemyStateInterceptor`
- [x] 敌人状态：`EnemyIdleState` / `EnemyChaseState` / `EnemyAttackState` / `EnemyDeadState`
- [x] `EnemyEntity` 接入 FSM：创建/更新黑板、状态机生命周期管理、死亡后延迟销毁
- [x] `IEnemyEvent` 新增 `OnEnemyStateChanged(int enemyId, string stateName, string previousStateName)`
- [x] 修复血条左右晃动：`SetFacing` 改为只翻转身体 Sprite（`_spriteRenderer.flipX`），不再翻转整个 `Transform`
- [x] Play Mode 验证：Idle → Chase → Attack → Dead 切换正常
- [x] 统一敌人与玩家障碍物碰撞效果：`Rigidbody2D` 改为 `Dynamic` + 冻结旋转
- [x] 自研 2D 网格 A* 寻路系统框架：`INavigationSystem` / `INavigationGridBuilder` / `NavigationGrid` / `AStarNavigationSystem` / `ColliderGridBuilder` / `NavigationSystem`
- [x] `EnemyChaseState` 接入寻路：路径跟随 + 近距离直线可达时直接冲刺 + 寻路失败 fallback
- [x] `ProcedureBattle` 初始化导航网格
- [x] 解耦 `EnemyChaseState` 与 `NavigationSystem` 具体类：状态机通过 `EnemyStateContext.NavigationSystem`（`INavigationSystem`）访问寻路
- [x] 限制 `ColliderGridBuilder` 扫描范围：不再全图扫描，以玩家出生点为中心、生成半径 + 余量为边界
- [x] 敌人血条使用共享白色 Sprite，避免每实例创建 Texture
- [x] A* 寻路使用 generation array 替代 `Array.Fill`，提升大网格性能
- [x] `PathResult.Failed` 改为共享只读实例，减少 GC
- [x] `EnemyChaseState` fallback 移动统一使用 `elapse` 参数
- [x] 清理 `NavigationSystem` 未使用的分帧队列 dead code
- [x] `TbEnemy` 已接入 `EnemySpawnSystem` 与 GM 刷怪
- [x] `EnemyEntity` 血量等属性默认值已移除，统一由 `Initialize` 从 `TbEnemy` 注入
- [x] **敌人对象池**：`EnemySpawnSystem` 使用 `PoolSystem` 预加载并复用敌人；`EnemyEntity` 死亡后回池；`EnemyEntity` 复用时自动恢复物理状态
- [x] **血条进入 Prefab**：`Assets/AssetRaw/Prefabs/Enemy.prefab` 已包含 `HealthBarRoot/Background/Fill` 节点，运行时优先使用 Prefab 节点，无 sprite 时自动补白色占位 Sprite
- [x] **`TbEnemy` 新增 `pathRefreshInterval` 字段**：`EnemyChaseState` 路径刷新间隔可配置，并按玩家距离动态缩放（近快远慢）
- [x] **A* 寻路减少分配**：`PathResult` 池化、`ReconstructPath` 复用 List、`SmoothPath` 改为原地平滑

## 进行中
- [ ] Play Mode 验证敌人绕过 `Ground` 障碍物追击玩家
- [ ] 敌人攻击行为与伤害判定（攻击逻辑已写，实际伤害派发待补齐）
- [ ] 接入 `TbWave` 波次生成逻辑

## 待办
- [ ] 敌人攻击行为与技能
- [ ] 精英/BOSS 差异化行为
- [ ] 寻路系统性能调优：分帧调度、缓存失效策略、格子大小调优（A* 分配与频率已优化）
- [ ] 动态障碍物网格更新支持

## 阻塞
- 等待 `TbWave` 表接入波次生成。
- 寻路系统 Play Mode 验证待继续。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
