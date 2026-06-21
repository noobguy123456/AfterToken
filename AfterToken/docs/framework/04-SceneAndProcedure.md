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
| `ProcedureLobby` | `LobbyScene` | `LobbyUI` |
| `ProcedureBattle` | `BattleScene` / `BattleScene_L01` | `BattleMainUI`、`DamageNumberUI`、`HitFeedbackUI` |

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
├── LobbyScene.unity
├── BattleScene.unity
└── BattleScene_L01.unity
```

纯 UI 场景（`MainMenuScene`、`LobbyScene`）必须包含 `tag = MainCamera`、`clearFlags = SolidColor` 的相机，避免黑屏或截图异常。
