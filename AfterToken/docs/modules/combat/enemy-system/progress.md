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
- [x] 敌人 3D 化占位（2026-08-27）：`Enemy.prefab` 的 Visual 由平躺 Sprite 改为 3D 胶囊（材质 `M_Enemy_Placeholder`，红色，0.55x0.9m）；`SetFacing` 的 flipX 对胶囊无意义，`Visual` 上无 SpriteRenderer 后自动空操作
- [x] **`TbEnemy` 新增 `pathRefreshInterval` 字段**：`EnemyChaseState` 路径刷新间隔可配置，并按玩家距离动态缩放（近快远慢）
- [x] **A* 寻路减少分配**：`PathResult` 池化、`ReconstructPath` 复用 List、`SmoothPath` 改为原地平滑
- [x] **`TbEnemy` 新增 `chaseRange` 字段**：仇恨（追击触发）范围可配置（当前 5m），替换 `EnemyStateMachineDriver` / `EnemyChaseState` 中硬编码的 8f——此前仇恨范围大于相机视野（约 5m），敌人总是从屏幕外冲进来，体感像"从玩家身上挤出来"
- [x] **敌人生成改为圆环带随机散射**：`EnemySpawnSystem` 由正圆环均匀分布改为 `[spawnRadius, spawnRadius*1.5]` 环带内随机角度/半径散射（生成位置本身经日志验证一直在 12m 环上，并无"同点出生"bug）
- [x] **修复池化敌人被物理回写到原点的真 bug**：项目关闭了 `Physics.autoSyncTransforms`（`DynamicsManager.asset`），池化实例 `SetActive` 后刚体停留在池化位置（原点）；`transform.position` 瞬移不会同步刚体，下一次 FixedUpdate 物理回写把约一半敌人覆盖回原点（与玩家重叠互挤，即"一移动挤出一堆"的根因）。修复：`EnemySpawnSystem` / `GMController` 刷怪瞬移后同步 `Rigidbody.position`。已经 Unity MCP Play Mode 实测：10 敌全部落位环带，atOrigin=0
- [x] **接入 Unity MCP 验证链路**：`http://localhost:8080/mcp`（mcp-for-unity-server），辅助脚本 `.tmp_unity_mcp.py` 支持编译检查 / Console 读取 / Play Mode / `execute_code` 运行时检查
- [x] **血条钉住固定朝向与位置（2026-08-05）**：敌人刚体约束不锁 Y 旋转（`FreezeRotationX/Z` only），物理推挤会让根节点打转，挂在根节点下的血条跟着转。修复：`EnsureHealthBar` 末尾捕获生成时刻的世界朝向/偏移（`_healthBarFixedRotation`/`_healthBarFixedOffset`，对象池复用时随 `Initialize` 重新捕获），新增 `LateUpdate` 每帧钉住 `_healthBarRoot.rotation` 与 `position`。实测：敌人根节点转 137°，血条保持 (0,0,0) 朝向 + 头顶 (0,0.6,0) 偏移不变
- [x] **血条改屏幕对齐 billboard（2026-08-06）**：固定世界朝向在相机偏航旋转后相对屏幕倾斜。修复：`LateUpdate` 中 `_healthBarRoot.rotation = Camera.main.transform.rotation`（血条平面平行屏幕，X=屏幕右/Y=屏幕上，相机偏航/俯仰任意变化角度都不变；相机引用静态缓存避免每帧 Find，无相机时退回 `_healthBarFixedRotation`）。位置仍钉住头顶偏移。实测：相机偏航 0°→60°，血条朝向从 (60,0,0) 同步为 (60,60,0) 与相机完全一致
- [x] **复杂地形寻路判定改造（2026-09-01）**：①修复存量阻塞 bug——A* `_gCost` 复用数组默认 0 导致节点永不扩展（寻路从未真正工作，敌人一直走直线 fallback），新增 `_gGeneration` 代数标记；②L01 场景 Test_Cube/Sphere/Capsule 落 Obstacle 层（Note/LootContainer 为 2m trigger 交互区，不改层）；③障碍按代理半径 0.3m 膨胀（`ColliderGridBuilder.AgentRadius`），LOS 平滑改 SphereCast；④`EnemyChaseState` stuck 检测恢复（0.8s 位移 <0.1m 强制重寻路、连卡 3 次待机 0.5s）+ 失败 fallback 收敛（无 LOS 原地等待退避重试封顶 2s）+ 分离可行走校验；⑤`EnemySpawnSystem`/GM 刷怪生成点可行走校验与吸附（新增 `INavigationSystem.TryGetNearestWalkable`）。Play 实测绕障合围、stuck 日志触发、Console 0 error；截图 `Assets/Screenshots/nav_obstacle_avoidance_mid/final.png`

