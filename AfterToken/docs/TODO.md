# AfterToken 项目整体 TodoList

> 本文件维护项目各模块的当前状态。每次新增/完成模块时，同步更新对应条目。
> 模块详细说明见 `docs/modules/<module-name>/README.md`，累计进度见 `docs/modules/<module-name>/progress.md`。

## 状态图例

- ✅ 已完成
- 🟡 进行中
- ⏳ 待办
- 🚧 阻塞

---

## 已完成

| 模块 | 说明 | 对应目录 |
|---|---|---|
| ✅ 射击系统 | 武器、弹道、飞行物、辅助瞄准 | `docs/modules/shooting-system/` |
| ✅ 命中反馈系统 | 伤害飘字、受击指示、命中标记 | `docs/modules/hit-feedback-system/` |
| ✅ 玩家战斗状态机 | Idle / Move / Reload / Dodge / Dead | `docs/modules/player-battle-fsm/` |
| ✅ UI Prefab 化 | MainMenuUI / LobbyUI / LoadingUI / BattleMainUI 等 Prefab 驱动 | `docs/modules/ui-prefab-workflow/` |
| ✅ LoadingUI 集成 | 带 LoadingUI 的场景过渡 | `docs/modules/loading-system/` |
| ✅ 流程层重构 | GameplayProcedureBase + 主菜单/大厅/战斗流程 | `docs/modules/procedure-system/` |
| ✅ 编辑器工具 | BattleSceneSetup、Force Recompile、TMP Migration | `docs/modules/editor-tools/` |
| ✅ YooAsset 资源管线 | 收集器配置、SimulateBuild、地址冲突修复 | `docs/modules/asset-pipeline/` |

---

## 进行中

| 模块 | 说明 | 对应目录 |
|---|---|---|
| 🟡 GitHub 仓库整理 | 已推送框架上级目录，待验证远程完整性 | — |
| 🟡 Console 编译验证 | TMP 迁移工具与 obsolete API 已修复，待 Unity 编译确认 | — |

---

## 待办

| 模块 | 说明 | 对应目录 |
|---|---|---|
| ⏳ Luban 配置表接入 | 替换硬编码 WeaponConfig / LevelConfig | `docs/modules/luban-config-system/` |
| ⏳ 网络模块 | 后端对接、状态同步或帧同步方案 | `docs/modules/network-system/` |
| ⏳ 音频系统 | BGM / SFX / 音量管理 | `docs/modules/audio-system/` |
| ⏳ 真机构建流程 | YooAsset 真实包构建、HybridCLR 出包 | `docs/modules/build-and-release/` |
| ⏳ 运行时全流程验证 | MainMenu → Lobby → Battle Play Mode 验证 | — |

---

## 阻塞

| 模块 | 说明 | 阻塞原因 |
|---|---|---|
| 🚧 Unity MCP 编译验证 | 无法通过 MCP 执行 `refresh_unity` / `read_console` | MCP 服务端返回 502，需手动在 Unity 中验证 |

---

## 变更记录

| 日期 | 变更内容 |
|---|---|
| 2026-06-21 | 初始化项目 TodoList，归档已完成模块 |
