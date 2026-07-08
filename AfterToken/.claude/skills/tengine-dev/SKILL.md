---
name: tengine-dev
description: TEngine Unity 游戏框架开发指导。触发词：TEngine, UIWindow, UIWidget, GameEvent, AddUIEvent, LoadAssetAsync, SetSprite, HybridCLR, YooAsset, Luban, GameModule, 热更, 资源加载, UI开发, 事件系统, 配置表
---

# TEngine 开发指导

TEngine 是基于 HybridCLR + YooAsset + UniTask + Luban 的 Unity 游戏框架。
本 skill 提供 AI 专用的精炼参考文档，确保生成的代码与框架 API 完全一致。

## 核心红线

1. **异步优先**：IO 操作用 `UniTask`，禁止同步加载/Coroutine
2. **模块访问**：通过 `GameModule.XXX` 访问，而非 `ModuleSystem.GetModule<T>()`
3. **资源必须释放**：`LoadAssetAsync` 对应 `UnloadAsset`，GameObject 用 `LoadGameObjectAsync`
4. **热更边界**：`GameScripts/Main` 不热更，`GameScripts/HotFix/` 全部热更
5. **事件解耦**：模块间用 `GameEvent`，UI 内部用 `AddUIEvent`
6. **卡住即上报**：如果同一个问题尝试 7 次仍无法解决，跳过该问题并记录到 `docs/DailyRecord` 或对应 issue，继续后续任务，交由人类处理
7. **UI Prefab 统一放 AssetRaw**：所有热更域 UI Prefab 必须放在 `Assets/AssetRaw/UI/` 下，通过 `WindowAttribute` 的 `location` 走 YooAsset 加载，禁止放在 `Assets/Resources/` 下
8. **模块文档与 TodoList 同步**：每次开始一个模块的设计/搭建前，必须在 `docs/modules/<module-name>/` 下新建目录，并写入 `README.md`（功能与代码介绍）和 `progress.md`（进度总结）；同时同步更新 `docs/TODO.md` 项目整体待办清单
9. **文档改动不主动提交**：AI 只负责生成/修改 `docs/` 下的文档（如模块 README、progress.md、TODO.md、DailyRecord 等），不执行 `git add` / `git commit` / `git push`，所有文档改动由人类审阅后统一提交
10. **游戏内容操作边界**：涉及实际游戏运行状态的操作（如 Play Mode 验证、场景切换、模拟输入、触发战斗/死亡/闪避/换弹等实时行为）受 MCP 工具限制，AI 不应强行执行。若任务需要此类验证，应明确告知用户由其在编辑器或真机中完成，并提供验证清单与预期结果
11. **规范优先，禁止绕过**：解决问题时必须先查阅本 skill 的 references 文档与项目规范，严格按照规范执行，不得贪图省事使用 `FromResources = true`、`Resources.Load`、临时目录等绕过方式。若规范与实际代码冲突，按 [CLAUDE.md](../../../CLAUDE.md) 第 2 步处理：用 Grep/Read 验证实际 API，优先信任代码实现，并在输出中标注冲突点。
12. **AssetDatabase 刷新防卡策略**：`refresh_unity` 工具在编译域重载或资源导入量大时容易超时。替代方案：
    - 优先使用 `execute_code` 调用 `AssetDatabase.Refresh()` 并指定具体作用域；
    - 若涉及 YooAsset 模拟模式，通过 `YooAsset.Editor.EditorSimulateModeHelper.SimulateBuild("DefaultPackage")` 触发清单重建；
    - 避免连续多次调用完整刷新；刷新后通过 `read_console` 观察日志，而不是无限等待 ready 状态。

## 模块开发与项目 TodoList 规范

### 目录结构

```
docs/
├── TODO.md                              # 项目整体待办清单
├── DailyRecord/                         # 日报（按日期记录具体改动）
└── modules/
    ├── <module-name>/
    │   ├── README.md                    # 模块职责、核心类、接口、依赖
    │   └── progress.md                  # 模块累计进度（已完成/进行中/待办/阻塞）
    └── ...
```