- [x] **敌人半径配置化（2026-09-01）**：`TbEnemy` 新增 `radius` 字段（碰撞/寻路膨胀半径，米；9001/9002 均填 0.3），`ColliderGridBuilder.AgentRadius` 由硬编码常量改为运行时取 TbEnemy 全表最大 `radius`（表不可用/为空兜底 0.3f 并 Warning 一次），LOS SphereCast 半径同步 `AgentRadius*0.9`，网格烘焙日志输出实际生效值。Play 实测：烘焙日志 `agentRadius=0.3m（TbEnemy 最大 radius）`；运行时反射改 9001 radius=0.6 后 Rebuild 日志变 0.6，改回恢复 0.3，max 取值生效
- [x] **动态障碍物网格更新支持（2026-09-01）**：`INavigationGridBuilder`/`INavigationSystem` 新增 `UpdateRegion(Bounds)`——`ColliderGridBuilder` 对 bounds 覆盖格复用 CheckSphere 重判可走性（越界 clamp、网格空静默忽略）；`NavigationSystem` 门面更新后清空路径缓存防旧路径穿新障碍；新增 `NavObstacle` 组件（Navigation 目录，RequireComponent Collider），OnEnable/OnDisable 以自身 bounds 外扩代理半径触发局部更新，OnDisable 延迟一帧（等销毁 Collider 移出物理场景），非 Play/未初始化/场景切换静默跳过。坑位：项目 `Physics.autoSyncTransforms=false`，运行时生成/移动后立刻读 `Collider.bounds` 是旧值，组件内 `Physics.SyncTransforms()` 兜底。Play 实测（101 关）：运行时创建 1×1×1 立方体（Obstacle 层）→ 不可走格 36→48、路径缓存清空、覆盖格不可走、寻路绕行（路径点距立方体中心 ≥0.89m）、敌人从障碍南侧绕东侧进入攻击状态；销毁后一帧不可走格恢复 36、格子可走。截图 `Assets/Screenshots/nav_dynamic_obstacle_chase/removed.png`
- [x] **检测/追踪双距离分离 + 散步状态（2026-09-02）**：`TbEnemy` 新增 `pursuitRange`（追踪距离，9001/9002 填 10），`chaseRange` 注释语义改为检测距离；`EnemyStateContext` 新增 `PlayerOutOfPursuit`（d > pursuitRange），`WantsToChase` 保持检测语义（d ≤ chaseRange）；新增 `EnemyWanderInterceptor`（优先级 150，仅 Chase 态且丢失目标时切 Wander）与 `EnemyWanderState`（锚点 4m 半径随机选可走点、移速 ×0.4、到达停 1~2s、3s 超时换点）；`EnemyChaseState` 删除 `!WantsToChase→Idle` 退出（避免滞回区间掉回待机）；`EnemyAttackState` 攻击结束改为追踪距离内回 Chase。Play 实测（101 关，god）：①7m 外 Idle 不动；②传送进 4.5m 立即追击至攻击距离；③交战/追击中玩家传 7.5m（滞回区间）敌人继续追（从 1.4m 追至 7.5m 再次近身），不再傻站；④玩家传 12m → 进入 Wander，vel=0.80（=2×0.4 恰为散步速度），位置在锚点附近随机漂移（含 1~2s 停留采样到 vel=0）；⑤Wander 中玩家传回 4m → 立即重新追击近身。Console 0 error

## 进行中
- [ ] 敌人攻击行为与伤害判定（攻击逻辑已写，实际伤害派发待补齐）
- [ ] 接入 `TbWave` 波次生成逻辑

## 待办
- [ ] 敌人攻击行为与技能
- [ ] 精英/BOSS 差异化行为
- [ ] 寻路系统性能调优：分帧调度、缓存失效策略、格子大小调优（A* 分配与频率已优化）

## 阻塞
- 等待 `TbWave` 表接入波次生成。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
