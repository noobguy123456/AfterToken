# 模块文档索引

> 模块按系统分类存放。
> 每次开始新模块前，在对应分类下创建 `<module-name>/README.md` 与 `<module-name>/progress.md`，并同步更新 `docs/TODO.md`。
> 所有新增模块代码必须遵循项目规范：`../standards/CODING_STANDARDS.md`。

## 目录结构

```
docs/modules/
├── ui/                    # UI 相关
├── combat/                # 战斗相关
├── infra/                 # 基础设施
├── shared/                # 共享系统 / 跨玩法
├── simulation/            # 模拟经营
└── pipeline/              # 管线与工具
```

## 当前模块列表

### UI 系统

| 模块 | README | 进度 |
|---|---|---|
| `hit-feedback-system` | [README](./ui/hit-feedback-system/README.md) | [progress](./ui/hit-feedback-system/progress.md) |
| `loading-system` | [README](./ui/loading-system/README.md) | [progress](./ui/loading-system/progress.md) |
| `simulation-ui` | [README](./ui/simulation-ui/README.md) | [progress](./ui/simulation-ui/progress.md) |
| `ui-prefab-workflow` | [README](./ui/ui-prefab-workflow/README.md) | [progress](./ui/ui-prefab-workflow/progress.md) |

### 战斗系统

| 模块 | README | 进度 |
|---|---|---|
| `ballistic-system` | [README](./combat/ballistic-system/README.md) | [progress](./combat/ballistic-system/progress.md) |
| `battle-system` | [README](./combat/battle-system/README.md) | [progress](./combat/battle-system/progress.md) |
| `camera-system` | [README](./combat/camera-system/README.md) | [progress](./combat/camera-system/progress.md) |
| `enemy-system` | [README](./combat/enemy-system/README.md) | [progress](./combat/enemy-system/progress.md) |
| `input-system` | [README](./combat/input-system/README.md) | [progress](./combat/input-system/progress.md) |
| `level-system` | [README](./combat/level-system/README.md) | [progress](./combat/level-system/progress.md) |
| `player-system` | [README](./combat/player-system/README.md) | [progress](./combat/player-system/progress.md) |
| `projectile-system` | [README](./combat/projectile-system/README.md) | [progress](./combat/projectile-system/progress.md) |
| `reward-system` | [README](./combat/reward-system/README.md) | [progress](./combat/reward-system/progress.md) |
| `weapon-system` | [README](./combat/weapon-system/README.md) | [progress](./combat/weapon-system/progress.md) |

### 基础设施

| 模块 | README | 进度 |
|---|---|---|
| `audio-system` | [README](./infra/audio-system/README.md) | [progress](./infra/audio-system/progress.md) |
| `effect-system` | [README](./infra/effect-system/README.md) | [progress](./infra/effect-system/progress.md) |
| `event-system` | [README](./infra/event-system/README.md) | [progress](./infra/event-system/progress.md) |
| `pool-system` | [README](./infra/pool-system/README.md) | [progress](./infra/pool-system/progress.md) |
| `procedure-system` | [README](./infra/procedure-system/README.md) | [progress](./infra/procedure-system/progress.md) |

### 共享系统

| 模块 | README | 进度 |
|---|---|---|
| `cross-play-link` | [README](./shared/cross-play-link/README.md) | [progress](./shared/cross-play-link/progress.md) |
| `shared-systems` | [README](./shared/shared-systems/README.md) | [progress](./shared/shared-systems/progress.md) |
| `unlock-system` | [README](./shared/unlock-system/README.md) | [progress](./shared/unlock-system/progress.md) |

### 模拟经营系统

| 模块 | README | 进度 |
|---|---|---|
| `building-system` | [README](./simulation/building-system/README.md) | [progress](./simulation/building-system/progress.md) |
| `farm-system` | [README](./simulation/farm-system/README.md) | [progress](./simulation/farm-system/progress.md) |
| `order-system` | [README](./simulation/order-system/README.md) | [progress](./simulation/order-system/progress.md) |
| `production-system` | [README](./simulation/production-system/README.md) | [progress](./simulation/production-system/progress.md) |
| `sim-time-system` | [README](./simulation/sim-time-system/README.md) | [progress](./simulation/sim-time-system/progress.md) |
| `simulation-system` | [README](./simulation/simulation-system/README.md) | [progress](./simulation/simulation-system/progress.md) |
| `worker-system` | [README](./simulation/worker-system/README.md) | [progress](./simulation/worker-system/progress.md) |

### 管线与工具

| 模块 | README | 进度 |
|---|---|---|
| `asset-pipeline` | [README](./pipeline/asset-pipeline/README.md) | [progress](./pipeline/asset-pipeline/progress.md) |
| `editor-tools` | [README](./pipeline/editor-tools/README.md) | [progress](./pipeline/editor-tools/progress.md) |
| `hotfix-pipeline` | [README](./pipeline/hotfix-pipeline/README.md) | [progress](./pipeline/hotfix-pipeline/progress.md) |
| `config-system`（配置表系统总览） | [README](./pipeline/config-system/README.md) | [progress](./pipeline/config-system/progress.md) |
| `luban-config-system`（Luban 详细配置） | [README](./pipeline/luban-config-system/README.md) | [progress](./pipeline/luban-config-system/progress.md) |

## 文件模板

### README.md

```markdown
# <模块名>

## 职责
...

## 核心类与文件
| 类/文件 | 说明 |
|---|---|
| ... | ... |

## 对外接口
...

## 依赖关系
...

## 设计要点
...
```

### progress.md

```markdown
# <模块名> 进度

## 已完成
- [x] ...

## 进行中
- [ ] ...

## 待办
- [ ] ...

## 阻塞
- ...
```
