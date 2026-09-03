# AfterToken 项目整体 TodoList

> 本文件依据 [`项目架构方案.md`](./项目架构方案.md) 与 [`开发计划方案.md`](./开发计划方案.md) 整理。
> 每个模块的详细任务见 `docs/modules/<category>/<module-name>/progress.md`。
> 配置表方案：**Luban**（Excel 源数据 → `cs-newtonsoft-json` 代码 + JSON 数据 → YooAsset 热更）。

---

## 图例

| 符号 | 含义 |
|------|------|
| ✅ | 已完成 |
| 🟡 | 进行中 / 基础版完成 |
| ⏳ | 待办 |
| 🚧 | 阻塞 / 强依赖其他模块 |

---

## 一、里程碑规划

| 里程碑 | 目标 | 预计时间 | 关键交付 |
|--------|------|----------|----------|
| **M1 战斗闭环** | Luban 接入；战斗核心数值配置化；波次/胜负判定跑通；Play Mode 全流程验证 | 2 周 | 可玩的战斗循环 |
| **M2 战斗完整** | 音特效、奖励结算、存档、设置、相机抖动 | 1-2 周 | 战斗有完整反馈与持久化 |
| **M3 共享层** | 玩家档案、货币、背包、解锁 | 1 周 | 战斗奖励可落入玩家数据 |
| **M4 经营玩法** | 经营场景、建筑、生产、工人、农场、订单、经营 UI | 2-3 周 | 经营可独立循环 |
| **M5 联动与优化** | 战斗↔经营奖励、强化/训练、性能优化、热更/真机测试 | 2 周 | 双玩法闭环，可出包 |

---

## 二、模块状态总览

### UI 系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| UI Prefab 工作流 | ✅ | - | - | `docs/modules/ui/ui-prefab-workflow/` | 全部热更域 UI 已 Prefab 化并放 `AssetRaw/UI/` |
| LoadingUI 与场景过渡 | ✅ | - | - | `docs/modules/ui/loading-system/` | `GameplayProcedureBase` 统一加载 |
| 命中反馈 | ✅ | - | - | `docs/modules/ui/hit-feedback-system/` | 伤害飘字、受击指示、命中标记 |
| 光标系统 | ✅ | - | - | `docs/modules/ui/cursor-system/` | 显示/隐藏、锁定模式、自定义光标纹理 |
| 设置 UI | 🟡 | P2 | - | `docs/modules/ui/settings-ui/` | 灵敏度/开镜灵敏度/开镜模式/按键改绑/准星样式颜色（含可视化预览）已完成并全部入档；待音量、画质页签 |
| 经营 UI | 🟡 | P1 | M4 经营系统 | `docs/modules/ui/simulation-ui/` | 渲染架构已统一 Overlay；三个经营窗口已正式 Prefab 化并集中到 `AssetRaw/UI/Simulation/`；待办：UI 特效（序列帧） |

### 战斗系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 输入系统 | ✅ | - | - | `docs/modules/combat/input-system/` | 移动、瞄准、开火、换弹、切枪、闪避、武器轮盘 |
| 玩家系统 | ✅ | - | - | `docs/modules/combat/player-system/` | `PlayerEntity` + FSM + 体力系统 + HP/体力条 HUD；`TbPlayer` 已接入并应用属性；3D 胶囊占位已替换 2D Sprite（2026-08-27） |
| 武器系统 | ✅ | - | - | `docs/modules/combat/weapon-system/` | 4 槽位（含火箭筒=直线爆炸物）、开火/换弹/辅助瞄准、3D 占位模型挂载（`WeaponMountView`）、武器轮盘四格；`TbWeapon` 已通过 `WeaponConfigMgr` 接入 |
| 弹道系统 | ✅ | - | - | `docs/modules/combat/ballistic-system/` | Raycast / Projectile 分发、Debug 射线；火箭激光常开且从枪口射出（2026-08-30） |
| 飞行物系统 | 🟡 | P1 | - | `docs/modules/combat/projectile-system/` | 基础已完成；快照遍历修复、寿命终点引爆、爆炸特效 shader 化（火球+冲击波，2026-08-30）已落地；待逻辑/视觉分离以支持弹幕（见 `docs/Proposal/combat/bullet-logic-visual-separation.md`） |
| 辅助瞄准系统 | ✅ | - | - | 并入武器系统文档 | 仅辅助磁吸（火箭锁定已随火箭筒直射化移除，2026-08-30） |
| 相机系统 | 🟡 | P1 | - | `docs/modules/combat/camera-system/` | 跟随、边界、抖动、Duckov 式狙击镜、玩家屏幕锚点（底部 1/4）、经营/战斗参数统一（`TbCamera3D` 单源）已完成；待关卡边界限制 |
| 小地图系统 | ✅ | - | - | `docs/modules/combat/minimap-system/` | 2D 烘焙缩略图（无光影）+ 敌我图标投影 + M 键大地图；打包需加 Unlit/Color 到 Always Included Shaders |
| 敌人系统 | 🟡 | P1 | 关卡/战斗系统 | `docs/modules/combat/enemy-system/` | `EnemyEntity`、生成、`TbEnemy` 已接入；FSM + 自研 A* 寻路已跑通并 Play 实测绕障（障碍膨胀/stuck 恢复/生成点校验，2026-09-01）；3D 胶囊占位已替换 Sprite；待 `TbWave` 波次接入、攻击伤害判定 |
| 掉落与拾取系统 | ✅ | - | - | `docs/modules/combat/pickup-system/` | 敌人死亡掉落、`PickupEntity`、拾取入临时背包已完成 |
| 战利品容器系统 | ✅ | - | - | `docs/modules/combat/loot-container-system/` | 搜打撤开箱全链路已实测（`TbLootContainer` 权重表 + E 键开箱面板 + 单格拿取/Take All）；待统一 IInteractable 仲裁器（Portal/Container 触发区重叠）、美术替换、正式摆放规则 |
| 撤离点系统 | ✅ | - | - | `docs/modules/combat/extraction-system/` | 撤离圈+倒计时（`TbLevel.extractionTime`）+敌人进圈暂停+顶部 UI+撤离结算复用 `PortalSystem.ExtractToBase`，端到端 Play 实测通过；101 已摆 (18,0,18)；待美术替换占位圆盘、正式点位规则、Portal/撤离圈重叠仲裁 |
| 战斗系统 | 🟡 | P0 | 事件系统完善 | `docs/modules/combat/battle-system/` | 伤害、死亡，待暴击/Buff/结果事件 |
| 关卡系统 | 🟡 | P1 | 事件系统 | `docs/modules/combat/level-system/` | `TbLevel` 已接入；硬编码表已替换；待波次/胜负/配置化 |
| 奖励系统 | ⏳ | P1 | 共享层 | `docs/modules/combat/reward-system/` | 战斗奖励分发 |

### 场景系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 传送门系统 | ✅ | - | - | `docs/modules/scene/portal-system/` | 配置表、核心逻辑、UI、转场、场景摆放、死亡判定防护已完成 |

### 基础设施

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 事件系统 | 🟡 | P0 | - | `docs/modules/infra/event-system/` | 战斗事件已定义，待补齐 `ILevelEvent`/`IBattleResultEvent`/经营/共享事件 |
| 对象池 | 🟡 | P1 | - | `docs/modules/infra/pool-system/` | 通用池已有，待按类型拆分与完善 Preload/ClearAll |
| 流程系统 | ✅ | - | - | `docs/modules/infra/procedure-system/` | `GameplayProcedureBase` + 主菜单/基地(经营)/战斗；大厅流程已废弃，选关挪进基地 |
| 音频系统 | ⏳ | P1 | - | `docs/modules/infra/audio-system/` | BGM / SFX / 音量管理 |
| 特效系统 | ⏳ | P1 | - | `docs/modules/infra/effect-system/` | 统一特效管理模块未立项；爆炸火球/冲击波 shader + 自驱动 Driver 已在 projectile-system 内落地（2026-08-30），可作为后续 EffectSystem 的参考实现 |

### 共享系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 玩家档案系统 | 🟡 | P1 | - | `docs/modules/shared/player-profile-system/`（新增） | 等级/经验已持久化；经验表已配置化（TbPlayerLevel）；通关记录已接入解锁系统 |
| 货币系统 | 🟡 | P1 | - | `docs/modules/shared/currency-system/`（新增） | 金币/钻石/体力已持久化；CurrencyType 通用接口；撤离奖励与经营消耗已对接 |
| 背包系统 | ✅ | - | - | `docs/modules/shared/inventory-system/` | 临时背包（槽位制+容量配置+B 键面板）与仓库（内存态）已完成；仓库持久化待 `save-system` |
| 道具系统 | ✅ | - | - | `docs/modules/shared/item-system/` | `cfg.Item` 扩展 + 4 档稀有度 + 稀有度框 prefab 已完成；使用效果后续接入 |
| 解锁系统 | 🟡 | P2 | 玩家档案系统 | `docs/modules/shared/unlock-system/` | TbUnlock（等级/通关链/金币条件）+ UnlockSystem + LobbyUI 关卡锁已落地；武器解锁消费侧待接入 |
| 跨玩法联动 | 🟡 | P2 | 共享系统、经营系统 | `docs/modules/shared/cross-play-link/` | 撤离→金币/经验/通关记录已落地（CrossPlayLink）；经营产出→战斗强化待强化系统立项 |
| 存档系统 | ✅ | P1 | - | `docs/modules/shared/save-system/` | 单 JSON 文件 + 变动即存 + 版本迁移；货币/档案/仓库/设置四件套已接入并实测跨重启保留；GM `save` 命令可用 |
| 设置系统 | 🟡 | P2 | - | `docs/modules/shared/settings-system/`（新增） | 灵敏度/开镜灵敏度/开镜模式/按键改绑/准星样式颜色均已迁入 SaveSystem；音量、画质待做 |

### 模拟经营系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 经营总控 | ✅ | P0 | - | `docs/modules/simulation/simulation-system/` | `ProcedureSimulation` 加载 `SimulationScene`、相机挂 `SimulationCameraController`、`SimulationInputSystem`；2026-08-05 卡死长期未复现、8 处心跳日志已移除；`SimulationPlayerController` 开刚体插值修复移动抖动 |
| 经营时间 | ✅ | - | - | `docs/modules/simulation/sim-time-system/` | 时间推进、加速/暂停已实现 |
| 建筑系统 | ✅ | - | - | `docs/modules/simulation/building-system/` | 建造、升级、拆除已实现；`BuildingPlacementSystem` 3D 摆放预览/校验/落位已实现 |
| 生产系统 | ✅ | - | - | `docs/modules/simulation/production-system/` | 生产队列、产出结算已实现 |
| 工人系统 | ⏳ | P2 | 建筑系统 | `docs/modules/simulation/worker-system/` | 工人分配、属性成长；MVP 后实现 |
| 农场系统 | ⏳ | P2 | 经营时间 | `docs/modules/simulation/farm-system/` | 种植、生长、收获；MVP 后实现 |
| 订单系统 | ✅ | - | - | `docs/modules/simulation/order-system/` | 订单生成、交付、奖励已实现 |
| NPC 系统 | 🟡 | P1 | - | `docs/modules/simulation/npc-system/` | `TbNpc` + `NpcEntity` + `NpcSystem`（E 交谈驱动对话系统）+ 最小档移动（巡逻往返/对话站住转身）已落地；经营场景 2 个测试 NPC 实测通过 |

### 叙事系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 对话系统 | 🟡 | P1 | - | `docs/modules/narrative/dialogue-system/` | Luban 扁平节点表（已切 CSV 数据源）+ 解释器 + `DialogueUI` MVP 已落地实测；可视化编辑器已就绪（Tools/Dialogue/Dialogue Editor，节点树状图/纵向布局/可拖分栏）；quest:* 词汇已落地；NPC 有任务时对话出任务枢纽选项（Turn in/Accept/Just chatting） |
| 任务系统 | ✅ | - | 对话系统 | `docs/modules/narrative/quest-system/` | MVP 已落地实测：TbQuest/TbQuestObjective + QuestSystem 四态状态机 + kill/collect/extract/flag 四类目标 + QuestLogUI（J 键）+ QuestTrackerUI（左侧常驻追踪 HUD）+ QuestAcceptConfirmUI（接取确认窗）+ NPC 头顶三态标记 + 示例任务链 1001-1003；待任务板实体、GM 命令 |
| 小纸条系统 | 🟡 | - | - | `docs/modules/combat/note-system/` | 已验收（见战斗系统区）；已读标记/收集计数待做 |

### 管线与工具

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 资源管线 | ✅ | - | - | `docs/modules/pipeline/asset-pipeline/` | YooAsset 收集器、SimulateBuild |
| 热更管线 | 🟡 | P1 | - | `docs/modules/pipeline/hotfix-pipeline/` | HybridCLR 环境、DLL 加载，待 AOT 元数据补充验证 |
| **Luban 配置表系统** | ✅ | - | - | `docs/modules/pipeline/config-system/`（总览）<br>`docs/modules/pipeline/luban-config-system/`（详细） | 配置工程已搭建，输出格式已切 JSON；`weapon`/`level`/`player`/`enemy`/`drop`/`item`/`inventory`/`portal` 已定义并接入业务；`buff`/`wave` 数据已存在但尚未接入业务系统 |
| 编辑器工具 | ✅ | - | - | `docs/modules/pipeline/editor-tools/` | BattleSceneSetup、Force Recompile、TMP Migration |

### 全局与支撑系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 设置系统 | 🟡 | P2 | - | `docs/modules/shared/settings-system/`（新增） | 灵敏度/开镜模式已持久化；音量、画质、操作设置待补充 |
| 性能优化 | 🟡 | P3 | - | `docs/modules/pipeline/performance-optimization/`（新增） | A* 分配、敌人对象池、爆炸非分配查询已落地；画质/血条 Draw Call/构建加速待继续 |
| GM / 调试工具 | 🟡 | P3 | - | `docs/modules/pipeline/gm-tools/`（新增） | 编辑器/Development Build 中已提供 `GMController` 控制台与面板（无敌、刷怪、跳关、改时间、重载配置）；显示碰撞盒等工具待补充 |

---

## 三、关键阻塞链

```
Luban 配置表数据补充
    ├── 已完成 → 玩家系统（TbPlayer）、武器系统（TbWeapon/TbLevel/TbItem/TbEnemy/TbDrop/TbInventory/TbPortal）
    ├── 数据已存在但业务未接入 → TbWave（波次生成）、TbBuff（Buff 系统）
    └── 间接阻塞 → 关卡波次/胜负、战斗 Buff 系统

事件系统完善（ILevelEvent / IBattleResultEvent / 共享事件）
    ├── 阻塞 → 战斗系统结果事件
    ├── 阻塞 → 关卡系统波次/胜负
    └── 阻塞 → 奖励系统、跨玩法联动

敌人 AI / FSM / 寻路框架已完成
    ├── `TbEnemy` 已接入 `EnemySpawnSystem`
    └── 仍待更优生成逻辑与 `TbWave` 波次/掉落联动

共享系统（Currency / Inventory / PlayerProfile / Save）
    ├── 背包/道具已实现（内存态）
    ├── 阻塞 → 奖励系统、跨玩法联动、经营系统消耗/产出
    └── 存档系统已落地（四件套持久化） → 货币/档案已接入；待实现 → 奖励系统、解锁系统
```

---

## 四、本周聚焦（M1 第一阶段）

