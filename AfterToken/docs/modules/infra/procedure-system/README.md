# 流程系统

## 职责

管理游戏主流程状态机，包括 Launcher 后的主包流程和热更域的游戏流程。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `GameplayProcedureBase` | `Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs` | 热更域流程基类 |
| `ProcedureMainMenu` | `Assets/GameScripts/HotFix/GameLogic/Procedure/ProcedureMainMenu.cs` | 主菜单流程 |
| `ProcedureLobby` | `Assets/GameScripts/HotFix/GameLogic/Procedure/ProcedureLobby.cs` | 大厅流程 |
| `ProcedureBattle` | `Assets/GameScripts/HotFix/GameLogic/Procedure/ProcedureBattle.cs` | 战斗流程 |
| `GameApp` | `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | 热更入口，切换流程 |

## 设计要点

- `GameplayProcedureBase` 统一负责 CancellationToken、LoadingUI 场景加载、OnLeave 清理。
- `GameApp.ChangeProcedure<T>()` 用于反射切换流程，并在切前关闭所有 UI。
