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
| 设置 UI | ⏳ | P2 | 共享层部分完成 | `docs/modules/ui/settings-ui/` | 音量、画质、操作设置（新增模块） |
| 经营 UI | ⏳ | P2 | M4 经营系统 | `docs/modules/ui/simulation-ui/` | SimulationMainUI、建筑/资源/订单 Widget |

### 战斗系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 输入系统 | ✅ | - | - | `docs/modules/combat/input-system/` | 移动、瞄准、开火、换弹、切枪、闪避、武器轮盘 |
| 玩家系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/player-system/` | `PlayerEntity` + FSM + 体力系统 + HP/体力条 HUD；`TbPlayer` 已接入；Play Mode 基础切换已初步确认，待后续新状态调试 |
| 武器系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/weapon-system/` | 武器槽、开火、换弹、辅助瞄准，待 `TbWeapon/TbBullet` |
| 弹道系统 | ✅ | - | - | `docs/modules/combat/ballistic-system/` | Raycast / Projectile 分发、Debug 射线 |
| 飞行物系统 | 🟡 | P1 | - | `docs/modules/combat/projectile-system/` | 基础已完成，待逻辑/视觉分离以支持弹幕（见 `docs/Proposal/combat/bullet-logic-visual-separation.md`） |
| 辅助瞄准系统 | ✅ | - | - | 并入武器系统文档 | 辅助瞄准 + 火箭锁定 |
| 相机系统 | 🟡 | P1 | - | `docs/modules/combat/camera-system/` | 跟随、边界、狙击镜，待抖动 |
| 敌人系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/enemy-system/` | `EnemyEntity`、生成、FSM（Idle/Chase/Attack/Dead）已跑通；自研 A* 寻路系统框架已完成并接入 `EnemyChaseState`；待 Play Mode 验证与 `TbEnemy` 配置接入 |
| 掉落与拾取系统 | 🟡 | P1 | 敌人系统、共享层 | `docs/modules/combat/pickup-system/` | 敌人死亡掉落、`PickupEntity`、拾取入包已完成；待 Play Mode 验证 |
| 战斗系统 | 🟡 | P0 | 事件系统完善 | `docs/modules/combat/battle-system/` | 伤害、死亡，待暴击/Buff/结果事件 |
| 关卡系统 | 🟡 | P0 | 配置表系统、事件系统 | `docs/modules/combat/level-system/` | 硬编码表，待波次/胜负/配置化 |
| 奖励系统 | ⏳ | P1 | 共享层 | `docs/modules/combat/reward-system/` | 战斗奖励分发 |

### 场景系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 传送门系统 | 🟡 | P1 | 关卡系统、事件系统、输入系统 | `docs/modules/scene/portal-system/` | 基础版完成：配置表、核心逻辑、UI、转场、场景摆放、编译通过；已新增 L01/L02/L03 三个测试战斗场景并配置传送门，门上显示目的地；待 Play Mode 验证 |

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
| 背包系统 | 🟡 | P1 | - | `docs/modules/shared/inventory-system/` | 临时背包（槽位制+容量配置+B 键面板）与仓库（内存态）已完成；UI prefab 待生成，待 Play Mode 验证 |
| 道具系统 | 🟡 | P1 | - | `docs/modules/shared/item-system/` | `cfg.Item` 扩展 + 4 档稀有度 + 稀有度框 prefab 已完成；使用效果后续接入 |
| 解锁系统 | ⏳ | P2 | 玩家档案系统 | `docs/modules/shared/unlock-system/` | 内容解锁条件与校验 |
| 跨玩法联动 | ⏳ | P2 | 共享系统、经营系统 | `docs/modules/shared/cross-play-link/` | 战斗奖励 → 经营资源 → 战斗强化 |

### 模拟经营系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 经营总控 | ⏳ | P2 | 共享系统 | `docs/modules/simulation/simulation-system/` | `ProcedureSimulation`、场景加载、协调子系统 |
| 经营时间 | ⏳ | P2 | - | `docs/modules/simulation/sim-time-system/` | 时间推进、加速/暂停 |
| 建筑系统 | ⏳ | P2 | 配置表系统、货币/背包 | `docs/modules/simulation/building-system/` | 建造、升级、拆除 |
| 生产系统 | ⏳ | P2 | 建筑系统、经营时间 | `docs/modules/simulation/production-system/` | 生产队列、产出结算 |
| 工人系统 | ⏳ | P2 | 建筑系统 | `docs/modules/simulation/worker-system/` | 工人分配、属性成长 |
| 农场系统 | ⏳ | P2 | 经营时间 | `docs/modules/simulation/farm-system/` | 种植、生长、收获 |
| 订单系统 | ⏳ | P2 | 背包、货币 | `docs/modules/simulation/order-system/` | 订单生成、交付、奖励 |

### 管线与工具

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 资源管线 | ✅ | - | - | `docs/modules/pipeline/asset-pipeline/` | YooAsset 收集器、SimulateBuild |
| 热更管线 | 🟡 | P1 | - | `docs/modules/pipeline/hotfix-pipeline/` | HybridCLR 环境、DLL 加载，待 AOT 元数据补充验证 |
| **Luban 配置表系统** | 🟡 | **P0** | - | `docs/modules/pipeline/config-system/`（总览）<br>`docs/modules/pipeline/luban-config-system/`（详细） | 配置工程已搭建，输出格式已切 JSON，`weapon`/`level`/`player`/`battle`（Enemy/Wave/Drop）已定义并接入，新增流程文档已补充；待 `buff` / `item` 修复 / YooAsset 收集器配置 |
| 编辑器工具 | ✅ | - | - | `docs/modules/pipeline/editor-tools/` | BattleSceneSetup、Force Recompile、TMP Migration |

### 全局与支撑系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 存档系统 | ⏳ | P1 | 共享系统 | `docs/modules/shared/save-system/`（新增） | 本地 JSON/PlayerPrefs 存档（新增模块） |
| 设置系统 | ⏳ | P2 | 存档系统 | `docs/modules/shared/settings-system/`（新增） | 音量、画质、操作设置持久化（新增模块） |
| 性能优化 | ⏳ | P3 | - | `docs/modules/pipeline/performance-optimization/`（新增） | Draw Call、GC、对象池、Atlas、分帧（新增模块） |
| GM / 调试工具 | ⏳ | P3 | - | `docs/modules/pipeline/gm-tools/`（新增） | 无敌、跳关、刷怪、显示碰撞盒（新增模块） |

---

## 三、关键阻塞链

```
Luban 配置表数据补充
    ├── 阻塞 → 玩家系统（TbPlayer）、武器系统（TbWeapon）、敌人系统（TbEnemy）、关卡系统（TbWave）
    └── 间接阻塞 → 战斗闭环验证

