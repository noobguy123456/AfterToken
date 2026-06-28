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
| 设置 UI | ⏳ | P2 | 共享层部分完成 | `docs/modules/ui/settings-ui/` | 音量、画质、操作设置（新增模块） |
| 经营 UI | ⏳ | P2 | M4 经营系统 | `docs/modules/ui/simulation-ui/` | SimulationMainUI、建筑/资源/订单 Widget |

### 战斗系统

| 模块 | 状态 | 优先级 | 阻塞/依赖 | 对应目录 | 备注 |
|------|------|--------|-----------|----------|------|
| 输入系统 | ✅ | - | - | `docs/modules/combat/input-system/` | 移动、瞄准、开火、换弹、切枪、闪避、武器轮盘 |
| 玩家系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/player-system/` | `PlayerEntity` + FSM，待接入 `TbPlayer` |
| 武器系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/weapon-system/` | 武器槽、开火、换弹、辅助瞄准，待 `TbWeapon/TbBullet` |
| 弹道系统 | ✅ | - | - | `docs/modules/combat/ballistic-system/` | Raycast / Projectile 分发、Debug 射线 |
| 飞行物系统 | ✅ | - | - | `docs/modules/combat/projectile-system/` | 生成、飞行、命中、回收 |
| 辅助瞄准系统 | ✅ | - | - | 并入武器系统文档 | 辅助瞄准 + 火箭锁定 |
| 相机系统 | 🟡 | P1 | - | `docs/modules/combat/camera-system/` | 跟随、边界、狙击镜，待抖动 |
| 敌人系统 | 🟡 | P0 | 配置表系统 | `docs/modules/combat/enemy-system/` | `EnemyEntity`、生成，待 AI/FSM |
| 掉落与拾取系统 | ⏳ | P1 | 敌人系统、共享层 | `docs/modules/combat/pickup-system/`（新增） | 敌人死亡掉落、PickupEntity、拾取（从敌人系统拆分） |
| 战斗系统 | 🟡 | P0 | 事件系统完善 | `docs/modules/combat/battle-system/` | 伤害、死亡，待暴击/Buff/结果事件 |
| 关卡系统 | 🟡 | P0 | 配置表系统、事件系统 | `docs/modules/combat/level-system/` | 硬编码表，待波次/胜负/配置化 |
| 奖励系统 | ⏳ | P1 | 共享层 | `docs/modules/combat/reward-system/` | 战斗奖励分发 |

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
| 背包系统 | ⏳ | P1 | - | `docs/modules/shared/inventory-system/`（新增） | 物品管理（从共享数据层拆分） |
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
| **Luban 配置表系统** | 🟡 | **P0** | - | `docs/modules/pipeline/config-system/`（总览）<br>`docs/modules/pipeline/luban-config-system/`（详细） | 配置工程已搭建，输出格式已切 JSON，`weapon`/`level` 已定义，生成脚本待验证 |
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
Luban 配置表系统
    ├── 阻塞 → 玩家系统、武器系统、敌人系统、关卡系统
    └── 间接阻塞 → 战斗闭环验证

事件系统完善（ILevelEvent / IBattleResultEvent / 共享事件）
    ├── 阻塞 → 战斗系统结果事件
    ├── 阻塞 → 关卡系统波次/胜负
    └── 阻塞 → 奖励系统、跨玩法联动

共享系统（Currency / Inventory / PlayerProfile）
    ├── 阻塞 → 奖励系统
    ├── 阻塞 → 跨玩法联动
    └── 阻塞 → 经营系统消耗/产出
```

---

## 四、本周聚焦（M1 第一阶段）

1. **验证 Luban JSON 生成**：运行 `Configs/GameConfig/gen_code_bin_to_project.bat`，确认 `GameProto/GameConfig/` 代码和 `AssetRaw/Configs/json/` 数据正常生成。
2. **运行 Luban 生成并补全战斗表**：`weapon`/`level` 已可生成；修复 `item` 表 Bean/Enum 定义；补充 `TbPlayer`/`TbEnemy`/`TbWave`/`TbDrop`/`TbBuff`。
3. **替换硬编码配置**：`WeaponConfigMgr` → `TbWeapon`，`LevelConfigMgr` → `TbLevel`。
4. **补齐事件接口**：`ILevelEvent`、`IBattleResultEvent`、共享层事件接口。
5. **Play Mode 验证**：`MainMenu → Lobby → Battle` 跑通，无明显报错。

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