1. **接入 `TbWave` 波次生成**：替换 `EnemySpawnSystem` 硬编码参数，按关卡配置驱动多波次敌人刷新。
2. **补齐事件接口**：`ILevelEvent`、`IBattleResultEvent`、共享层事件接口。
3. **实现关卡胜负判定**：全灭敌人/生存目标触发 `IBattleResultEvent`，并打开结算/传送门。
4. **实现奖励系统**：战斗胜利奖励分发，临时背包转入仓库。
5. ~~实现共享层持久化~~（已完成 2026-08-06）：`SaveSystem` 单 JSON + 货币/档案/仓库/设置已接入。
6. **Play Mode 验证**：`MainMenu → Lobby → Battle → 返回/下一关` 跑通，无明显报错。

---

## 五、全局待验证

| 事项 | 状态 | 说明 | 计划完成里程碑 |
|------|------|------|----------------|
| Play Mode 全流程验证 | ⏳ | MainMenu → Lobby → Battle，需在线验证 | M1 |
| 配置表热更验证 | 🟡 | 修改配置后重新导表已通过；YooAsset 收集器包含 `AssetRaw/Configs/json/` 与 SimulateBuild 运行时加载待验证 | M1 |
| 真机构建流程 | ⏳ | YooAsset 真实包、HybridCLR 出包 | M5 |
| AOT 泛型补充验证 | ⏳ | 打包后无 ExecutionEngineException | M5 |

---

## 六、变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-06-21 | 按项目架构方案与系统分类整理模块 TodoList |
| 2026-06-28 | 重写：增加里程碑、优先级、阻塞链、本周聚焦；配置表方案改为 Luban；拆分共享数据层、新增存档/设置/掉落/性能优化/GM 模块 |
| 2026-06-30 | 全面盘点项目进度；更新 Luban 配置表系统状态为「生成逻辑已跑通，缺数据补充」；同步更新各模块 progress.md 与日报 |
| 2026-06-30 | 整理 `docs/Proposal/` 目录结构（按模块分类）；提出并记录「逻辑子弹与视觉表现分离」弹幕扩展方案；同步更新 `projectile-system` 模块文档、`射击模块实现文档.md`、`CONTEXT.md` |
| 2026-06-30 | 实现敌人头顶血条；更新 `EnemyEntity.cs`、`BattleSceneSetup.cs`；同步更新 `enemy-system` 模块文档与 `射击模块实现文档.md` |
| 2026-06-30 | 实现自动换弹与换弹转圈准星；更新 `WeaponInstance.cs`、`WeaponSystem.cs`、`IWeaponEvent.cs`、`BattleMainUI.cs`、`CrosshairUpdater.cs`；同步更新 `weapon-system` 模块文档与 `射击模块实现文档.md` |
| 2026-06-30 | 修复镜头跟随卡顿；`CameraSystem` 改在 `LateUpdate` 直接读取玩家 `Transform`；玩家 `Rigidbody2D` 启用 `Interpolate`；新增 `CameraFollowMode`（Hard/Exponential/SmoothDamp）；同步更新 `camera-system` 模块文档与 `射击模块实现文档.md` |
| 2026-06-30 | 新增光标管理系统；`CursorManager` 管理光标显示/隐藏与锁定模式；`MainMenuUI`/`LobbyUI`/`WeaponWheelUI` 按需显示；流程切换时强制设置；新增 `cursor-system` 模块文档；同步更新 `射击模块实现文档.md` |
| 2026-06-30 | 光标资源可配置化；`CursorManager` 支持 `SetDefaultCursor`/`SetCursor` 自定义 `Texture2D` 光标纹理；`MainMenuUI.SetupDefaultCursor()` 生成默认箭头光标并支持后续替换为美术资源；更新 `cursor-system` 模块文档与 `射击模块实现文档.md` |
| 2026-06-30 | 修复 Console 编译报错：将自定义枚举 `CursorLockMode` 重命名为 `GameCursorLockMode`，避免与 `UnityEngine.CursorLockMode` 冲突；查看 `Editor.log` 确认编译通过 |
| 2026-06-30 | 修复光标锁死在屏幕中心：`CursorManager.ApplyCursorState` 在 Free 模式不可见时不再提前解锁，显示光标时根据当前 `lockState` 决定是否 `UniTask.Yield()` 等待一帧；注释 `ProcedureMainMenu` 编辑器自动跳转战斗调试代码；补充 `using System;` 解决 `OperationCanceledException` 编译错误；更新 `cursor-system` 模块文档与日报 |
| 2026-07-03 | 搭建敌人 FSM 框架：`EnemyStateContext` / `EnemyStateMachineDriver` / `EnemyStateInterceptor`；实现 `EnemyIdleState` / `EnemyChaseState` / `EnemyAttackState` / `EnemyDeadState`；重构 `EnemyEntity` 接入 FSM 与黑板驱动；`IEnemyEvent` 新增 `OnEnemyStateChanged`；Play Mode 验证状态切换与死亡销毁正常 |
| 2026-07-03 | 统一敌人与玩家障碍物碰撞效果：敌人 `Rigidbody2D` 改为 `Dynamic` + 冻结旋转；`EnemyIdle`/`Attack`/`Dead` 进入时清空速度；修复敌人穿 `Ground` 问题 |
| 2026-07-03 | 实现自研 2D 网格 A* 寻路系统：`INavigationSystem`/`INavigationGridBuilder`/`NavigationGrid`/`AStarNavigationSystem`/`ColliderGridBuilder`/`NavigationSystem`；接入 `EnemyChaseState` 路径跟随；`ProcedureBattle` 初始化导航网格；编译通过；Play Mode 寻路验证待继续 |
| 2026-06-30 | 代码审查与问题整改：`WeaponSystem.GetWeaponInSlot` 替代直接访问私有字段；修复 `InputSystem._weaponWheelUI` 未赋值；迁移 `KeyCode.C` 到 `IBattleInputEvent.OnCycleCrosshairStyle`；`SensitivitySetting` 常量命名与 `PlayerPrefs.Save` 优化；`SettingsUI` 设置字体、`RemoveAllListeners`、移除手动 Layer 切换；`CursorManager.Release()` 释放 CTS；补充 `InputSystem` 缺少的 `using Cysharp.Threading.Tasks`；更新日报 |
| 2026-07-08 | 修复 Portal UI 不可见问题：Prefab `RectTransform.localScale` 归一化并保留 `Canvas`；portal 提示改为英文；Lobby 关卡按钮改为 `Stage X`；运行 TMP 迁移工具将 7 个 UI Prefab 的 Legacy Text 迁移为 `TextMeshProUGUI`；防御性修复 `CursorManager` 窗口切换异常处理；更新 `tengine-dev` skill 与文档；待 Play Mode 最终验证 |
| 2026-07-19 | 修复多个 UI bug：PlayerDeathUI/SettingsUI 按钮无响应（根因 `UIModule.ShowUIImp` 参数污染与 `FindChildComponent` 路径缺前缀）、关闭设置后光标不隐藏（逐帧重试策略）、HitFeedbackUI 遮挡弹窗（层级改为 UILayer.UI）、死亡瞬间传送门导致新场景冻结（PortalSystem 死亡判定 + GamePauseManager.Reset 兜底）；性能 GC 治理：敌人追击态分离检测改非分配物理 API、伤害飘字文本缓存与 struct 写回、寻路缓存复用 List、体力事件仅在数值变化时派发；新增空弹匣按开火键自动换弹；更新 `portal-system` / `cursor-system` / `weapon-system` 文档与日报 |
| 2026-07-20 | 新增道具系统、背包系统与掉落拾取系统：Luban 新增 `cfg.EItemType` / `cfg.TbInventoryConfig` / `cfg.Drop`，扩展 `cfg.Item`；实现 `ItemStack` / `RunInventory` / `Warehouse` / `DropSystem` / `PickupEntity` / `ItemConfigMgr` / `DropConfigMgr` / `InventoryConfigMgr`；新增 `BattleBagUI` / `WarehouseUI` / `ItemSlot` prefab；完成敌人死亡掉落 → 拾取入临时背包 → 胜利转仓库 / 死亡清空 / 回大厅清空的闭环；新建 `item-system` / `inventory-system` / `pickup-system` 模块文档并更新 `docs/TODO.md` / `docs/modules/README.md` / `CONTEXT.md` |
| 2026-07-21 | 修复代码中硬编码的玩家/敌人/武器数值：`PlayerEntity` 移速/闪避属性移除默认值；`PlayerSystem` fallback 集中为常量；`EnemyEntity` / `EnemySpawnSystem` 移除血量/数量/半径默认值；`AimAssistSystem` 辅助瞄准与锁定参数从 `TbWeapon` 读取；`WeaponSystem` 切换冷却从 `TbPlayer` 读取；同步在 `weapon.xlsx` / `player.xlsx` 添加新字段并更新 JSON 数据与 Luban 生成代码 |
| 2026-07-22 | 将剩余硬编码数值全部配置化：扩展 `TbPlayer` 动画名；新增 `TbCamera`（相机参数）、`TbBallistic`（弹道全局参数）、`TbUiConfig`（伤害数字/命中标记/受击指示器/Loading 文本）、`TbPickup`（拾取物半径/缩放/排序）；业务代码 `PlayerEntity` / `CameraSystem` / `BallisticSystem` / `DamageNumberUI` / `HitFeedbackUI` / `LoadingUI` / `PickupEntity` / `WeaponSystem` 改为读取配置；同步更新 Excel/JSON/生成代码/文档；`GameLogic.csproj` 编译通过 |
| 2026-07-22 | 性能优化与敌人系统收尾：A* 寻路 `PathResult` 池化、路径平滑原地优化、`EnemyChaseState` 路径刷新间隔通过 `TbEnemy.pathRefreshInterval` 配置并动态缩放；敌人接入 `PoolSystem` 预加载/回池；爆炸范围伤害改为非分配物理查询；敌人血条节点纳入 `Enemy.prefab` 由前端直接调整；同步更新敌人系统/飞行物系统/对象池/Luban/TODO 文档 |
| 2026-07-22 | 梳理模拟经营系统需求并输出 MVP 开发方案：明确本期实现 `SimTimeSystem` / `BuildingSystem` / `ProductionSystem` / `OrderSystem` / `SimulationSystem` / `ProcedureSimulation` / `SimulationMainUI` 的最小闭环；农场与工人系统本期不做；设计新增 Luban 配置表 `TbBuilding` / `TbProduction` / `TbOrder` / `TbSimTimeConfig` 与共享层 `CurrencySystem` / `InventorySystem` / `PlayerProfileSystem`；新增事件接口 `ISimulationEvent` / `ICurrencyEvent` / `IInventoryEvent` / `IPlayerProfileEvent`；创建 `docs/Proposal/simulation/simulation-mvp.md`；同步更新 7 个模拟经营模块 README / progress.md 与 `docs/TODO.md` / `docs/Proposal/README.md` |
| 2026-07-25 | 实现模拟经营 MVP：新增 Luban 配置表 `TbBuilding` / `TbProduction` / `TbOrder` / `TbSimTimeConfig` 及通用 Bean `ItemExchange` 扩展；实现共享层 `CurrencySystem` / `InventorySystem` / `PlayerProfileSystem`；新增事件接口 `ISimulationEvent` / `ICurrencyEvent` / `IInventoryEvent` / `IPlayerProfileEvent`；实现 `SimTimeSystem` / `BuildingSystem` / `ProductionSystem` / `OrderSystem` / `SimulationSystem`；实现 `ProcedureSimulation` 与 `SimulationMainUI`（代码动态创建 UI，后续替换为正式 Prefab）；`GameApp` 注册 `ProcedureSimulation`；`MainMenuUI` 动态创建“Simulation”入口按钮；修复 `WeaponConfig` / `WeaponSystem` / `EnemySpawnSystem` 因配置表字段缺失导致的编译错误；`GameLogic.csproj` 编译通过 |
| 2026-08-01 | 接入 Unity MCP（mcp-for-unity-server，`localhost:8080/mcp`），实现自助编译检查 / Console 读取 / Play Mode 实测 / `execute_code` 运行时检查（辅助脚本 `.tmp_unity_mcp.py`）。实测修复敌人"同点出生挤出一堆"真 bug：项目关闭 `Physics.autoSyncTransforms`，池化敌人 `SetActive` 后刚体留在原点，`transform.position` 瞬移被下一次 FixedUpdate 物理回写覆盖（约半数敌人中招）；修复为瞬移后同步 `Rigidbody.position`（`EnemySpawnSystem` / `GMController`），复测 10 敌全部落位 12~18m 环带。`TbEnemy` 新增 `chaseRange` 字段（当前 5m）替换硬编码 8f 仇恨范围；敌人生成由正圆环改为环带随机散射。实测发现新阻塞：进入经营流程后 Unity 主线程死循环卡死（100% CPU、日志停在 `SimulationMainUI` 打开前后），待重启后加二分日志定位。同步更新 enemy-system / simulation 各 progress.md、simulation-module-overview（相机控制更正为 `CameraSystem3D`）与本文件 |
| 2026-08-01 | 主菜单恢复并整理 Simulation 入口：`MainMenuUI.OnCreate` 动态克隆 Start 按钮创建绿色 "Simulation" 按钮（插入 Start 下方，点击切 `ProcedureSimulation`，大厅内入口保留）；调整 `MainMenuUI.prefab` 布局：标题字号 80→48 并上移（y=-120→-40，高 120→80），按钮组下移（y=-50）、间距 20→12，按钮顺序改为 Start / Settings / Exit（运行时含 Simulation 共四个）；Play Mode 截图验证布局正常、无 Console 报错 |
| 2026-08-01 | 经营场景集中修复与 UI 重设计：①修复场景内无法移动——根因是没有任何系统接收移动输入（虚拟玩家仅作相机跟随点，`SimulationCameraController` 从未挂载），`ProcedureSimulation` 改为移除 `CameraSystem3D`、挂载 `SimulationCameraController`，废弃虚拟玩家胶囊；②`SimulationMainUI` 重设计——顶部常驻 HUD 条（金币/等级/时间/时间控制）+ 管理面板默认隐藏 Tab 切换；按用户选定"面板 B + 场景牌子 C"方案，新建独立 Screen Space - Camera 根 Canvas（框架 UIRoot 为全局 Overlay，嵌套 Canvas 无法单独切渲染模式），World Space 建筑头顶牌子沿用 `BuildingEntity`；滚动列表 `Mask` 换 `RectMask2D`（模板缓冲裁剪失败导致列表内容全不可见）；③修复建筑/订单配置表运行时为空——`ConfigSystem._tableFiles` 预加载清单缺 10 张表（inventoryconfig/camera/ballistic/uiconfig/pickup/building/production/order/simtimeconfig/camera3d），`LoadAsync` 路径对未列出的表返回空 JArray；④主菜单 prefab 布局整理见上条。同步更新 simulation-module-overview、simulation-ui progress 与本文件 |
| 2026-08-01 | 经营场景加载玩家角色并改为角色中心视角：`ProcedureSimulation.SpawnPlayerAsync` 复用战斗 `Player` prefab（移除战斗 `PlayerEntity`），新增 `SimulationPlayerController`（WASD 移动/面向/边界钳制，移速读 `TbPlayer`）；相机改 `SimulationCameraController` 跟随玩家（跟随模式下禁用 WASD 平移与右键拖动），跟随偏移 (0,7,-5) 贴近战斗视角；`Visual` 占位视觉实例放大 5 倍（0.2m→1m，不动 prefab）；地面染灰绿与天空区分；HUD 新增 `Panel (Tab)` 按钮调出面板的（不只有 Tab 键）。顺带修复 `SimulationMainUI.OnDestroy` 重复调用 `RemoveAllUIEvent` 导致内存池二次释放异常（框架 `UIWindow.InternalDestroy` 已统一释放，其他 UI 存在同款隐患）。Play Mode 实测：角色生成/相机跟随/按钮调面板均正常。同步更新 simulation-module-overview、simulation-ui progress 与本文件 |
| 2026-08-01 | 经营场景"空白无参照、角色看不见"修复：地面改为运行时生成 1m 网格纹理（`ProcedureSimulation.CreateGridTexture`，纯色俯视无移动参照、天地难分）；`Visual` 占位圆点抬高 0.05m——圆点贴地平放与地面共面 z-fighting，是角色在部分视角下闪烁/消失的根因。Play Mode 实测：网格清晰、角色圆点居中可见、HUD 正常、Console 0 报错。同步更新 simulation-system progress 与本文件 |
| 2026-08-01 | 记录经营 UI 相机归属问题（下次重点调整，本次不改代码）：`SimulationUIRoot` 为 SSC Canvas 挂角色相机（透视 60° 俯视跟随玩家），用户实测 Game/Scene 视图均见面板倾斜/"陷入地下"；框架另有 UICamera（正交、depth=2、Depth 清屏、仅 UI 层、合成在场景之上）。用户决策：**UI 统一挂 UI 摄像头，不挂角色相机**；同时梳理场景大小与相机参数差异。已记入 simulation-ui progress「已知问题」与本文件经营 UI 行（状态 🟡、P1）。排查佐证：复现时 Game 视图面板贴屏正常、移到 z=-24 仍正常，运行时双相机参数已核实 |
| 2026-08-01 | 输出 UI 渲染架构统一方案 `docs/Proposal/ui/ui-render-architecture.md`：现状分析——TEngine 无 UI 模块，`GameLogic.UIModule` 为自研（窗口堆栈/UILayer 五层）；UIRoot.prefab 原设计即 UICanvas SSC 挂 UICamera（正交、只渲 UI 层），但 `UIWindow.FixFullScreenCanvas` 强制所有窗口转 Overlay 致 UICamera 空转、`SimulationMainUI` 自建 SSC 挂 Main Camera 致面板倾斜。方案三步核心：解除 Overlay 强制、拆 `SimulationUIRoot` 回归框架 UIRoot、场景相机剔除 UI 层；另含 CanvasScaler 750x1334→1920x1080 横屏修正（可拆分）与全量回归清单 |
| 2026-08-01 | UI 方案修订（用户反馈：Scene 视图"场景小 UI 大"影响调试、不为简单 UI 付多余性能，要求业界成熟方案）：确认项目为 Built-in RP（无 URP Camera Stacking 可用）；**废弃初版"全窗口转 SSC 挂 UICamera"**（会加剧 Scene 视图污染、全窗口回归风险、无性能收益），修订为三层结构——①界面 UI 全部 Overlay（Scene 视图零污染、19 窗口零改动）②UI 特效首选序列帧（Overlay 内直接播），未来 3D 粒子再启用 UICamera 特效层（UI 层物体锚定 x=1000 远区）③场景内 UI 保持 World Space；场景相机剔除 UI 层、UIRoot.prefab 的 UICanvas 对齐改 Overlay。纠正认知：渲染模式本身性能影响可忽略，UI 性能瓶颈在 Canvas rebuild/overdraw/合批。改动清单缩为三步（经营 UI 回归框架 / UIRoot.prefab 对齐 / 场景相机 mask），CanvasScaler 横屏修正单独再做 |
| 2026-08-01 | UI 渲染架构统一方案落地（三步全部完成并实测）：①`SimulationMainUI` 拆除自建 SSC Canvas，`SimulationUIRoot` 改为纯 RectTransform 容器挂框架 UIRoot（Overlay），按设计分辨率 1920x1080 固定尺寸 + 反向缩放抵消 CanvasScaler 影响（首版全屏拉伸容器导致顶锚 HUD 缩到屏幕中部，改为固定尺寸居中容器修复）；②`UIRoot.prefab` UICanvas renderMode SSC→Overlay、UICamera 设 inactive 备用；③`ProcedureBattle`/`ProcedureSimulation` 场景相机 `cullingMask` 剔除 UI 层（实测 mask=-33）。Play Mode 实测：主菜单正常、经营 HUD/面板任意位置贴屏平整（含 z=-24 边界）、战斗 HUD/敌人血条/传送门牌子正常，Console 0 报错。遗留：传送门牌子中文显示为方框（TMP 字体缺中文字形，疑为旧问题，待确认）；排查插曲——编辑器未自动感知磁盘脚本变更导致 Play 跑旧代码报已修复的编译错误，`AssetDatabase.Refresh(ForceSynchronousImport)` 后正常。同步更新 simulation-ui progress（摘掉已知问题）与本文件经营 UI 行 |
| 2026-08-02 | 全场景网格统一 + 建筑占地与占位模型落地：①用户拍板全局基础格 **1m**、寻路子格 0.5m，新建 `MapGrid`（`BaseCellSize`/`NavCellSize`/`Snap`/`GetFootprintCells`，奇数格中心对整数、偶数格对 x.5 格缝）作为全项目唯一网格尺寸来源；②`TbBuilding` 新增 `footprintX/Z` 字段（工坊/农场 4x4、贸易站/装饰 2x2），xlsx + Luban 直跑命令再生成（bat 在 Git Bash 下不可用，改用 `Tools/Luban/Luban.exe -t client -c cs-newtonsoft-json -d json --conf luban.conf ...`）；③`BuildingSystem` 新增网格占用表（建造吸附 + 占地查重、拆除释放、`IsAreaFree`）；④新建 `BuildingModelFactory` 按建筑类型拼装占位方块模型（工坊棕主体+烟囱 / 农场田块+绿作物 / 贸易站蓝主体+雨棚 / 装饰白底座+金立柱），`BuildingEntity` 默认模型与摆放预览共用，标签高度按类型适配；⑤`BuildingPlacementSystem` 重构：预览改真实占地占位模型（Ignore Raycast 层）、落点校验改占用表、摆放模式显示 1m 网格线（取消即销毁）；⑥`NavigationSystem._cellSize` 固定引用 `MapGrid.NavCellSize`（值不变，战斗零影响）。顺带修复两个存量 bug：`ConfigSystem._tableFiles` 漏 10 张表（此前已修清单但这次发现运行时建筑表为空，本次完整补齐并实测 count=4）；`BuildingEntity` 模型加载返回 null 无回退（实体 0 renderer 不可见）+ 代码拼装模型被 `UnloadAsset` 误卸载抛 `GameFrameworkException`（改 `_modelFromResource` 区分 Destroy/Unload）。Play Mode 实测：4 建筑占位模型外观/占地吸附（偶数格中心 x.5）/占地拒绝重叠/拆除释放/摆放网格线出现消失/战斗场景 A* 冒烟全部通过。注意：实测期间编辑器被反复置为 Pause（非项目代码所为，疑外部操作），导致时间/建造进度冻结，排查时先查 `EditorApplication.isPaused`。同步更新 building-system progress 与本文件 |
| 2026-08-02 | 修复经营 UI 全部无法点击：根因——`SimulationMainUI`/`BuildingSelectionUI` 共用占位 `TestUI.prefab`，带 Canvas + 全屏半透明 Image 但**无 GraphicRaycaster**；UGUI 图形按最近父 Canvas 注册，框架 UICanvas 的 Raycaster 管不到窗口自身 Canvas 下的图形，`EventSystem.RaycastAll` 全屏 0 命中。修复：`TestUI.prefab` 补挂 `GraphicRaycaster`（PrefabUtility 编辑，所有 TestUI 占位窗口一并生效）。实测：HUD/面板按钮射线命中正常，Build → BuildingSelectionUI → 点选建筑进入摆放模式全链路通过。记录规范：代码动态创建窗口的占位 Prefab 必须自带 Canvas + GraphicRaycaster（框架 `UIWindow.OnPrepare` 约定）。同步更新 simulation-ui progress 与本文件 |
| 2026-08-02 | 经营建造交互修复：①消除"点击建造后报错"——根因是建筑 `icon` 配置为占位地址（`icon_building_*` 无对应资源），`BuildingEntity` 尝试加载时资源模块打 ERROR；改为加载前 `CheckLocationValid` 前置校验，无效地址直接走占位模型，Console 恢复 0 报错。②按用户偏好改交互：管理面板建筑列表项原点击即在原点 `TryBuild`（无预览、无选址），改为点击进入摆放模式（`SimulationMainUI.EnterPlacement`：关面板 + `StartPlacement`，玩家自己选位置放置），与 `BuildingSelectionUI` 行为统一。实测：面板点击建筑项 → 面板关闭 + 摆放模式开启 → 放置成功、实体 2 renderer、Console 无报错。同步更新 simulation-ui progress 与本文件 |
| 2026-08-02 | 修复摆放网格线错位 + 新增 R 键旋转：①网格线错位——网格线此前画在格中心（整数坐标），而 `MapGrid` 约定格中心在整数、边界在 x.5，导致线条与建筑实际占地恒差半格，玩家按线摆放时占地校验不通过"放不下去"；修复为线画在格子边界 x.5（实测 4x4 预览边缘与线严格重合，占地边界数值校验一致）。②R 键旋转——`BuildingPlacementSystem` 新增 `_rotationY`（0/90/180/270），摆放中按 R 转 90°；旋转 90/270 时占地 X/Z 对调，预览吸附/占用校验/建造全链路一致；`BuildingSystem.TryBuild` 新增 `rotationY` 参数（旧签名保留转发），`CreateBuildingEntityAsync` 按朝向创建实体。实测：90° 建造农场实体 rotY=90、占地/Console 正常。同步更新 building-system progress 与本文件 |