### 流程

1. **新建模块目录**：开始设计/搭建模块前，在 `docs/modules/<module-name>/` 创建目录。
   - 目录名使用小写英文或数字，单词间用 `-` 连接，例如 `shooting-system`、`ui-prefab-migration`。
2. **编写 README.md**：说明模块职责、涉及的主要类/文件、对外接口、依赖关系、设计要点。
3. **编写 progress.md**：列出模块关键任务，标注状态（`已完成` / `进行中` / `待办` / `阻塞`），并在每次重大进展后更新。
4. **同步整体 TodoList**：在 `docs/TODO.md` 中新增/更新该模块条目，保持全项目视角。
5. **与日报配合**：`docs/DailyRecord/` 记录当天具体改动；`docs/modules/<module>/progress.md` 记录模块累计进度。
6. **不主动提交文档**：完成上述文档更新后，仅保存文件，不执行任何 Git 提交操作，等待人类处理。

## UI Prefab 路径速查

| UI | Prefab 路径 | 备注 |
|---|---|---|
| `MainMenuUI` | `Assets/AssetRaw/UI/MainMenuUI/MainMenuUI.prefab` | 全屏 |
| `LobbyUI` | `Assets/AssetRaw/UI/LobbyUI/LobbyUI.prefab` | 全屏 |
| `LoadingUI` | `Assets/AssetRaw/UI/LoadingUI/LoadingUI.prefab` | `UILayer.System` |
| `BattleMainUI` | `Assets/AssetRaw/UI/BattleMainUI/BattleMainUI.prefab` | HUD |
| `DamageNumberUI` | `Assets/AssetRaw/UI/DamageNumberUI/DamageNumberUI.prefab` | 伤害飘字 |
| `HitFeedbackUI` | `Assets/AssetRaw/UI/HitFeedbackUI/HitFeedbackUI.prefab` | `UILayer.System` |
| `WeaponWheelUI` | `Assets/AssetRaw/UI/WeaponWheelUI/WeaponWheelUI.prefab` | 武器轮盘 |
| `SniperScopeUI` | `Assets/AssetRaw/UI/SniperScopeUI/SniperScopeUI.prefab` | 狙击镜 |
| `TransitionUI` | `Assets/AssetRaw/UI/TransitionUI/TransitionUI.prefab` | 全屏转场 |
| `InteractionPromptUI` | `Assets/AssetRaw/UI/InteractionPromptUI/InteractionPromptUI.prefab` | 交互提示 |

## 文档路由

根据任务类型，读取对应的 reference 文档：

| 任务类型 | 必读文档 | 进阶文档 | 优先级 |
|---------|---------|---------|--------|
| UI 开发 | [ui-lifecycle.md](references/ui-lifecycle.md) | [ui-patterns.md](references/ui-patterns.md) | P0 |
| 事件系统 | [event-system.md](references/event-system.md) | [event-antipatterns.md](references/event-antipatterns.md) | P0 |
| 资源加载 | [resource-api.md](references/resource-api.md) | [resource-patterns.md](references/resource-patterns.md) | P0 |
| 模块使用 | [modules.md](references/modules.md) | — | P0 |
| 热更代码 | [hotfix-workflow.md](references/hotfix-workflow.md) | — | P1 |
| 代码规范 | [CODING_STANDARDS.md](../../../docs/standards/CODING_STANDARDS.md) | [naming-rules.md](references/naming-rules.md) | P0 |
| Luban 配置 | [luban-config.md](references/luban-config.md) | — | P1 |
| 项目结构 | [architecture.md](references/architecture.md) | — | P2 |
| 问题排查 | [troubleshooting.md](references/troubleshooting.md) | — | P2 |
| MCP 场景/GO/UI/脚本/Editor | [mcp-tools.md](references/mcp-tools.md) | — | P1 |
| MCP 材质/Shader/动画/VFX | [mcp-visual.md](references/mcp-visual.md) | — | P2 |
