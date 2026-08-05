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
| 设置 UI | 🟡 | P2 | 存档系统 | `docs/modules/ui/settings-ui/` | 灵敏度滑块已可用；音量、画质、操作设置持久化待补充 |
| 经营 UI | 🟡 | P1 | M4 经营系统 | `docs/modules/ui/simulation-ui/` | SimulationMainUI、建筑/资源/订单 Widget；渲染架构已统一（Overlay，2026-08-01 实测通过）；待办：正式 Prefab 化、UI 特效（序列帧）、CanvasScaler 横屏修正 |

### 战斗系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 输入系统 | ✅ | - | - | `docs/modules/combat/input-system/` | 移动、瞄准、开火、换弹、切枪、闪避、武器轮盘 |
| 玩家系统 | ✅ | - | - | `docs/modules/combat/player-system/` | `PlayerEntity` + FSM + 体力系统 + HP/体力条 HUD；`TbPlayer` 已接入并应用属性 |
| 武器系统 | ✅ | - | - | `docs/modules/combat/weapon-system/` | 武器槽、开火、换弹、辅助瞄准；`TbWeapon` 已通过 `WeaponConfigMgr` 接入 |
| 弹道系统 | ✅ | - | - | `docs/modules/combat/ballistic-system/` | Raycast / Projectile 分发、Debug 射线 |
| 飞行物系统 | 🟡 | P1 | - | `docs/modules/combat/projectile-system/` | 基础已完成，待逻辑/视觉分离以支持弹幕（见 `docs/Proposal/combat/bullet-logic-visual-separation.md`） |
| 辅助瞄准系统 | ✅ | - | - | 并入武器系统文档 | 辅助瞄准 + 火箭锁定 |
| 相机系统 | 🟡 | P1 | - | `docs/modules/combat/camera-system/` | 跟随、边界、狙击镜，待抖动 |
| 敌人系统 | 🟡 | P1 | 关卡/战斗系统 | `docs/modules/combat/enemy-system/` | `EnemyEntity`、生成、`TbEnemy` 已接入；FSM + 自研 A* 寻路已跑通；待 Play Mode 验证绕过障碍物、攻击伤害判定接入 |
| 掉落与拾取系统 | ✅ | - | - | `docs/modules/combat/pickup-system/` | 敌人死亡掉落、`PickupEntity`、拾取入临时背包已完成 |
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
| 流程系统 | ✅ | - | - | `docs/modules/infra/procedure-system/` | `GameplayProcedureBase` + 主菜单/大厅/战斗 |
| 音频系统 | ⏳ | P1 | - | `docs/modules/infra/audio-system/` | BGM / SFX / 音量管理 |
| 特效系统 | ⏳ | P1 | - | `docs/modules/infra/effect-system/` | 特效生成、播放、回收 |

### 共享系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 玩家档案系统 | ⏳ | P1 | - | `docs/modules/shared/player-profile-system/`（新增） | 等级、经验、解锁（从共享数据层拆分） |
| 货币系统 | ⏳ | P1 | - | `docs/modules/shared/currency-system/`（新增） | 金币、钻石、体力（从共享数据层拆分） |
| 背包系统 | ✅ | - | - | `docs/modules/shared/inventory-system/` | 临时背包（槽位制+容量配置+B 键面板）与仓库（内存态）已完成；仓库持久化待 `save-system` |
| 道具系统 | ✅ | - | - | `docs/modules/shared/item-system/` | `cfg.Item` 扩展 + 4 档稀有度 + 稀有度框 prefab 已完成；使用效果后续接入 |
| 解锁系统 | ⏳ | P2 | 玩家档案系统 | `docs/modules/shared/unlock-system/` | 内容解锁条件与校验 |
| 跨玩法联动 | ⏳ | P2 | 共享系统、经营系统 | `docs/modules/shared/cross-play-link/` | 战斗奖励 → 经营资源 → 战斗强化 |
| 存档系统 | ⏳ | P1 | - | `docs/modules/shared/save-system/`（新增） | 本地 JSON/PlayerPrefs 存档（新增模块） |
| 设置系统 | 🟡 | P2 | 存档系统 | `docs/modules/shared/settings-system/`（新增） | 灵敏度已可用；音量、画质、操作设置持久化待 `save-system` |

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
| 设置系统 | ⏳ | P2 | 存档系统 | `docs/modules/shared/settings-system/`（新增） | 音量、画质、操作设置持久化（新增模块） |
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
    └── 待实现 → 存档系统、货币系统、玩家档案系统
```

---

## 四、本周聚焦（M1 第一阶段）

1. **接入 `TbWave` 波次生成**：替换 `EnemySpawnSystem` 硬编码参数，按关卡配置驱动多波次敌人刷新。
2. **补齐事件接口**：`ILevelEvent`、`IBattleResultEvent`、共享层事件接口。
3. **实现关卡胜负判定**：全灭敌人/生存目标触发 `IBattleResultEvent`，并打开结算/传送门。
4. **实现奖励系统**：战斗胜利奖励分发，临时背包转入仓库。
5. **实现共享层持久化**：`SaveSystem` → `CurrencySystem` / `PlayerProfileSystem` / `SettingsSystem` 持久化。
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