| 2026-08-02 | 修复"绿色预览却放不下去" + 摆放失败反馈与连续摆放：①真因——预览染色只查占地，`TryBuild` 里金币/材料不足、同类型建筑已存在三种隐性失败无任何提示；新增 `BuildingSystem.CanBuild(configId, position, rotationY, out reason)` 统一校验（配置错误/当前位置无法放置/该建筑已存在/金币不足/材料不足），`TryBuild` 先调 CanBuild，`BuildingPlacementSystem.CanPlaceAt` 改走 CanBuild（红色预览覆盖全部失败原因）。②失败飘字——`BuildingPlacementSystem.ShowFloatText` 世界空间 Canvas + legacy `UnityEngine.UI.Text`（`LegacyRuntime.ttf`，项目 TMP 字库 Latin-only 中文是方框，中文 UI 文字一律走 legacy 动态字体），内嵌 `FloatTextAnim` 上飘淡出 1.2s。③连续摆放——放置成功/失败均不退出摆放模式（仅右键/Esc 取消），失败时飘字显示具体原因。实测：无金→金币不足、有金无材料→材料不足、全齐→pass；无效点击后 `placing=True` 保持 + 飘字中文清晰显示。注意：排查"时间不走/截图抓不到"先查 `EditorApplication.isPaused`（本次又是外部置 Pause 所致）。同步更新 building-system progress 与本文件 |

| 2026-08-02 | 测试期默认物资：`ProcedureSimulation.GrantTestMaterials` 进入经营流程时遍历 `TbBuilding` 全部建造/升级材料，每种补足到堆叠上限（实测 Wood/Stone 各 x99，槽位 2/200），避免测试被"材料不足"卡住；正式经济循环接入后移除。Play Mode 反射调用实测通过，Console 0 报错。同步更新 building-system progress 与本文件 |

| 2026-08-02 | 同类型建筑数量上限三方式并存解锁落地：①`TbBuilding` 新增 `maxCount`/`maxCountPerPlayerLevel`/`maxCountUpgradeLevel`/`maxCountSlotBaseCost`/`maxCountSlotCostGrow` 五字段（`__beans__.xlsx` comment 列与 `building.xlsx` 注释行均已备注配置方法；默认解锁方式为升级解锁——同类每有 1 座达到 maxCountUpgradeLevel 上限 +1）；②`BuildingSystem` 新增 `GetMaxCount`（基础+玩家等级+升级解锁+已购栏位四种叠加）/`CountByConfig`/`GetSlotPrice`（线性涨价）/`TryPurchaseSlot`，`CanBuild` 重复检查改"数量已达上限"；③管理面板建筑项显示 `[当前/上限]` + 右侧 Unlock 按钮（TMP 无中文字形，面板文本维持英文）。Play Mode 实测 15/15 PASS（基础上限/上限拦截/购买+涨价/升级解锁 Lv3→+1/玩家等级解锁/装饰基础 3）。事故与修复：Luban bat 复制桥接文件步骤用旧模板覆盖 `GameProto/ConfigSystem.cs`，此前未提交 git 的"`_tableFiles` 补 10 张表"修复丢失、运行时建筑表变空；已将 19 张表清单同时写入 GameProto 与 `Configs/GameConfig/CustomTemplate/ConfigSystem.cs` 模板源头，并记录教训"改 GameProto/ConfigSystem.cs 必须同步改 CustomTemplate"。同步更新 building-system progress 与本文件 |

| 2026-08-02 | 建筑数量上限提升条件显示 + 摆放 ESC/右键退回建筑选择 UI：①管理面板建筑项第三行显示提升途径（`SimulationMainUI.BuildUnlockHint`：升级解锁 `+1 slot at building Lv{n}` / 玩家等级解锁 `+N slot per player Lv`，购买途径由 Unlock 按钮价格体现）；②`BuildingPlacementSystem.ExitToBuildingSelection`——ESC/右键取消摆放并打开 `BuildingSelectionUI`（原为直接退出建造流程）；③修复按键冲突：摆放中按 ESC 此前会同时触发取消摆放 + `SimulationInputSystem` 弹出设置面板，现增加 IsPlacing 拦截。实测：4 建筑提升条件文本正确、反射调用退出后 IsPlacing=False 且 BuildingSelectionUI 实际打开。同步更新 building-system progress 与本文件 |

| 2026-08-02 | 建筑信息面板 + 经营交互修正四项：①新建 `BuildingInfoUI`——非摆放模式左键点击场景建筑打开（`BuildingPlacementSystem.TryOpenBuildingInfo`，EventSystem 防 UI 穿透，`PendingInstanceId` 支持已开窗口切换目标）；左侧产出（进行中队列进度 + `TbProduction` 配方列表带 Start 直接投产），右侧建筑名+Lv+状态+模型快照（临时相机拍一帧到 RenderTexture，实体临时切 layer 30 防混入他物）+Upgrade 按钮；ESC 关闭加入 SimulationInputSystem 关闭链首位。②摆放 ESC/右键由"退回 BuildingSelectionUI"改为退回 Management 面板（`ExitToManagement` → `SimulationMainUI.OpenManagementPanel`）。③Management 列表自适应滚动：Content 补 `ContentSizeFitter`（此前高度恒等于 viewport 导致无法滚动）+ 垂直 Scrollbar（AutoHideAndExpandViewport）。④删除 Management 的 Upgrade 占位按钮（原 TODO 无脑升级第一个建筑），升级入口移至建筑信息面板。坑位记录：UIWindow 非 MonoBehaviour（刷新用 `OnUpdate`、销毁用 `Object.Destroy`）；动态 UI 需 1920x1080 固定容器 + 反向缩放抵消根 CanvasScaler 2.56 倍放大（首版 BuildingInfoUI 直接画 Canvas 被放大出屏）。Play Mode 实测：信息面板布局/快照/配方列表截图验证、StartPlacement→ExitToManagement 后 IsPlacing=False 且 ManagementPanel 展开、滚动条 AutoHide 行为正确。同步更新 building-system progress 与本文件 |

