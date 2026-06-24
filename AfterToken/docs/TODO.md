# AfterToken 项目整体 TodoList

> 本文件依据 [`项目架构方案.md`](./项目架构方案.md) 整理，并按系统分类。
> 每个模块的详细任务见 `docs/modules/<category>/<module-name>/progress.md`。

## 图例

- ✅ 已完成
- 🟡 进行中 / 基础版完成
- ⏳ 待办
- 🚧 阻塞

---

## UI 系统

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| UI Prefab 工作流 | ✅ | `docs/modules/ui/ui-prefab-workflow/` | 全部热更域 UI 已 Prefab 化并放 `AssetRaw/UI/` |
| LoadingUI 与场景过渡 | ✅ | `docs/modules/ui/loading-system/` | `GameplayProcedureBase` 统一加载 |
| 命中反馈 | ✅ | `docs/modules/ui/hit-feedback-system/` | 伤害飘字、受击指示、命中标记 |
| 经营 UI | ⏳ | `docs/modules/ui/simulation-ui/` | SimulationMainUI、建筑/资源/订单 Widget |

---

## 战斗系统

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| 输入系统 | ✅ | `docs/modules/combat/input-system/` | 移动、瞄准、开火、换弹、切枪 |
| 玩家系统 | 🟡 | `docs/modules/combat/player-system/` | `PlayerEntity` + FSM，待接入 Luban |
| 武器系统 | 🟡 | `docs/modules/combat/weapon-system/` | 武器槽、开火、换弹、辅助瞄准，待配置表 |
| 弹道系统 | ✅ | `docs/modules/combat/ballistic-system/` | Raycast / Projectile 分发、Debug 射线 |
| 飞行物系统 | ✅ | `docs/modules/combat/projectile-system/` | 生成、飞行、命中、回收 |
| 相机系统 | 🟡 | `docs/modules/combat/camera-system/` | 跟随、边界，待抖动 |
| 敌人系统 | 🟡 | `docs/modules/combat/enemy-system/` | `EnemyEntity`、生成，待 AI/FSM/掉落 |
| 战斗系统 | 🟡 | `docs/modules/combat/battle-system/` | 伤害、死亡，待 Buff/暴击/结果事件 |
| 关卡系统 | 🟡 | `docs/modules/combat/level-system/` | 硬编码表，待波次/胜负/Luban |
| 奖励系统 | ⏳ | `docs/modules/combat/reward-system/` | 战斗奖励分发 |

---

## 基础设施

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| 事件系统 | 🟡 | `docs/modules/infra/event-system/` | 战斗事件已定义，经营/共享事件待补充 |
| 对象池 | 🟡 | `docs/modules/infra/pool-system/` | 通用池已有，按类型分池待完善 |
| 流程系统 | ✅ | `docs/modules/infra/procedure-system/` | `GameplayProcedureBase` + 主菜单/大厅/战斗 |
| 音频系统 | ⏳ | `docs/modules/infra/audio-system/` | BGM / SFX / 音量 |
| 特效系统 | ⏳ | `docs/modules/infra/effect-system/` | 特效生成、播放、回收 |

---

## 共享系统

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| 共享数据层 | ⏳ | `docs/modules/shared/shared-systems/` | 玩家档案、货币、背包、解锁、奖励 |
| 解锁系统 | ⏳ | `docs/modules/shared/unlock-system/` | 内容解锁 |
| 跨玩法联动 | ⏳ | `docs/modules/shared/cross-play-link/` | 战斗奖励 → 经营资源 → 战斗强化 |

---

## 模拟经营系统

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| 经营总控 | ⏳ | `docs/modules/simulation/simulation-system/` | 协调各经营子系统 |
| 建筑系统 | ⏳ | `docs/modules/simulation/building-system/` | 建造、升级、拆除 |
| 生产系统 | ⏳ | `docs/modules/simulation/production-system/` | 生产队列、产出结算 |
| 工人系统 | ⏳ | `docs/modules/simulation/worker-system/` | 工人分配、成长 |
| 农场系统 | ⏳ | `docs/modules/simulation/farm-system/` | 种植、生长、收获 |
| 订单系统 | ⏳ | `docs/modules/simulation/order-system/` | 订单生成、交付、奖励 |
| 经营时间 | ⏳ | `docs/modules/simulation/sim-time-system/` | 时间推进、加速/暂停 |

---

## 管线与工具

| 模块 | 状态 | 对应目录 | 备注 |
|---|---|---|---|
| 资源管线 | ✅ | `docs/modules/pipeline/asset-pipeline/` | YooAsset 收集器、SimulateBuild |
| 热更管线 | 🟡 | `docs/modules/pipeline/hotfix-pipeline/` | HybridCLR 环境、DLL 加载 |
| Luban 配置表 | ⏳ | `docs/modules/pipeline/luban-config-system/` | 替换硬编码配置 |
| 编辑器工具 | ✅ | `docs/modules/pipeline/editor-tools/` | BattleSceneSetup、Force Recompile、TMP Migration |

---

## 全局待验证

| 事项 | 状态 | 说明 |
|---|---|---|
| Play Mode 全流程验证 | ⏳ | MainMenu → Lobby → Battle，因 MCP 502 未在线验证 |
| 真机构建流程 | ⏳ | YooAsset 真实包、HybridCLR 出包 |

---

## 变更记录

| 日期 | 变更内容 |
|---|---|
| 2026-06-21 | 按项目架构方案与系统分类整理模块 TodoList |
