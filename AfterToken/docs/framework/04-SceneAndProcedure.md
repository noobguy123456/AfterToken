# 04 场景与流程

## 启动场景

- 主入口场景：`Assets/Scenes/main.unity`
- 包含 `GameEntry` 与 `UIRoot`。
- `UIRoot` 在 `GameEntry.Awake()` 中被标记为 `DontDestroyOnLoad`，跨场景保留。

## GameEntry

`Assets/GameScripts/GameEntry.cs`：

```csharp
void Awake()
{
    var uiRoot = GameObject.Find("UIRoot");
    if (uiRoot != null) DontDestroyOnLoad(uiRoot);
    DontDestroyOnLoad(this);

    ModuleSystem.GetModule<IUpdateDriver>();
    ModuleSystem.GetModule<IResourceModule>();
    ModuleSystem.GetModule<IDebuggerModule>();
    ModuleSystem.GetModule<IFsmModule>();
    Settings.ProcedureSetting.StartProcedure().Forget();
}
```

`Settings.ProcedureSetting.StartProcedure()` 启动主包流程状态机。

## 主包流程

入口流程配置：`Assets/TEngine/Settings/ProcedureSetting.asset`

```yaml
entranceProcedureTypeName: Procedure.ProcedureLaunch
availableProcedureTypeNames:
- Procedure.ProcedureLaunch
- Procedure.ProcedureLoadAssembly
- Procedure.ProcedureStartGame
# ... 资源初始化、下载相关流程
```

流程路径：

```
ProcedureLaunch
  → ProcedureSplash
  → ProcedureInitPackage
  → ProcedureInitResources
  → ProcedureCreateDownloader
  → ProcedureDownloadFile
  → ProcedureDownloadOver
  → ProcedureLoadAssembly      # 加载热更 DLL
  → ProcedurePreload
  → ProcedureStartGame         # 进入热更域
```

## 热更域流程

`Assets/GameScripts/HotFix/GameLogic/Procedure/`：

| 流程 | 目标场景 | 打开的 UI |
|------|----------|-----------|
| `ProcedureMainMenu` | `MainMenuScene` | `MainMenuUI` |
| `ProcedureSimulation` | `SimulationScene`（基地/据点） | `SimulationMainUI`、`QuestTrackerUI` |
| `ProcedureBattle` | `BattleScene_3D_L01` ~ `L03`（由 `level.xlsx` 的 `sceneName` 决定） | `BattleMainUI`、`QuestTrackerUI`、`DamageNumberUI`、`HitFeedbackUI`、`MinimapUI` |

> 早期的大厅流程 `ProcedureLobby` 已废弃：主菜单"开始游戏"直接进入基地（`ProcedureSimulation`），选关在基地内通过 Deploy 按钮/选关传送门打开 `LobbyUI` 窗口（纯 UI 窗口，不再是独立场景/流程）。

### GameplayProcedureBase

`Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs` 统一处理：

1. 管理 `CancellationTokenSource`，支持场景加载取消。
2. `LoadSceneWithLoadingAsync(sceneName, onLoaded)`：
   - 显示 `LoadingUI`
   - `GameModule.Scene.LoadSceneAsync(sceneName, progressCallBack)`
   - 关闭 `LoadingUI`
   - 执行子类回调
3. `OnLeave`：
   - `CloseAll`
   - `RemoveAllTimer`
   - `UnloadUnusedAssets`

子流程只需实现 `EnterAsync()`，声明目标场景名与加载完成后的业务逻辑。

### 跨流程数据

当前临时方案：`BattleContext.CurrentLevelId`

- `LobbyUI` 选择关卡后写入 `BattleContext.CurrentLevelId`。
- `ProcedureBattle.OnEnter` 读取并加载对应 `LevelConfig`。

> 长期应迁移到正式运行时数据层，避免全局可变状态。

## 场景目录

```
Assets/AssetRaw/Scenes/
├── MainMenuScene.unity
├── SimulationScene.unity
├── BattleScene_3D_L01.unity
├── BattleScene_3D_L02.unity
└── BattleScene_3D_L03.unity
```

纯 UI 场景（`MainMenuScene`）必须包含 `tag = MainCamera`、`clearFlags = SolidColor` 的相机，避免黑屏或截图异常。

> 早期的 2D 场景（`LobbyScene`、`BattleScene`、`BattleScene_L01`~`L03`）已随 3D 化全部删除。

## 传送门系统

场景间切换通过传送门（Portal System）实现，详细设计见 [`docs/portal-system-design.md`](../portal-system-design.md)。

- 传送门实体挂载 `PortalEntity` 脚本，通过 `ConfigId` 关联 `portal.xlsx` 配置表。
- 触发方式：玩家进入触发区域后按交互键 `E`。
- 传送门类型（`PortalType.cs`）：
  - `portal_return_base`：返回基地（`ProcedureSimulation`）
  - `portal_next_level`：进入下一关卡（`ProcedureBattle`）
  - `portal_select_level`：不切场景，直接打开 `LobbyUI` 选关窗口（基地内 Deploy 门）
  - `portal_custom_scene`：自定义场景跳转
- 流程切换统一通过 `GameApp.ChangeProcedure<T>` 完成，转场效果由 `TransitionUI` 提供灰色渐变。
- 玩家状态保留通过 `PortalPlayerState` 实现，是否保留由配置表 `keepPlayerState` 控制。