- 2026-08-02 建筑系统交互收尾：修复 TestUI 根 Image 挡射线（左键放不下去/点不开信息面板的根因）、`BuildingEntity.EnsureClickCollider` 补点选碰撞体（注意工厂 Destroy 帧末生效的误判坑）、ESC 关闭链加入 Management 面板（右上角 X 按钮同步加入）；规定设置面板仅在无菜单 UI 时按 ESC 弹出；R 旋转功能正常但正方形占地+对称占位模型导致视觉无差异（需要可见朝向时给模型加非对称部件）。详见 docs/modules/simulation/building-system/progress.md

- 2026-08-03 修复 Management 面板滚动时相机同时缩放：`SimulationCameraController` 在指针悬停 UI 时拦截滚轮缩放与右键拖动起拖（规则：悬停任何 UI 时视角操作不生效）。详见 docs/modules/simulation/building-system/progress.md

- 2026-08-03 建筑信息面板升级失败显示原因：`BuildingSystem.CanUpgrade` 输出中文失败原因（忙碌/满级/金币不足/材料不足），`BuildingInfoUI` 升级按钮下方红字显示 3 秒（legacy 动态字体，中文不走 TMP）。已实测。

- 2026-08-03 规范更新：所有新 UI 必须做成正式 Prefab（禁止 TestUI 占位 + 代码拼装），遗留三个代码拼装窗口（SimulationMainUI/BuildingInfoUI/BuildingSelectionUI）下次结构性改动时迁移；同时澄清字体例外——TMP 字库 Latin-only，中文动态文本允许 legacy Text + LegacyRuntime.ttf。详见 docs/standards/UI_STANDARDS.md 与 CODE_REVIEW_CHECKLIST.md

