# LoadingUI 与场景过渡

## 职责

在场景切换时显示加载界面，并同步更新加载进度，避免玩家看到黑屏或场景未准备好的状态。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `LoadingUI` | `Assets/GameScripts/HotFix/GameLogic/UI/LoadingUI/LoadingUI.cs` | 加载 UI 窗口 |
| `GameplayProcedureBase` | `Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs` | 统一流程基类，负责场景过渡 |

## 关键流程

1. 打开 `LoadingUI`（`UILayer.System`）。
2. 异步加载目标场景，更新 `SetProgress(float)`。
3. 加载完成后关闭 `LoadingUI`，进入目标流程。

## 设计要点

- 所有游戏流程（主菜单 → 大厅 → 战斗）均通过 `LoadSceneWithLoadingAsync` 切换。
- `GameplayProcedureBase` 管理 `CancellationTokenSource`，支持取消场景加载。