事件系统完善（ILevelEvent / IBattleResultEvent / 共享事件）
    ├── 阻塞 → 战斗系统结果事件
    ├── 阻塞 → 关卡系统波次/胜负
    └── 阻塞 → 奖励系统、跨玩法联动

敌人 AI / FSM 基础框架已完成
    ├── 仍待接入 `TbEnemy` 配置表
    └── 仍待更优生成逻辑与波次/掉落联动

共享系统（Currency / Inventory / PlayerProfile）
    ├── 阻塞 → 奖励系统
    ├── 阻塞 → 跨玩法联动
    └── 阻塞 → 经营系统消耗/产出
```

---

## 四、本周聚焦（M1 第一阶段）

1. **补充 Luban 战斗表数据**：`TbPlayer`/`TbEnemy`/`TbWave`/`TbDrop`/`TbBuff`；修复 `item.xlsx` 中 `ItemExchange`/`EQuality` 定义并注册 `cfg.TbItem`。
2. **替换硬编码配置**：`WeaponConfigMgr` → `TbWeapon`，`LevelConfigMgr` → `TbLevel`。
3. **补齐事件接口**：`ILevelEvent`、`IBattleResultEvent`、共享层事件接口。
4. **实现敌人 AI / FSM**：Idle / Chase / Attack / Dead，接入 `TbEnemy`。
5. **实现关卡波次与胜负判定**：接入 `TbWave`，触发 `IBattleResultEvent`。
6. **Play Mode 验证**：`MainMenu → Lobby → Battle` 跑通，无明显报错。

---

## 五、全局待验证

| 事项 | 状态 | 说明 | 计划完成里程碑 |
|------|------|------|----------------|
| Play Mode 全流程验证 | ⏳ | MainMenu → Lobby → Battle，需在线验证 | M1 |
| 配置表热更验证 | ⏳ | 修改配置后重新导表，YooAsset SimulateBuild 正常 | M1 |
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
| 2026-07-21 | 修复背包系统 bug：`BattleBagUI` / `WarehouseUI` 的 `ScriptGenerator` 绑定路径补 `m_img_Background/` 前缀；新增 `ItemTooltipUI` 悬浮提示窗，由 `ItemSlotHoverHandler` 转发鼠标悬停事件，展示道具名称/稀有度/类型/价格/描述；修复 `BattleBagUI` 打开时未暂停时间且未隐藏准星的问题（新增 `TimeScaleWhenVisible` 与 `CrosshairUpdater.SetVisible`）；给 `BattleBagUI` 添加 `m_btn_Close` 关闭按钮并支持再次按 B 键关闭（`InputSystem.HandleBagInput` 提到 `TimeScale` 判断之前）；背包默认显示全部容量格子（含空槽位）；ESC 键统一关闭当前最上层 UI（`InputSystem.HandleEscapeInput` + `WarehouseUI.OnUpdate`）|