- 2026-08-03 经营三窗口（SimulationMainUI/BuildingSelectionUI/BuildingInfoUI）Prefab 化完成并 Play 实测通过：静态结构入 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`，脚本改 `ScriptGenerator()` 绑定；动态列表项仍运行时生成。同时所有面向用户文本改英文（CanBuild/CanUpgrade/TryPurchaseSlot 失败原因等），BuildingInfoUI 失败红字改回 TMP；"新 UI 必须 Prefab + 文本英文优先（直到用户许可中文）"两条规则已写入 docs/standards/UI_STANDARDS.md 与 CODE_REVIEW_CHECKLIST.md

- 2026-08-03 经营 UI 归拢目录：三个窗口脚本移到 `GameLogic/UI/Simulation/{Name}/`，prefab 移到 `AssetRaw/UI/Simulation/{Name}/`（地址按文件名解析不受影响，已 Play 实测加载正常）；UI_STANDARDS §2.1 三者一致原则补充模块子目录规则

- 2026-08-05 修复敌人血条跟随敌人打转：`EnemyEntity` 刚体不锁 Y 旋转，物理推挤导致根节点旋转、血条跟着转；改为 EnsureHealthBar 捕获固定世界朝向/偏移 + LateUpdate 每帧钉住，实测敌人转 137° 血条不动。详见 docs/modules/combat/enemy-system/progress.md

- 2026-08-08 战斗三系统 review 与整改：①子弹系统修复 3 个存量 bug——双重命中（伤害唯一权威路径收敛为 `ProjectileSystem.Tick` SphereCast→HandleHit，`OnProjectileHit` 事件处理器改空钩子）、火箭追踪失效（`FireProjectile` 传 `Transform.GetInstanceID()` 改为 `EnemyEntity` 组件 InstanceID，与 `_enemyMap` 键语义统一）、飞行中视觉不跟随（Tick 每帧驱动 `UpdateVisual`）。②玩家状态收敛——IsDead/IsDodging/MoveInput/AimInput/IsAiming 唯一 owner 归 `PlayerStateContext` 黑板，`PlayerEntity`/`WeaponSystem` 重复字段改转发属性，零行为变化；三条实现原理与状态归属表记入 `docs/modules/combat/player-system/README.md`。③产出提案 `docs/Proposal/combat/pure-csharp-data-oriented-roadmap.md`（纯 C# 数据导向演进路线：子弹 SoA 样板/敌人数据层/敌人感知黑板，待评审，评审前不改代码）。热更程序集编译通过、Console 0 错误 |

- 2026-08-08 狙击镜优化两批：①效果调整——倍率减半（`TbWeapon.scopeFov` 37.5→75，即 1.2x→0.6x，Excel+JSON 同步）、镜外暗角更通透（有效不透明度约 30%）、开镜命中伤害数字入镜窗（`BattleSystem`→`SniperScopeUI.ShowScopeDamage`，镜相机取景换算，复用 `DamageNumberUI` 池与动画）；顺带修复开镜期间 `DamageNumberUI` 被框架隐藏导致飘字冻结（`TickExternal` 代驱动）与关镜残留销毁对象两个存量问题。②开镜/不开镜灵敏度分离——`SensitivitySetting.ScopedValue` 独立存档、设置面板新增 Scope Sensitivity 滑块、`CrosshairUpdater` 开镜时切换灵敏度；镜窗改跟随准星而非原始鼠标位置，统一镜窗/准星/子弹落点。均 Play Mode 实测截图验证。详见 weapon-system、settings-ui progress |

- 2026-08-08 狙击镜视觉模型修正：①修复开镜切武器卡镜（`SwitchToSlot` 取消瞄准移到换槽前）；②镜相机改"视场放大镜"模型——与主相机同位仅旋转对准瞄准点，镜内=主相机视野放大裁剪，解决开镜后只能看到玩家附近的问题；③scopeFov 75→37.5 恢复 1.2x。实测切武器关镜、镜内远景可见。详见 camera-system README 与 weapon-system progress

- 2026-08-08 狙击镜抬高+压边平移（二版，后废弃）：镜相机架到瞄准点上空 12m、FOV 按高度比例换算保持视觉 1.2x；开镜瞄准射线改从镜相机发出，准星压边驱动瞄准点限速平移（8m/s），射程不再受主相机视锥限制。两个反馈环陷阱：镜相机旋转必须固定（LookRotation 跟踪会在平滑滞后期拉平视轴致瞄准距离发散）、平移必须限速。实测平移 7.9m/s 线性、回中即停。用户按鸭科夫参考图拍板改纯放大镜，机制回退。详见 camera-system README 留档

- 2026-08-08 狙击镜改纯视觉镜窗（三版，按鸭科夫参考图+用户四点需求）：①开镜=灰色蒙版+跟随准星的镜窗图案（圆环+贯径十字线，≈0.59 屏高，去中心点）；②默认无放大无畸变——`TbWeapon.scopeFov` 语义改 0=无放大纯视觉镜窗、>0=放大镜模式（1004 默认 0，Excel+JSON+__beans__ 注释同步）；③`WeaponInstance.ScopeFov` 删 15 兜底直接透传、`IsScopedSniping` 不再要求 scopeFov>0；④开镜射击直接命中镜窗中心（瞄准射线始终来自主相机过准星）；⑤蒙版随后改带圆孔——孔内零遮挡正常渲染、孔外压灰，注意力聚焦镜窗（像素级验证通过）。实测开火命中、Console 0 错误。详见 camera-system README 与 weapon-system progress

- 2026-08-08 狙击镜命中/后坐力反馈：①命中标记——订阅 `IHitFeedbackEvent.OnHitTarget`（白/暴击橙）+ `IBattleEvent.OnEntityKilled`（红，仅玩家击杀），镜窗中心四刺 × 标记 punch 缩放+0.25s 淡出（开镜时 HitFeedbackUI 被框架隐藏，镜内自绘）；②后坐力——订阅 `IWeaponEvent.OnFire`，镜窗连同蒙版圆孔上跳 30px+横向 ±7px，指数回弹。实测 kick 回弹归零、标记 punch+淡出逐帧衰减、开火命中正常。详见 camera-system README 与 weapon-system progress

- 2026-08-17 新增搜打撤「战利品容器（开箱）」功能：Luban 新表 `TbLootContainer`（权重掉落表）；`LootContainerEntity`/`LootContainerSystem`/`LootContainerUI` 全套（复用传送门交互范式与 ItemSlot 格子）；Esc 关闭链与 `ProcedureBattle` 系统挂载已接入；101 关放 2 个测试容器。遗留：Portal/Container 触发区重叠时需统一 IInteractable 仲裁器

- 2026-08-18 UI 缩放体系统一（修复 LobbyUI 关卡界面溢出屏幕）：根因——`UIRoot.prefab` 的 UICanvas CanvasScaler 沿用 TEngine 竖屏默认 750×1334 / match=宽，1920×1080 下 scaleFactor=2.56，逻辑屏仅 750×422，所有按 1920×1080 编写的硬编码像素布局整体放大 2.56 倍出屏（且随窗口宽度变化）。修复：①`UIRoot.prefab` CanvasScaler 改 1920×1080 / match=0.5（1920×1080 下 sf=1）；②`LobbyUI` 代码原假设中心锚点硬写坐标（标题 (0,400)、底部按钮 y=-400），与 prefab 实际锚点（标题顶部中心、按钮左下角）叠加导致二次偏移——改为显式设锚点：标题顶部中心 (0,-80)，Back/Warehouse/Simulation 底部中心 (±300/0, 60)；③三个经营窗口（SimulationMainUI/BuildingSelectionUI/BuildingInfoUI）的 `1/scaleFactor` 反向缩放补偿代码移除（sf=1 后已无意义）。规范更新：`docs/standards/UI_STANDARDS.md` §5.2 统一设计分辨率 1920×1080、禁止反向缩放。Play 实测：主菜单/Lobby 全部元素在窗口内，编译 0 错误 |

- 2026-08-18 修复战利品箱子占位图标闪烁：容器根节点在 y=0，平躺 SpriteRenderer 与地面共面 z-fighting，`EnsureVisualRenderer` 将 Visual 抬高 0.05m；经验：贴地占位 SpriteRenderer 都要留离地偏移。并在 loot-container-system progress 补充「如何新增可开启箱子」配置流程 |

- 2026-08-19 新增小纸条叙事系统：Luban 新表 `TbNote`（id/title/content，content 支持 `\n` 换行，2 条英文测试数据，白名单同步）；`NoteEntity`（trigger + 纸白色占位视觉，抬 0.05m 防 z-fighting）/ `NoteSystem`（E 阅读、提示 UI、死亡闸、出区自动关，ProcedureBattle 挂载）/ `NoteUI`（640x360 ≈ 1/3 屏，不暂停，Esc 链接入）；101 关放测试纸条 Note_1。实测：提示→E 开→E 关链路通，0 错误。详见 docs/modules/combat/note-system/
- 2026-08-19 修复开箱/背包/纸条 UI 打开时仍可射击：`InputSystem.IsMenuUIOpen()` 拦截射击与瞄准输入，UI 打开瞬间补发释放事件防卡键。详见 docs/modules/combat/input-system/progress.md
- 2026-08-19 修复看纸条/开箱时鼠标被"强制挪动"：菜单 UI 打开期间隐藏的准星仍累加鼠标位移、瞄准射线继续驱动角色朝向；改为 `CrosshairUpdater` 在系统光标可见时冻结 + `InputSystem` 屏蔽瞄准事件，关 UI 后准星原地继续。详见 input-system progress

- 2026-08-19 修复纸条两条遗留：①"瞄点还是会变"——瞄准输入屏蔽后 AimPosition 冻结但 `PlayerEntity.Update` 仍朝旧瞄点旋转，玩家移动时角色自转；改为菜单 UI 打开时朝向完全冻结（`InputSystem.IsMenuUIOpen()` 改 public static）。②关闭纸条后 Windows 鼠标仍显示（光标引用计数泄漏，实测关后 refCount=1）；`CrosshairUpdater.Update` 新增兜底——战斗中无菜单类 UI（含 WeaponWheelUI 豁免）且光标可见时 `ForceHideCursor()` 强制恢复隐藏+锁定。编译 0 错误，待用户手动验收。详见 input-system progress

- 2026-08-20 共享/联动侧四模块落地：①解锁系统——Luban 新表 `TbUnlock`（unlock.xlsx；条件=玩家等级+通关关卡链+金币价格；`UnlockContentType` 枚举 Level/Weapon；未配置内容默认开放）+ `UnlockSystem`（IsUnlocked/TryUnlock/GetLockHint/Reset）+ `IUnlockEvent` + 存档 unlock 段；②玩家档案——经验表配置化 `TbPlayerLevel`（playerlevel.xlsx，缺配置回退 level*100）+ 通关记录 `MarkLevelCompleted/IsLevelCompleted`（profile 段 `completedLevels`）；③货币——`CurrencyType` 枚举与通用 GetAmount/Has/Add/TryConsume；④跨玩法联动——`CrossPlayLink.OnBattleExtracted` 挂接 PortalSystem 撤离分支，`TbLevel` 新增 rewardGold/rewardExp 列（LevelConfig 适配器透传），撤离发金币/经验并记录通关驱动关卡链解锁；LobbyUI 关卡按钮按 UnlockSystem 置灰+英文条件提示；GM 新增 gold/exp/unlock/profile 命令，save clear 重置解锁记录。坑位记录：Luban bean 必须在 `__beans__.xlsx` 显式定义（数据 xlsx 只放数据行），新表两处 `_tableFiles` 白名单照旧同步。编译 0 错误，待用户手动验收。详见 shared 各模块文档

- 2026-08-20 传送门职责重划（用户拍板）：`PortalPlayerState` 不再快照玩家属性，瘦身为场景上下文（`RecordTransition` 记录目标关卡/场景 + keepPlayerState 语义）；玩家血量/体力/武器弹药改由新建 `PlayerAttrStore` 承担——订阅 `IPlayerEvent`（Hp/Stamina/AmmoChanged）与 `IWeaponEvent`（Equipped/Switched）变动即存，`PlayerSystem`/`WeaponSystem` 恢复路径改读 store（恢复前先把值拷到本地防初始化/装备广播覆盖，`PlayerAttrStore.SetWeapon` 公开写口修正恢复中间值）；一局结束清理点：ProcedureLobby 进入 + PlayerDeathHandler 死亡。编译 0 错误，待用户手动验收跨场景 HP/弹药保留。详见 portal-system progress 与 player-system README

- 2026-08-20 101 场景补放传送门：portal.xlsx 新增 1101（next level→102）/1102（→103）并导表；`BattleScene_3D_L01` 经 MCP 摆放 Portal_Next_102（1101，(0,0,-5)，全灭激活+保留属性）与 Portal_ReturnLobby（1001，(-6,0,-3)），场景已保存。3D_L02/L03 仍未放传送门，待补。详见 portal-system progress

- 2026-08-20 B 方案落地（用户拍板）：废弃"大厅"概念——模拟经营场景即基地（据点）。`PortalType.RETURN_TO_LOBBY` 改名 `RETURN_BASE`（portal.xlsx 1001 行同步，prompt "Press E to return to base"）；撤离（PortalSystem RETURN_BASE 分支：仓库转入+CrossPlayLink 奖励）与死亡（PlayerDeathHandler.ReturnToBase，PlayerDeathUI "Back to Base"）均回 `ProcedureSimulation`；主菜单 Start 直进基地并删除动态 Simulation 按钮（MainMenuUI）；LobbyUI 复用为基地内选关窗口（Back 改 Close()、删 Simulation 按钮，解锁置灰逻辑不变）；SimulationMainUI prefab 的 HudBar 新增 m_btn_Deploy（x=880，克隆 m_btn_Panel）打开 LobbyUI；一局结束清理（RunInventory/PlayerAttrStore/PortalPlayerState.Clear）从 ProcedureLobby 挪入 `ProcedureSimulation.EnterAsync`；ProcedureLobby 注销并删文件（GameApp 注册数组+热更重进 switch 同步，LobbyScene.unity 保留磁盘仅标记废弃）。编译 0 错误，待用户验收闭环：主菜单 Start→基地→Deploy 选关→101→撤离/死亡回基地

- 2026-08-20 基地内选关传送门 + LobbyUI 关闭修复：新增 `PortalType.SELECT_LEVEL`（portal.xlsx 2001）——交互直接开 LobbyUI 选关窗口不切场景（ExecuteTransition 前置分支绕过死亡判定/转场记录，OnInteractPressed 对选关门放行）；经营场景接入交互：ProcedureSimulation 挂 PortalSystem 到 SimulationRoot，SimulationInputSystem 新增 E 键发 OnInteractPressed；SimulationScene 摆放 Portal_Deploy（(5,0,5)，已保存）。修复 Deploy 打开的选关 UI "关不掉"：SimulationInputSystem 的 ESC 关闭链漏了 LobbyUI，ESC 只会叠开设置面板。编译 0 错误，待用户验收（E 开门选关、ESC/Back 关窗、HUD Deploy 按钮保留可用）

- 2026-08-20 修复传送门无法交互根因（2D 遗留）：`Portal_Placeholder.prefab` 的 CircleCollider2D 与 3D 玩家刚体互不检测，OnTriggerEnter 永不触发——prefab 改 SphereCollider(trigger,r=1.5)，PortalEntity.Awake 加运行时兜底自动补 3D 碰撞体；ExecuteTransition 的选关门分支前移到 IsPlayerDead 之前。Play Mode 验证：进触发区→交互开 LobbyUI→Back 关闭，全链路通过

- 2026-08-20 武器轮盘优化：槽位武器名挪到图标下方（原叠在图标中心，prefab 标签锚点/位置调整）；新增悬停属性面板 `m_text_WeaponStats`（名称/伤害/射速/弹匣/换弹/射程，悬停槽位变化时刷新，空槽清空）。Play Mode 验证通过。详见 weapon-system progress

- 2026-08-22 UI 渲染架构翻案（方案 D 落地）：纠正"Overlay 在 Scene 视图不可见"的错误前提——Overlay 画布恒以世界原点 1px=1m 渲染且 transform 不可控，是 Play 调试污染源。全部窗口改 Screen Space - Camera 挂 UIRoot 自带 UICamera（正交/depth=2/只渲 UI 层），相机挪到 (200,200,0) "UI 区"。改动：`UIRoot.prefab`（UICanvas renderMode=1 + worldCamera 关联 + UICamera 移位）、`UIWindow.FixFullScreenCanvas()` 统一 SSC、`UIModule.OnInit()` 兜底路径对齐（含 CanvasScaler 1920×1080）。编译 0 错误，回归测试（窗口显示/点击/层级/狙击镜 RT/伤害数字）由用户自测。详见 docs/Proposal/ui/ui-render-architecture.md 第三版

- 2026-08-22 方案 D 修复：UIRoot.prefab 的 UICamera GameObject 原本是禁用状态（Overlay 设计遗留），导致 SSC 画布拿不到相机退化成 Overlay 行为（画布仍钉在场景原点）；已通过 MCP 启用 UICamera 并确认运行时 MainMenuUI/UICanvas 均为 SSC + 相机在 (200,200,0)

- 2026-08-22 修复 SSC 改造的屏幕坐标副作用：狙击镜"看不到"的根因是 SniperScopeUI 直接把屏幕像素赋给 RectTransform.position（Overlay 下屏幕像素恰好等于世界坐标，SSC 下元素被甩到画布外）；同隐患还有 DamageNumberUI/HitFeedbackUI/ItemTooltipUI 的 ScreenPointToLocalPointInRectangle 传 null 相机（null 仅 Overlay 正确）。统一改走 UI 相机换算（CrosshairUpdater 本来就传了 worldCamera，无需改）。编译 0 错误，Play 验证映射线性正确

- 2026-08-22 命中标记统一：①修复狙击镜"瞄准即出现白色 X"——关镜时窗口只隐藏不销毁，OnUpdate 停走导致上次命中的标记冻结在 enabled 状态，下次开镜残留显示；现 OnRefresh/OnSetVisible(true) 双路径重置标记与后坐力偏移。②X 形命中标记推广到全武器：SniperScopeUI.CreateHitMarkerSprite 改 internal static 供复用；HitFeedbackUI 命中标记从 prefab 红方块换成同款 X 精灵，显示位置从目标屏幕位置改到准星中心（CrosshairUpdater.CurrentScreenPos，与镜内标记"中心=子弹落点"语义一致，准星不可用时退回目标位置）。Play 验证：开镜 enabled=False、命中标记 64×64 X 精灵 + 暴击橙色 + 准星中心落点均正确

- 2026-08-22 修复狙击镜 X 标记"瞄准即出现"的真正根因：`BattleSystem.OnEntityDamaged` 对任何伤害事件都发 `OnHitTarget` + 伤害飘字，敌人打玩家也会点亮命中标记。现按攻击者过滤（`AttackerId == PlayerEntity.GetInstanceID()` 才触发），并把 `OnHitTarget` 写死的 `false` 改为透传 `DamageInfo.IsCritical`（暴击橙色此前永远不触发）。Play 双向验证：敌人攻击玩家标记不亮，玩家命中标记亮白色 X。详见 battle-system progress

- 2026-08-23 设置面板新增 Input 按键改绑页签：①新增 `KeyBindAction` 枚举（Fire/Aim/Reload/Dodge/Interact/WeaponWheel/Bag/CrosshairStyle）与 `KeyBindingSetting`（`Module/SettingModule/KeyBindingSetting.cs`，默认键位+存档 `settings.keyBindings` 变动即存+冲突检测+恢复默认）；②`InputSystem` 全部动作键改运行时读绑定（原硬编码 Mouse0/C 一并收编），ESC 固定且捕获改绑期间不触发关 UI；③`SettingsUI` 加 General/Input 页签，Input 页运行时克隆行模板生成改绑列表，点击按键按钮→按新键生效、ESC 取消、冲突提示；④prefab 通过脚本补丁（.tmp_patch_settings_prefab.py）新增页签按钮/双面板/行模板/重置按钮，原控件移入 m_panel_General。编译与 Play 验证待 MCP 重连后进行。详见 settings-ui / input-system progress

- 2026-08-23 设置面板 Input 页签 + 准星样式/颜色设置落地并 Play 验证通过：①`KeyBindingSetting`（8 动作改绑、冲突检测、变动即存 settings.keyBindings）+ `InputSystem` 全部动作键运行时读绑定（Mouse0/C 硬编码收编，ESC 捕获期不触发关 UI）；②`CrosshairSetting`（样式 Dot/Cross/Circle/TShape + 6 预设色，OnChanged 广播）+ `BattleMainUI` 准星精灵改白色烘焙由 Image.color 染色、订阅即时刷新，C 键循环与设置按钮共用 CycleStyle 并写档；③SettingsUI prefab 双页签 + 改绑行模板 + 色板（两个 YAML 补丁脚本），Close 按钮挪右上角。验证：编译 0 错误、8 行默认键位/页签切换/改绑 T→重置回 R/准星 Circle+红色实时生效/存档字段齐全、测试后已还原默认并清理捕获状态。详见 settings-ui / input-system progress

- 2026-08-23 准星样式可视化预览 + 改绑左键同帧防抖：①准星精灵生成从 BattleMainUI 抽取为静态工厂 `CrosshairSpriteFactory.Create(style,size,thickness)`（5 样式生成器+纹理辅助全部搬迁，BattleMainUI 瘦身约 200 行）；②SettingsUI General 页签新增 `m_img_CrosshairPreview`（64x64 预览图，prefab YAML 补丁3），`UpdateCrosshairViews()` 订阅 OnChanged 统一刷新样式名+预览精灵+染色，旧精灵连贴图销毁防泄漏；③修复改绑鼠标键死循环：按下完成绑定（GetKeyDown）→同帧松开落在按钮上触发 onClick→又进捕获，`StartCapture` 加同帧防抖（`_captureEndFrame==Time.frameCount` 拦截），EndCapture/CancelCapture 记录结束帧。Play 验证：编译 0 错误、预览渲染正确、样式循环 Circle→TShape→Dot、色板换色即时同步、防抖拦截同帧捕获且正常路径不受影响。详见 settings-ui progress

- 2026-08-23 修复"切换准星后系统鼠标显示且与游戏准星位置不一致"：①`CrosshairUpdater.Update` 光标可见分支从"冻结准星"改为"同步真实鼠标位置"（光标未锁定时 Input.mousePosition 有效），关掉 UI 后准星与鼠标无缝衔接；原光标泄漏兜底（ForceHideCursor）保留并纳入 SettingsUI 豁免；②新增 `OnEnable` 对齐——各菜单 UI 约定先 SetVisible(true) 再 HideCursor，恢复瞬间光标仍可见未锁定，位置有效，覆盖准星被隐藏的背包/开箱/纸条路径；③`SettingsUI` 补上 CrosshairUpdater.SetVisible(false/true) 配对（OnCreate/OnDestroy），与背包/开箱/纸条一致，打开设置不再双光标并存。Play 验证：设置开→准星隐藏+光标可见+解锁；关→准星恢复+光标隐藏锁定+位置同步真实鼠标；编译 0 错误。详见 input-system progress

- 2026-08-23 对话系统 + 任务系统设计提案（待评审）：按业界成熟方案（RPGMaker 扁平节点表 / 塔科夫式跨局任务 / 三层分离：内容表→解释器→呈现）输出两份提案——`docs/Proposal/narrative/dialogue-system.md`（TbDialogue/TbDialogueNode 扁平节点表、DialogueSystem 解释器、Flag Blackboard 对话标志位、含统一 IInteractable 交互仲裁器落地计划）与 `docs/Proposal/narrative/quest-system.md`（四态状态机、kill/collect/extract/flag 四类目标、事件驱动进度、QuestSystem 纯 C# 常驻单例、塔科夫式 NPC 接交动线）。两系统共用条件/动作表达式词汇表（flag:/quest:/level:/give: 等）。待用户评审后再实现

- 2026-08-23 准星位置神圣不可侵犯（用户拍板，彻底解决游戏准星/系统鼠标统一性问题，取代同日的"同步"方案）：原则——准星位置是玩家瞄准状态，只有战斗中的鼠标位移能驱动它；任何 UI 操作（背包/轮盘/设置/切准星样式）不得强制移动准星，关 UI 后瞄点原样保留；系统鼠标只在点选类 UI 期间出现、用完可靠收回。改动：①`CrosshairUpdater` 撤销光标可见同步与 OnEnable 对齐，回归纯冻结（泄漏兜底保留）；②`WeaponWheelUI` 不再显示系统鼠标（移除 ShowCursor/HideCursor），选择改为打开以来鼠标位移增量驱动（死区 20px 保持原武器），并隐藏/恢复准星；③死区 -1 槽位由 WeaponSystem 原有保护兼容。Play 验证：背包/设置/轮盘三路径准星全程 (960,540) 零漂移、光标显隐锁定正确。轮盘手感（灵敏度/死区）待用户实测。详见 input-system / weapon-system progress

- 2026-08-23 设置面板"两种鼠标"收尾：①用户截图定位——红框中的"第二个鼠标"实为上一轮加的准星样式预览图（外形=准星、无衬底，被误认为游戏光标跑进面板），已给预览加深灰衬底板（`m_img_CrosshairPreviewBg`，prefab 补丁4），视觉上是色板而非光标；②`IsMenuUIOpen()` 纳入 `SettingsUI`——此前设置面板不在屏蔽名单，点面板按钮的鼠标按下会穿透到开火键；③`CrosshairUpdater` 战斗态每帧无条件断言 `Cursor.visible=false + lockState=Locked`，缓解编辑器/Windows 下关闭 UI 后 OS 鼠标残留（编辑器仍需 Game 视图聚焦才能完全生效，打包后无此问题，属 Unity 编辑器固有限制）。Play 验证：设置开→准星隐藏+IsMenuUIOpen=true；关→准星恢复+光标隐藏锁定+位置零漂移

- 2026-08-24 小地图系统落地：`MinimapSystem`（正交俯视相机 y=50/orthoSize=22/北向上，渲 512×512 RenderTexture，LateUpdate 跟随玩家 XZ 居中，剔除 UI 层）+ `MinimapUI`（220×220 右下角非全屏窗口，RawImage 绑 RT，敌人红点池按 `EnemyRegistry.All` + `TryWorldToMap` 逐帧投影，玩家静态中心标记）；`ProcedureBattle` 挂载系统并入场打开窗口；prefab 按 InteractionPromptUI 根结构手写到 `Assets/AssetRaw/UI/MinimapUI/`。MCP 断连，编译与 Play 验证待重连。详见 combat/minimap-system

- 2026-08-24 修复开狙击镜时 HUD 消失：`UIModule.OnSetWindowVisible()` 在 fullScreen 窗口就绪后隐藏其下所有窗口，SniperScopeUI 的 `fullScreen: true` 把 BattleMainUI（血条/弹药）藏了。改 SniperScopeUI 为 `fullScreen:false`，新增 `BattleMainUI.SetCrosshairImageHidden` 只切准星 Image.enabled，`WeaponSystem` 开关镜时配对调用（防普通准星钉在镜窗中心）。待 Play 验证。详见 weapon-system progress

- 2026-08-24 小地图修复：空白根因是 MinimapUI.ScriptGenerator 的 `FindChildComponent` 路径漏了 `m_rect_Panel/` 前缀（transform.Find 精确路径），地图纹理与红点池全部绑定失败；面板按用户要求从右下角挪到右上角。待 Play 验证

- 2026-08-24 小地图返工为 2D 平面缩略图方案（用户反馈：露场景外/开镜变灰/不该实时渲 3D，参考 Apex/三角洲）：MinimapSystem 改入场一次性烘焙（BattleBoundary.Bounds 定图范围，新增 Bounds 属性，Render 一次后相机停用）；MinimapUI 改 uvRect 窗口平移（45% 视野、钳制不露图外）+ 图标窗口投影 + 层级升 Tips 压过狙击镜蒙版。待 Play 验证。详见 combat/minimap-system

- 2026-08-24 小地图 + 开镜 HUD Play 验证通过（101 关）：烘焙 ready=True、uvRect 窗口随玩家居中（0.28,0.28,0.45×0.45）、右上角面板显示地形/红点/绿标；手动打开 SniperScopeUI 后 BattleMainUI 与 MinimapUI 均保持 visible，小地图不变灰。编译 0 错误。真实开镜流程（右键）的准星隐藏配对待用户实测

- 2026-08-24 小地图菜单遮挡修复 + 平面化烘焙：MinimapUI 监听 IsMenuUIOpen 菜单打开时隐藏面板（保留 Tips 层压狙击镜蒙版），设置 Close 按钮不再被挡；烘焙改 Unlit/Color 替换 shader 平色无光影（打包需加 Always Included Shaders）。Play 三项验证全过。详见 combat/minimap-system

- 2026-08-25 M 键大地图模式：`KeyBindAction.Map`（默认 M，可改绑）+ MinimapUI.SetBigMapMode——面板右上角 220² ↔ 屏幕居中 636²，视野 45% ↔ 整图；开图计入 IsMenuUIOpen 屏蔽射击瞄准（InputSystem 拆出 IsWindowMenuOpen 防循环依赖）；其它菜单压上自动退出大地图；OnDestroy 复位静态状态。Play 验证：开→居中整图+menuBlocked=True，关→pos/size 恢复右上角小图+menuBlocked=False

- 2026-08-25 修复 `BillboardRenderer` 与 Unity 内置组件同名警告（AddComponent/GetComponent 会失效）：改名 `BillboardFaceCamera`，.meta GUID 不变场景引用不受影响，代码无其它引用点。编译 0 错误警告消除

- 2026-08-25 新增 NPC 系统（经营场景底座）：Luban 新表 `TbNpc`（id/name/role/dialogueId，dialogueId 预留，2 条英文测试数据，白名单同步 `cfg_tbnpc`）；`NpcConfigMgr` / `NpcEntity`（SphereCollider trigger 1.5m + 钢蓝色占位胶囊 + 头顶 TMP 名字牌，`BillboardFaceCamera` 朝向相机）/ `NpcSystem`（E 交谈占位、提示复用 InteractionPromptUI，ProcedureSimulation 挂载）/ `INpcEvent.OnNpcTalked`（对话系统接线点）；SimulationScene 摆 NPC_Quartermaster(-3,0,3)、NPC_Doc(3,0,-4)。Play 实测：靠近出 "Press E to Talk" → E 触发交谈日志，名字牌可读，0 错误。对话系统提案仍 pending（docs/Proposal/narrative/dialogue-system.md）。详见 docs/modules/simulation/npc-system/

- 2026-08-26 对话系统 MVP 落地（按 docs/Proposal/narrative/dialogue-system.md 的 P1~P4，P0 仲裁器未做）：Luban 新表 `TbDialogue`（对话头：startNode/onceOnly/priority）+ `TbDialogueNode`（扁平节点：line/choice/setflag/end + speaker/text/choices/condition/action/next），白名单同步 `cfg_tbdialogue`/`cfg_tbdialoguenode`；`DialogueConfigMgr`（条件开场选择）+ `NarrativeCondition`/`NarrativeAction`（flag/level/give:gold 生效，quest:* 占位）+ `DialogueFlagSystem`（`SaveData.dialogue.flags` 变动即存，旧档自动取默认值）+ `DialogueSystem`（状态机 + E 两拍推进 + 数字键选项 + 出区打断 + 同帧防连跳）+ `DialogueUI`/prefab（底部 280px 对话框打字机 + 中部选项按钮，不暂停不动光标）；NpcSystem/SimulationInputSystem/ProcedureSimulation 完成接线（对话中 E 不广播交互、Esc 链最先关对话）。测试数据：对话 900（Quartermaster 含 setflag+2 选项分支）、901（Doc）。Play 实测全链路通过：开对话→推进→选项→结束关窗→提示恢复，flag 已写档，0 错误。详见 docs/modules/narrative/dialogue-system/

- 2026-08-26 对话编辑器：TbDialogue/TbDialogueNode 从 xlsx 改为 CSV 数据源（编辑器纯 C# 直读直写，无 openpyxl 依赖；引号字段/逗号文本无损；Excel 仍可打开）；新增 `Assets/Editor/Dialogue/DialogueEditorWindow.cs`（菜单 Tools/Dialogue/Dialogue Editor）：左栏对话列表 + 节点列表 + 节点详情，增删改、引用校验（next/选项/起始节点存在性、ID 重复亮黄）、"保存并导表"一键跑 gen bat + Refresh；原 dialogue.xlsx/dialoguenode.xlsx 已删除。无头实测：加载 2 对话 8 节点、保存无损、导表链路成功（cfg_tbdialoguenode.json 重新生成）。同日可视化优化：节点列表改为**节点图视图**（卡片 + 贝塞尔连线，next 蓝线/选项黄线带选项文本，从起始节点 BFS 自动分列布局，未连通节点排最右；卡片左侧类型色条、选中蓝框/起始绿框）、对话列表 onceOnly ① 标记、删除按钮红色化；修域重载后数据清空导致的潜在 NRE（OnEnable 自动重新加载）

- 2026-08-26 修复 `UnityException: Tag: UIRoot is not defined`：main.unity 的 UIRoot 实例带 UIRoot 标签但 `ProjectSettings/TagManager.asset` 未注册，补上该标签后报错消除（Play 验证通过）。对话编辑器节点图新增**纵向（从上到下）布局**切换：图标题栏 "→ 横向 / ↓ 纵向" 开关（EditorPrefs 记忆），纵向模式深度=行、同行分支横排、连线从卡片底缘出/顶缘入；布局算法与两种模式的连线/坐标均无头验证通过。同日界面再优化：窗口改为 Rect 布局 + **可拖拽分隔条**（竖条调左栏宽度、横条调节点图/详情高度，悬停显示调整光标、拖拽时高亮，尺寸 EditorPrefs 记忆），三面板随区域自适应填充

- 2026-08-27 NPC 最小档移动落地（用户拍板最小档：巡逻 + 对话站住转身，不做寻路/FSM/存档记忆位置）：`TbNpc` 加 `moveSpeed`/`patrolPath`（`__beans__.xlsx` bean 同步，npc.xlsx 表头修正为 7 列后导表成功）；`NpcEntity` 新增 `InitPatrol`（解析 `x,z|x,z`，不足 2 点或 speed<=0 站桩）+ `Patrol`（ping-pong 往返、到点停留 1s、移动前转身）+ 对话分支（`DialogueSystem.IsPlaying && CurrentNpcId==本NPC` 时站住并 `FaceTowards(玩家)`，仅 Y 轴 Slerp 8/s）。坑位：玩家生成晚于场景加载，`Start` 里 `FindGameObjectWithTag("Player")` 拿到 null 导致转身静默失效，改 Update 懒获取。Play 实测：巡逻位移 1.2m/s 正常、Doc 站桩、对话中位置冻结 + dot=1.00 完全面向玩家，编译 0 错误。详见 docs/modules/simulation/npc-system/

- 2026-08-27 主角/敌人 3D 化占位 + 武器模型切枪联动：①`Player.prefab`/`Enemy.prefab` 的 Visual 子节点由平躺 SpriteRenderer 改为 3D 胶囊（CreatePrimitive 去 Collider，玩家青 0.6x1m / 敌人红 0.55x0.9m，底面贴地），新材质资产 `Assets/AssetArt/Materials/M_Player_Placeholder.mat`、`M_Enemy_Placeholder.mat`；敌人 `SetFacing` 的 flipX 对胶囊自动空操作，血条结构不变；②新增 `WeaponMountView`（玩家右侧 WeaponMount 挂点，按武器类型生成不同尺寸/颜色长方体，订阅装备/切换事件只显示当前槽位，Start 读 WeaponSystem 现状补模型；坑位：ownerId 必须用 `IWeaponOwner.OwnerId`，组件自身 InstanceID 会被事件过滤）。Play 验证：编译 0 错误，基地与 101 关胶囊显示正常、三槽切换显隐正确、截图确认武器随角色朝向挂右侧

- 2026-08-27 玩家胶囊尺寸对齐 NPC（0.6x1.8m）：修复经营场景玩家巨大的根因——`ProcedureSimulation` 里有 2D 占位圆点时代的 `visual.localScale = Vector3.one * 5f` 兜底（注释自述"换正式角色模型后移除"），胶囊化后把 1.8m 玩家放大到 10m 高；该放大块整体删除，prefab 的 Visual 调整为 NPC 同款尺寸。Play 截图确认玩家与 NPC 胶囊等大

- 2026-08-30 统一经营/战斗相机参数：`SimulationCameraController` 弃用硬编码（旧值偏移 (0,7,-5)、FOV 沿用场景相机残留、平滑阻尼跟随、滚轮只改高度导致俯仰角随缩放漂移），改为与战斗 `CameraSystem3D` 同读 Luban `TbCamera3D`——俯仰 60°、偏移 (0,5,-3.5)、FOV 45、缩放范围 5~30、移动/缩放速度全部同源；跟随统一为硬跟随（像素级锁定，弃用平滑阻尼）；滚轮缩放改为按基准偏移等比缩放（高度/距离同比，俯仰角恒定）；Awake 显式钉住旋转（旧版不设旋转，沿用场景相机残留角度）。Play 验证：编译 0 错误，经营场景 rot=(60,0,0)/fov=45/offset=(0,5,-3.5) 与战斗一致，缩放到高度 10 后 offset=(0,10,-7) 角度不变。后续调相机只改 `camera3d.xlsx` 一处两场景同时生效

- 2026-08-30 火箭筒定型直线爆炸物 + 4 槽武器轮盘 + 相机底部锚定：①`WeaponSystem.MAX_WEAPON_SLOTS` 3→4，101 关默认武器 `1001,1003,1004,1005`（Pistol/Rifle/Sniper/Rocket），WeaponWheelUI 扇区按槽数泛化、prefab 补第 4 槽；②火箭去索敌/去右键瞄准，`AimAssistSystem` 只留磁吸，`IHitFeedbackEvent.OnTargetLocked` 移除；③激光常开且起点改从 `WeaponMountView.GetMuzzleWorldPos()` 枪口射出；④`ProjectileSystem` 修复遍历中销毁弹体导致 `Collection was modified` 每帧抛异常卡死游戏（快照遍历），火箭寿命终点引爆；⑤`CameraSystem3D` 新增 `_screenAnchorY=0.25`（人物锚画面底部 1/4，ViewportPointToRay 绝对反解）。Play 实测：轮盘 4 槽、枪口激光、开火链路（`LastFireTime` 证实击发，此前"开火无效"系 clipSize=1 打空即自动换弹 + MCP 探测慢的观测假象）。爆炸击杀效果待用户实测。爆炸 shader 方案见 docs/Proposal/combat/explosion-shader-proposal.md（已评审待实施）

- 2026-08-30 爆炸特效 shader 化（RPG 爆炸正式视觉，替换橙色球占位）：新增 `ExplosionFireball.shader`（噪声位移半球 + FBM 侵蚀 + 黑体色带）与 `ExplosionShockwave.shader`（地面圆环亮环 + 加色淡出），`ExplosionEffectDriver` 0.5s 自驱动自毁，`ProjectileSystem` 删除占位 Tick 列表。重要修正：项目实际是 **Built-in 渲染管线**（非方案假设的 URP），shader 按 Built-in ShaderLab 编写；冲击波外扩改 1.5 倍半径避免被不透明火球遮挡。编辑模式静态帧截图验证：p=0.15 火球亮黄+圆环领先、p=0.45 转橙侵蚀出孔洞，时间轴符合提案 §3.3。待用户 Play 实测（火箭弹命中/寿命终点两处引爆均走新特效）。打包提醒：两个 shader 需加 Always Included Shaders。另注意：发现 main 场景运行时存在两个 UIRoot GameObject（id 56288/-19166），疑似 UIModule 兜底创建与 prefab 实例并存，待排查

- 2026-08-30 排查"开火后移动速度变低"：结论为非 bug，是 `weapon.xlsx` 的移速系数设计——`MoveSpeed = BaseMoveSpeed(5) × MoveSpeedMultiplier ×（开火中 ×FireMoveSpeedMultiplier，仅全自动按住扳机时）`。实测：步枪 4.75→开火 3.80→松手恢复 4.75；火箭筒常驻 3.00（0.6 系数，与开火无关）；狙击常驻 3.50（0.7）。`WeaponInstance.IsFiring` 属性无人读写（冗余可清理）。各武器系数：Pistol 1/0.9、SMG 1/0.85、Rifle 0.95/0.8、Sniper 0.7/0.5、Rocket 0.6/0.4。若手感不佳改 `Configs/GameConfig/Datas/weapon.xlsx` 这两列即可

- 2026-08-30 总览表状态刷新：战利品容器系统 🚧→✅（开箱链路早已实测通过，遗留 IInteractable 仲裁器/美术/摆放）；新增小地图系统条目（✅）；武器系统/弹道/飞行物/相机/辅助瞄准备注同步火箭筒直射化、4 槽轮盘、爆炸特效 shader 化、相机锚点与经营战斗参数统一；设置 UI/设置系统备注同步按键改绑与准星自定义已完成；经营 UI 备注同步三窗口 Prefab 化已完成；敌人/玩家备注补 3D 胶囊化；NPC 补最小档移动；特效系统备注注明爆炸 Driver 可作未来 EffectSystem 参考实现。未启动系统不变：奖励/音频/特效管理/任务/工人/农场

- 2026-08-30 撤离点系统全链路落地并 Play 实测通过：`TbLevel` 新增 `extractionTime` 列（可配，101=10s）；`ExtractionPointEntity`（撤离圈触发器+占位圆盘）+`ExtractionSystem`（圈内玩家倒计时、敌人同圈暂停、归零且存活则撤离）；撤离结算抽取为 `PortalSystem.ExtractToBase` 公共方法（传送门 RETURN_BASE 分支复用）；`BattleMainUI` 顶部倒计时文本；`ProcedureBattle` 挂载系统；101 场景摆撤离点 (18,0,18)。专项验证：圈内假敌人→IsPaused=True 且剩余时间冻结，敌人清除后恢复并正常撤离至经营场景。遗留：占位圆盘待美术、正式点位规则、撤离圈与 Portal/Container 触发区重叠仲裁。详见 docs/modules/combat/extraction-system/progress.md

- 2026-08-30 修复 ConfigSystem 启动时序 bug（疑似此前偶发"进游戏配置全空/卡死"根因）：`ConfigSystem.Tables` getter 在 `_init=false` 时同步懒加载 `Load()`，启动早期（资源系统未就绪）被抢跑后同步加载全空，却把 `_init=true` 永久堵死 `LoadAsync`（入口 `if (_init) return`）。修复：LoadAsync 全空时延迟重试最多 3 次；Load() 以 TbLevel 为哨兵（全空不标 _init）；单表失败降级 Warning 并打真实异常；Tables getter 加抢跑堆栈 Warning 便于定位。`Configs/GameConfig/CustomTemplate/ConfigSystem.cs` 模板已同步。另排查发现：Unity 编辑器 Error Pause 开启时，一条错误日志即暂停整个 Play（frameCount 冻结），已用反射关闭；MCP 点击按钮须用 `b.onClick.Invoke()`，ExecuteEvents 会抛异常触发 Error Pause

- 2026-08-30 清理废弃场景：删除 `LobbyScene.unity`（大厅概念废弃后仅存磁盘标记）与 `BattleScene.unity`（2D 原型兜底场景，所有关卡均已配置 sceneName 不会命中），同步移除 EditorBuildSettings 两条记录与 `BattleSceneSetup` 中的 CreateLobbyScene/CreateBattleScene 重建代码；`ProcedureBattle` 兜底场景名改为 `BattleScene_3D_L01`。保留 `BattleScene_L01/L02/L03`——`level.xlsx` 的关卡 1/2/3 仍引用它们，若确认废弃 2D 老关卡，需连配置行一起删

- 2026-08-30 删除全部 2D 场景与关联配置：`BattleScene_L01/L02/L03` 三个场景文件删除；`level.xlsx` 删除关卡 1/2/3 行（剩 101/102/103，Deploy 菜单随之不再显示 Stage 1-3）；`unlock.xlsx` 删除指向 2D 关卡的解锁行 1/2；`portal.xlsx` 删除 1002/1003/1004（指向 2D 关卡/场景）；`BattleSceneSetup` 移除 CreateBattleSceneL01；EditorBuildSettings 同步清理；Luban 已重新导表（level/portal/unlock 三表），编译 0 错误
- 2026-08-30 设置界面新增"返回主界面"按钮：SettingsUI prefab 克隆 Close 按钮生成 `m_btn_ReturnMainMenu`（文本 "Main Menu"），点击切 `ProcedureMainMenu`（复用 ChangeProcedure 的 CloseAll+暂停复位）；已在主菜单时仅关窗口（新增 `GameApp.IsCurrentProcedure<T>()`）。YooAsset SimulateBuild 已刷新

- 2026-08-30 任务系统 MVP 落地（提案 docs/Proposal/narrative/quest-system.md 转正）：①P0 `IEnemyEvent.OnEnemyDied` 补 configId 参数（EnemyDeadState 发布处 + DropSystem/PortalSystem/ProjectileSystem 订阅处签名同步）；②新表 `TbQuest`/`TbQuestObjective`（quest.csv/questobjective.csv，CSV 数据源同 dialogue 惯例，_tableFiles 白名单两处同步）；③`QuestSystem` 纯 C# 静态类（四态状态机，订阅全局 GameEvent 击杀/仓库变化 + DialogueFlagSystem.OnFlagSet + CrossPlayLink.OnBattleExtracted 钩子；collect 按仓库持有数重算；奖励走 CurrencySystem/PlayerProfileSystem/InventorySystem 窄口）；④SaveData 加 quest 段（进度按 objectiveId 存）；⑤叙事词汇 quest:id:active/ready/done/accept 与 quest:accept/turnin:id 从 pending 桩转正；⑥QuestLogUI（prefab 化，左列表右详情，J 键开关+ESC 关窗链）；⑦示例任务链 1001 杀 5 假人 → 1002 带 3 木料 → 1003 撤离 101，Quartermaster/Doc 对话接交节点已插链。Play 实测全链路通过（含 collect 持有即完成、对话起始节点选择回归）。坑位：新建 UI prefab 根必须挂 Canvas+GraphicRaycaster，AssetRaw 变更后需 SimulateBuild

- 2026-08-30 NPC 头顶任务标记（MMO 惯例三态）：`QuestConfigMgr` 新增 `GetQuestsByGiver(npcId)` 反向索引（giverNpc>0 才入索引）；`NpcEntity` 头顶 y=2.7 挂 TextMeshPro 文本标记（fontSize 3.5、localScale 0.7、BillboardFaceCamera 面向相机，与名字牌同范式）。`RefreshQuestMarker()` 优先级：任意 ReadyToTurnIn → 黄"?"（1,0.85,0.2）> 任意 CanAccept → 黄"!" > 任意 Active → 灰"!"（0.6 灰）> 隐藏；Start 订阅 GameEventMgr 的 IQuestEvent 四事件实时刷新，OnDestroy 清订阅。Play 代码断言验证全过：初始 npc1 黄"!"/npc2 隐藏 → accept(1001) 后灰"!" → 5 次击杀后黄"?" → 交付后 npc1 回落黄"!"（1002 可接）+npc2 黄"!"（1003 可接）。视觉大小/可读性待用户俯视角实测，可能需微调 fontSize/高度

- 2026-08-30 任务接取确认窗 + 任务追踪 HUD：①`quest:accept:id` 叙事动作改为弹 `QuestAcceptConfirmUI`（新 prefab，居中面板：任务名+描述+Confirm/Cancel，Enter 确认 Esc 取消），玩家确认才走 `QuestSystem.Accept`；弹窗期间 `DialogueSystem.Update` 加守卫不响应推进键，对话结束时弹窗自动按取消关闭（订阅 OnDialogueEnded）；不可接取保持跳过+告警。②新增 `QuestTrackerUI`（新 prefab，屏幕左侧中部，UILayer.UI）：常驻列出进行中任务名称+目标 x/y 进度，Ready 任务名转绿加 "(Ready)" 后缀，纯展示（根 CanvasGroup blocksRaycasts=false、文本 raycastTarget 全关），ProcedureSimulation/ProcedureBattle 均常驻打开，订阅 IQuestEvent 四事件全量重建。Play 验证全过：弹窗 Confirm→接取成功+追踪器出现条目；3 杀→3/5 实时刷新；5/5→(Ready)；交付→追踪器清空；Cancel→任务保持 Inactive 不上追踪器。坑位：execute_code 里 GameModule 要写 GameLogic.GameModule（非 TEngine 命名空间）；C# 字符串里的换行经 python heredoc/JSON 多层转义易炸，用 (char)10 替代

- 2026-08-30 NPC 对话改为任务枢纽式（常见 RPG 选项交互）：NPC 有可交付/可接取任务时，对话不再自动从任务节点链开始，改为先播默认问候语并出选项菜单——"Turn in: X"（可交付优先）/"Accept: Y"/"Just chatting."（走默认闲聊后续节点）。实现：`DialogueSystem.CollectQuestChoices` 扫描 TbDialogueNode 中 condition 为 `quest:id:ready/accept` 的节点，按任务实时状态动态生成选项（无需改表），`ShowQuestHub` 用无条件起始节点作问候语+选项出口；任务选项跳到原任务节点（offer 台词+接取确认窗 / 交付台词+发奖励）。Play 实测：可接时选项 "1. Accept: Pest Control / 2. Just chatting."，闲聊走 9006 正常链；Accept 项弹确认窗 Confirm 后才接取；Ready 时变 "Turn in" 选项，选中即交付发奖励。选项上限 4（3 任务+1 闲聊，DialogueUI.MaxChoices 限制）

- 2026-08-31 文档梳理（清理 3D 化/Lobby 废弃后的过时内容）：①加废弃横幅——`俯视角2D射击游戏功能设计.md`（2D 时代设计稿）、`项目架构方案.md`（早期架构规划）；②`射击模块实现文档.md` 加"部分过时"横幅并重写 §1 流程图（MainMenu→Simulation→Battle，BattleRoot 系统清单补全至现状，CameraSystem3D）；③`开发计划方案.md` 加状态冻结横幅（指向 TODO.md/progress.md）；④`战斗模块使用手册.md` 全面修订——武器/关卡改 weapon.xlsx/level.xlsx 工作流（含导表步骤）、关卡表更新为 101~103、预制体改 3D 胶囊、输入改 Settings 可改绑、UI 路径修正（SniperScope/WeaponWheel 已迁入 AssetRaw/UI）、场景章节删 LobbyScene/BattleScene 改 3D 场景与基地、火箭筒改直射无锁定；⑤`framework/04-SceneAndProcedure.md` 更新流程表（ProcedureSimulation）、场景目录、传送门类型（return_base/select_level）；⑥`standards/scene-creation-standard.md` 光照/相机/流程图 3D 化；⑦`notice/UI-and-Prefab-Pitfalls.md` 正交相机坑改为透视说明；⑧`portal-system-design.md` 加修正横幅（return_base/PlayerAttrStore/ExtractionSystem）；⑨`docs/modules/README.md` 索引补 minimap/extraction/narrative 三个模块；⑩`infra/procedure-system`、`shared/inventory-system` README 去掉 Lobby 引用；⑪Proposal 状态标注同步（simulation-mvp 已落地）。ADR 保持决策时点原貌未改；progress.md 的日期变更记录作为历史档案未动

- 2026-08-31 重写 `docs/项目架构方案.md` 为 3D 时代现状架构（取代 2D 规划稿，旧内容随 git 历史保留）：项目定位（搜打撤+经营闭环）/技术栈/程序集/GameLogic 真实目录树/三流程与 BattleRoot·SimulationRoot 挂载清单/事件总线 19 接口/Luban 表清单/UI 架构（方案 D SSC+UICamera）/存档变动即存/搜打撤数据流（RunInventory↔Warehouse、ExtractToBase、CrossPlayLink）/叙事三层/占位美术与 shader 化特效策略/技术债（双 UIRoot、反射切流程、交互仲裁器缺失等）/演进方向

- 2026-08-31 多语言完整方案立项（提案 `docs/Proposal/infra/localization.md`，待评审）：盘点 TEngine 内置 I2（LocalizationManager/Localize 组件/I2Languages.asset/I2_ 前缀分表热更，游戏侧零使用、innerLocalizationCsv 为空）后决策不走 I2 数据源，采用 Luban 词条表路线——新增 `localization.xlsx`（key + en/zh_cn/zh_tw/ja/ko 列）+ 运行时 `LocalizationSystem`（取词/语言切换事件/PlayerPrefs 持久化，复用 ProcedureLaunch 存档逻辑）+ `LocTextBinder` 组件（等价 I2 Localize）；Luban 内容表（quest/dialoguenode/item/note/npc）文本列改存词条 key，显示层统一 GetText 兜底链（当前语言→en→key 原文+告警）。分三期：P1 框架+语言下拉、P2 存量 UI/内容 key 化迁移、P3 中文回填与字体验证

- 2026-08-31 接入思源黑体作为 TMP 全局默认字体：用户提供的 `SourceHanSans-Regular.ttc` 移入 `Assets/Fonts/`，经 MCP 创建动态图集 TMP 字体资产 `SourceHanSans-Regular SDF.asset` 并设为 `TMP_Settings.defaultFontAsset`（项目所有 UI 文本经 `TMPFontProvider.DefaultFont` 读取该设置，自动全局生效）；已验证字体同时含中文（U+4E2D）与英文字形。多语言方案 P3 的中文字体风险项随之解除

- 2026-08-31 多语言 P1 落地（提案 docs/Proposal/infra/localization.md）：①新增 Luban 词条表 `localization.csv`（key/en/zh_cn/zh_tw/ja/ko 列，首批 21 词条覆盖 Settings 面板与通用按钮）+ `__beans__.xlsx` 加 `cfg.Localization` bean + `__tables__.xlsx` 加 `cfg.TbLocalization`（string key map 模式），ConfigSystem._tableFiles 白名单与 CustomTemplate 同步；②`LocalizationSystem`（GameLogic/System/）：词条索引、Current 语言（复用 TEngine.Language 枚举与 Constant.Setting.Language 存档）、GetText 兜底链（当前语言→en→key+告警）、GetText 参数化 {0}、OnLanguageChanged 事件，GameApp.EntranceAsync 在 ConfigSystem.LoadAsync 后初始化；③`Loc` 静态门面（UI/Loc.cs）：Get/Bind——Bind 注册全局绑定列表，语言切换全量刷新并清理假空（热更类型无法序列化 prefab，放弃 LocTextBinder 组件改代码绑定）；④SettingsUI：静态文本全部 Loc.Bind key 化（标题/页签/灵敏度/开镜/准星样式颜色/Close/MainMenu/ResetBindings），灵敏度数值文本走 Get(key, arg)；General 页签新增语言切换行（运行时克隆颜色标签+准星样式按钮，y=-430/-474，与 CrosshairStyle 同循环切换范式），SupportedLanguages 当前只列 English/ChineseSimplified（其余列填内容后再开放）；⑤思源黑体已是 TMP 默认字体，中文渲染正常。Play 实测：切中文后标题"设置"/页签"通用输入"/"准星灵敏度"/"关闭"等全部即时刷新，切回英文正常，存档持久化。坑位：TEngine UIWindow 生命周期是 ScriptGenerator→RegisterEvent→OnCreate，运行时创建的控件监听必须在 OnCreate 里注册。P2 待做：其余 UI 静态文本与 quest/dialogue/item/note/npc 内容列 key 化

- 2026-08-31 Settings 界面重构为列表型布局 + 修复 TMP 字体资产报错：①prefab 重排——m_panel_General 改居中固定 1000x760，六行"左标签右控件"（语言/准星灵敏度/开镜灵敏度/狙击瞄准模式/准星样式/准星颜色），标签 x=-230 宽 360 左对齐，控件列 x≈150，灵敏度数值改纯数字显示在滑条右侧；m_img_Background 改 (0.04,0.04,0.06,0.96) 压暗遮挡主菜单；语言行控件（m_text_LanguageLabel/m_btn_Language）改为 prefab 序列化对象（克隆现有控件生成），不再运行时克隆，监听回归 RegisterEvent 注册；②狙击开镜开关右侧文本改为动态状态（Toggle/Hold，走 aim_mode.toggle/hold 词条）；③清理废弃词条（sensitivity_value×2、sniper_aim_toggle）；④修复 `MissingReferenceException: m_AtlasTextures of TMP_FontAsset`——首版字体资产在 execute_code 里用 CreateAsset 创建时图集纹理/材质未作为子资产持久化，Play 退出时被销毁留空引用；重建时将 atlasTexture 与 material `AddObjectToAsset` 持久化后解决。Play 实测：中英切换即时生效，思源黑体中文渲染正常（截图 Assets/Screenshots/settings_layout_en/zh.png），控制台无游戏侧报错（残留 generators.ai.unity.com 报错为 Unity AI Assistant 包网络噪音，与项目无关）

- 2026-08-31 多存档位 + 存档选择界面 + 主菜单本地化：①`SaveSystem` 改造为 3 槽位（save_N.json，旧 save.json 自动迁移为槽位 1），`CurrentSlot` 存 PlayerPrefs；`SwitchSlot` = 落盘当前档→加载新槽→统一调 6 个缓存模块的 `InvalidateCache`（Currency/PlayerProfile/Unlock/Warehouse/Quest/SensitivitySetting，新增约定见 save-system README）→新槽立即落盘→`OnSlotChanged` 事件；`GetSlotSummary` 只读摘要（exists/level/gold/diamond）；②新增 `SaveSlotSelectUI`（prefab 在 AssetRaw/UI/SaveSlotSelectUI/，3 个 300x640 竖槽位：Player prefab 渲染到共享 RenderTexture 的人物预览 + 左上角黄色等级 + 人物下方金币/钻石 + 底部槽位名，当前槽位绿底+▶，空槽显示 Empty/New Game 不显示模型，整槽可点）；③主菜单新增"存档"按钮（m_btn_Saves）与"当前存档：N"指示（订阅 OnSlotChanged/OnLanguageChanged），全部按钮文本 key 化（ui.mainmenu.* 词条）；④词条表新增 11 条。Play 实测：切槽位 3 空档→文件创建+主菜单指示即时刷新+缓存正确重置（gold 500/Lv1），切回槽位 1 数据恢复（gold 1850/Lv3）；中文显示全链路正常。截图 Assets/Screenshots/mainmenu_zh.png、saveslot_zh2.png、saveslot_final2.png

- 2026-08-31 修复"存档界面点槽位后无法进入游戏"：根因是交互设计缺陷——SaveSlotSelectUI 为不透明全屏窗口且背景吃射线，点槽位只选中不关闭，用户被困在界面里（开始游戏按钮被盖住）。修复（后按用户要求调整）：点槽位=选中并直接进入游戏（`OnSlotSelected` → SwitchSlot + CloseUI + ChangeProcedure<ProcedureSimulation>，符合"点存档即开局"直觉）。另排查出两个测试侧假象：①Play 中改代码不会热重载（LockAssemblyReloadInPlayMode 锁定），MCP 验证必须重启 Play；②流程切换中抢跑（场景异步加载未完成时 ChangeProcedure）会让 UIModule.OnInit 崩溃留下脏状态（未注册的窗口残骸 GameObject 关不掉），属编辑器测试竞态，正式流程未复现。Play 回归通过：选槽位→自动关→开始游戏→进 SimulationScene（ts=1，gold/等级正确）

## 2026-09-01 修复：存档数据进游戏后 HUD 显示对不上
- 现象：选 3 级存档（金币 1850）进游戏，Simulation 顶部 HUD 仍显示 `Gold: 0 Lv: 1`。
- 根因：`SimulationMainUI` 的金币/等级文本只在 `OnGoldChanged`/`OnExpChanged` 事件里更新；加载旧存档后这两个值本就不变、事件不触发，文本停留在 Prefab 默认值。
- 修复：`SimulationMainUI` 新增 `RefreshHud()`（直接读 `CurrencySystem.Gold` 与 `PlayerProfileSystem.Level/Exp/ExpToNextLevel`），在 `OnCreate` 中调用，并订阅 `SaveSystem.OnSlotChanged` 兜底（`OnDestroy` 中退订）。
- 待验证：MCP 复现"主菜单→存档槽位 1→进 Simulation"，HUD 应显示 Lv 3 / Gold 1850。

## 2026-09-01 复杂地形敌人寻路判定改造（3D 化后寻路真正落地）
- 背景：3D 化后全项目没有任何物体在 Obstacle 层，导航网格认为全图可走；排查中另发现一个存量阻塞 bug——`AStarNavigationSystem` 的 `_gCost` 复用数组不清零、未访问格默认 0，导致 `tentativeG < _gCost[n]` 永假、A* 从未真正扩展过任何节点（此前敌人追击实际一直走"直线冲玩家"fallback，即顶墙死锁的根因）。修复：新增 `_gGeneration` 代数标记，本代未写入 g 值的格子视为无穷大。
- 障碍层落地：L01 场景 `Test_Cube`/`Test_Sphere`/`Test_Capsule` 改 Obstacle（10）层（MCP additive 开场景改完 SaveScene 再关）。`Note_1`/`LootContainer_1/2` 不改——其实体 Awake 把 BoxCollider 强制为 2m trigger 交互区，改层会让导航 CheckSphere 与子弹 SphereCast（mask 含 Obstacle）误撞触发区。运行时无动态生成的障碍物。
- `ColliderGridBuilder`：可行走判定球半径从 cellSize*0.25 改为代理半径 `AgentRadius=0.3f`（TbEnemy 无半径字段，取敌人胶囊半径常量，已注释）；CheckSphere 加 `QueryTriggerInteraction.Ignore`；网格边界改为扫描范围与障碍 bounds 求并集（障碍入层后原"只包障碍"逻辑会裁掉远离障碍的可行走区）。
- `AStarNavigationSystem`：LOS 平滑从零宽 Linecast 改 `Physics.SphereCast`（半径=AgentRadius*0.9，Ignore trigger）；新增 `TryGetNearestWalkable`（`INavigationSystem`/`NavigationSystem` 同步暴露）供出生点吸附。
- `EnemyChaseState`：①stuck 检测——0.8s 窗口位移 <0.1m 判定卡住立即强制重寻路，连续 3 次待机 0.5s 再重试；②寻路失败 fallback 收敛——无 LOS 原地等待+退避重试（0.5s 起递增封顶 2s），有 LOS 保留直追；③`ApplySeparation` 分离方向预判 0.3m 不可走则丢弃分离分量。
- `EnemySpawnSystem`：生成点圆环带随机后校验 `IsWalkable`，不可走重掷最多 8 次，再失败吸附最近可走格；GM 刷怪命令同样吸附；刚体同步处理保持。
- Play 实测（101 关）：网格 216x216 含 36 不可走格；方块边缘外 0.4m 内不可走（膨胀生效）；敌人从球/胶囊北侧绕障三面合围玩家；stuck 日志"[EnemyChase] 疑似卡住，强制重寻路"实际触发；Console 0 error，旧 warning（双 EventSystem / DontDestroyOnLoad / I2Localization 导出 CSV）均未出现。截图 `Assets/Screenshots/nav_obstacle_avoidance_mid.png` / `nav_obstacle_avoidance_final.png`。
- 坑位：关卡 ID 是 101~103 不是 1/2/3；`LevelConfigMgr.EnsureLoaded` 有 `_loaded` 闩锁，配置表加载完成前首次 Get 会永久缓存空表（GM reload 可解，本次踩到）。

## 2026-09-01 第二轮 warning 清理
- `PlayerEntity.SetDead`：重复死亡时刚体已是 Kinematic，写 `linearVelocity` 触发 "Setting linear velocity of a kinematic body is not supported" 警告（每局 3 次）；加 `!_rb.isKinematic` 守卫。MCP 实测两轮"死亡→Restart→再死亡"后 Console 0 error、该警告不再出现。
- 孤儿 meta（nav_obstacle_avoidance_t1/t2.png.meta）为测试中临时文件、已被清理，告警为存量日志，自然消失。
- 本轮验证后 Console 仅余 2 条有意保留的项目日志级 warning：开战 banner（Start Battle Game Logic）与 EditorSimulateMode 提示。

## 2026-09-01 敌人半径配置化 + 导航网格动态障碍局部更新
- `TbEnemy` 新增 `radius` 字段（碰撞/寻路膨胀半径，米；9001/9002 填 0.3）：`enemy.xlsx` 加列 + `__beans__.xlsx` 的 `cfg.Enemy` bean 加字段后跑 `gen_code_bin_to_project.bat` 导表；导表用模板覆盖 `GameProto/ConfigSystem.cs`，本次导后 `git diff` 确认 `_tableFiles` 清单未丢（模板与工程文件一致）。改 JSON 后已 `SimulateBuild("DefaultPackage")`。
- `ColliderGridBuilder.AgentRadius` 由硬编码 0.3f 常量改为运行时属性：取 `ConfigSystem.Instance.Tables.TbEnemy` 全表最大 `radius`，表不可用/为空兜底 0.3f 并 Log.Warning 一次；LOS SphereCast 半径（`AStarNavigationSystem.LosRadius`）同步改属性取值 ×0.9；烘焙完成日志输出 `agentRadius` 实际生效值。
- 动态障碍局部更新：`INavigationGridBuilder`/`INavigationSystem`/`AStarNavigationSystem`/`NavigationSystem` 链路新增 `UpdateRegion(Bounds)`（复用 CheckSphere 重判、越界 clamp、无网格静默忽略），门面更新后清空路径缓存；新增 `NavObstacle` 组件（`GameLogic/Navigation/NavObstacle.cs`），挂运行时动态障碍上即可（静态障碍烘焙已包含，不用挂）。两个坑位已在组件内处理：①项目 `Physics.autoSyncTransforms=false`，生成/移动后立刻读 bounds 是旧值 → 组件内 `Physics.SyncTransforms()`；②OnDisable 时自身 Collider 还在物理场景里，立即重扫会误判阻挡 → 延迟一帧经 NavigationSystem 协程执行。
- Play 实测（101 关，重启 Play 后验证）：烘焙日志 `agentRadius=0.3m（TbEnemy 最大 radius）`；运行时反射把 9001 radius 改 0.6 再 Rebuild，日志变 0.6，改回恢复（免二次导表验证 max 生效）；运行时创建 Obstacle 层立方体（挂 NavObstacle）→ 不可走格 36→48（2×2 障碍时 36→68）、路径缓存清零、目标格不可走、敌人绕东侧进入攻击状态（路径点距障碍中心 ≥0.89m）；销毁后一帧不可走格恢复 36、格子恢复可走。Console 0 error（generators.ai.unity.com 报错为 Unity AI 包网络噪音，与项目无关）。截图 `Assets/Screenshots/nav_dynamic_obstacle_chase.png` / `nav_dynamic_obstacle_removed.png`。

## 2026-09-02 101 关添加复杂地形测试布局
- `BattleScene_3D_L01.unity` 新增 `TestObstacles` 根节点，12 个 Obstacle 层障碍（均按 1m 网格对齐、2m 高）：北墙排 z=8（x -8..4，留 2m 门洞 x 0..2）、东墙 x=8（z 2..10，留 2m 门洞 z 5..7）、5 根 1m 散柱、西北角 U 形死角（开口朝南）、(12,6) 4x4 大块障碍（撤离点仍可东侧绕行）、(-2,12) 1m 窄缝墙对（验证膨胀后不可通行）。
- Play 实测：网格不可走格 36→486；关键点位校验全对（出生点/门洞/撤离点可走，墙体/死角内壁/窄缝不可走）；敌人隔墙触发追猎后穿门洞到达玩家；玩家躲入 U 形死角后多敌从南口涌入合围。Console 0 error 0 意外 warning。截图 `Assets/Screenshots/nav_complex_terrain_L01.png`。
- 注意：敌人 chaseRange=5m，超距不追（实测两次"敌人不动"均为未入仇恨范围，非卡死）；传送测试需把敌人放到玩家 5m 内。

## 2026-09-02 敌人 AI：检测/追踪双距离分离 + 散步状态
- `TbEnemy` 新增 `pursuitRange` 字段（追踪距离，米；9001/9002 填 10），`chaseRange` 注释语义改为检测距离；流程同 radius：`enemy.xlsx` 加列 + `__beans__.xlsx` 的 `cfg.Enemy` bean 加字段 + `gen_code_bin_to_project.bat` 导表，导后 `git diff` 确认 `ConfigSystem.cs` 的 `_tableFiles` 未被模板覆盖。
- 滞回设计落地：`EnemyStateContext` 新增 `PlayerOutOfPursuit`（d > pursuitRange，丢失判定），`WantsToChase` 保持检测语义（d ≤ chaseRange，触发判定）；`EnemyStateMachineDriver.UpdateContext` 注入两距离（`EnemyEntity.PursuitRange` 新属性，`EnemySpawnSystem`/GM 刷怪两处 Initialize 调用点同步传参）。5~10m 滞回区间内保持追击，避免边界抖动。
- 新增 `EnemyWanderInterceptor`（优先级 150，介于 Attack 200 与 Chase 100 之间）：仅当前状态为 Chase 且 `PlayerOutOfPursuit` 时切 `EnemyWanderState`。新增 `EnemyWanderState`：进入时记录锚点，4m 半径内随机选点 + `NavigationSystem.TryGetNearestWalkable` 吸附，移速 = `MoveSpeed × 0.4`，到达停 1~2s 再选下一点，3s 走不到换点；攻击/重新追击仍由 Attack/ChaseInterceptor 触发。`EnemyEntity.CreateFsm` 注册新状态。
- 配套修正：`EnemyChaseState` 删除自身 `!WantsToChase→Idle` 退出（否则滞回区间会掉回待机，丢失统一切 Wander）；`EnemyAttackState` 攻击结束时玩家在追踪距离内回 Chase 而非 Idle（修复"玩家闪避拉开 5m+ 后敌人傻站原地"——攻击 1s 内玩家可出检测圈，Attack 固定回 Idle 而 Idle 只在 5m 内再检测）。
- Play 实测（101 关，god 模式，重启 Play 后）：①敌人在玩家 7m 外 Idle 不动（vel=0）；②传送进 4.5m 立即追击至近身（1.4m 进 Attack）；③交战中玩家传 7.5m（滞回区间）→ 敌人从 1.4m 持续追至 7.5m 再次近身，不再傻站；④玩家传 12m → 进入 Wander，实测 vel=0.80（=2×0.4），位置绕锚点随机漂移（(8.1,-0.1)→(8.5,1.7)→(11.1,-1.2)→(7.3,-2.5)→(4.9,-0.3)），停留阶段采样到 vel=0；⑤Wander 中玩家传回 4m → 立即重新 Chase 并近身。Console 0 error（generators.ai.unity.com 报错为 Unity AI 包网络噪音，与项目无关）。

## 2026-09-02 代码审查修复（导航/FSM/UI 系统三组，Play 统一验证通过）
- 导航组：①修复路径缓存×对象池所有权冲突（🔴 会串路径）——`NavigationSystem` 新增 `CloneResult`，缓存条目存独立副本、命中时也拷贝返回，缓存自有实例不外泄，调用方实例用完靠 GC（ChaseState 从不 Release，无 double-release）；②缓存命中续期（刷新 Timestamp 防热路径被逐出）；③`ColliderGridBuilder.AgentRadius` 改 `_cachedAgentRadius` 惰性缓存 + `InvalidateAgentRadiusCache`（由 `NavigationSystem.Rebuild` 调用），避免每帧全表扫描；④A* 同格快速路径末点改用吸附后格中心；⑤`NavObstacle` 延迟重扫协程改 UniTask（Yield + GetCancellationTokenOnDestroy），不再依赖 MonoBehaviour 协程生命周期。
- FSM 组：①`EnemyEntity` 动画映射补 `"Wander" => "Enemy_Run"`（此前 Wander 必打动画缺失警告）；②`EnemyChaseState` stuck 日志降 Log.Debug，且仅 `IsPathValid()` 时强制重寻路，路径无效走 `_pathFailInterval` 退避（修玩家 unreachable 时的日志刷屏+寻路风暴）；③`EnemyEntity.Initialize` 滞回校验：clamp `chaseRange ≥ attackRange`、`pursuitRange ≥ chaseRange`，修正时 Log.Warning 一次。
- UI/系统组：①`SaveSlotSelectUI.LoadPreviewModel` async void 改 UniTaskVoid + CancellationToken + Forget，`CleanupPreview` 补 `Destroy(_previewRt)`（修 RenderTexture 泄漏）；②`BattleMainUI` 撤离倒计时文本 0.1s 量化缓存，不变不赋值（去每帧字符串分配与 TMP 重排）；③`BuildingEntity` UpdateLabel 缓存、进度 1% 量化、选中/建造染色从 sharedMaterial 写改 MaterialPropertyBlock 写 `_Color`（修共享材质污染同型建筑），CacheOriginalColors 改读 sharedMaterial；④`BallisticSystem.UpdateRocketLaser` 缓存 `_cachedMountView`；⑤三个 ConfigMgr 统一 `Instance?.Tables?` 判空 + `_fallbackWarned` 只告警一次（加载成功后重置）；⑥`EnemySpawnSystem` 模板 SetActive(false) 提出池分支外，吸附失败兜底改返回 spawnCenter。
- Play 统一验证（101 关 + Simulation + 主菜单存档界面，god 模式）：动态障碍（NavObstacle 立方体）增删时多敌追击无串路径、Console 0 error；2 敌人进 Wander 状态持续 4s+ 无任何动画警告；玩家传送到 (100,100) unreachable 12 秒 Console 0 新增（stuck 节流生效）；撤离倒计时全链路正常（进圈 10s 倒计时→结算→切 Simulation）；存档界面 3 槽位 3D 预览正常、0 异常；建筑 SetSelected MPB 染色实测（选中变黄仅本栋、取消恢复原色，NPC/玩家不受影响）。截图 Assets/Screenshots/verify_selected_*.png / verify_deselected.png / verify_saveslot.png。
- 审查遗留未修（登记备查）：Wander/Chase 调优常量未入 TbEnemy、经营三窗口代码拼装残留、Launcher 中文文本未走词条、敌人物理查询隔帧优化（规模项）。
