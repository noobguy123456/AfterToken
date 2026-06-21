# AfterToken 项目上下文

## 项目简介

`AfterToken` 是一款使用 Unity 6000.0.76f1 开发的俯视角 2D 射击游戏原型。项目采用 TEngine 作为底层框架，HybridCLR 实现热更，YooAsset（EditorSimulateMode）管理资源，UniTask 处理异步逻辑。

## 技术栈

- **Unity**：6000.0.76f1（2D URP / 内置渲染管线，视配置而定）
- **物理**：2D 物理（`Rigidbody2D` / `Collider2D`）
- **框架**：TEngine（Procedure、UI、Scene、Resource、Timer 等模块）
- **热更**：HybridCLR
- **资源**：YooAsset（当前为 EditorSimulateMode）
- **异步**：UniTask

## 程序集划分

| 程序集 | 路径/用途 | 说明 |
|--------|-----------|------|
| `Assembly-CSharp` | `Assets/Launcher/` 等 | 主包（Launcher），不可热更 |
| `GameProto` | `Assets/GameScripts/HotFix/GameProto/` | 热更协议/数据结构 |
| `GameLogic` | `Assets/GameScripts/HotFix/GameLogic/` | 热更业务逻辑 |
| 其他 Editor 程序集 | `Assets/Editor/` | 编辑器工具 |

热更入口由 Launcher 的 `UpdateSetting.LogicMainDllName = GameLogic.dll` 指定。

## 关键目录

```
Assets/
├── Launcher/                       # 主包启动器代码
├── GameScripts/HotFix/GameLogic/   # 热更业务逻辑
│   ├── Procedure/                  # 游戏流程（主菜单、大厅、战斗）
│   ├── UI/                         # UIWindow（MainMenuUI、LobbyUI、LoadingUI、BattleMainUI）
│   ├── System/                     # 战斗运行时系统（PlayerSystem、WeaponSystem 等）
│   ├── Config/                     # 临时配置管理（LevelConfigMgr）
│   └── Module/UIModule/            # TEngine UI 的本地扩展/覆盖
├── GameScripts/HotFix/GameProto/   # 热更数据结构
├── AssetRaw/Scenes/                # 可寻址场景资源
│   ├── MainMenuScene.unity
│   ├── LobbyScene.unity
│   ├── BattleScene.unity
│   └── BattleScene_L01.unity
├── AssetRaw/UI/                    # UI Prefab
│   ├── MainMenuUI/
│   ├── LobbyUI/
│   ├── LoadingUI/
│   └── BattleMainUI/
├── Editor/BattleSetup/             # 战斗场景/资源快速创建工具
└── Scenes/main.unity               # 启动场景（GameEntry + UIRoot）
```

## 流程架构

### Launcher 流程（主包）

`main.unity` 启动后，TEngine 的 `ProcedureModule` 执行主包流程，典型路径：

```
ProcedureLaunch → ... → ProcedureLoadAssembly
```

加载热更 DLL 后，进入热更域。

### 热更流程（GameLogic）

`GameApp.StartGameLogic()` 中：

1. `GameModule.Procedure.Shutdown()` 关闭主包流程状态机。
2. 重新 `Initialize` 热更域流程列表：`ProcedureMainMenu`、`ProcedureLobby`、`ProcedureBattle`。
3. 启动 `ProcedureMainMenu`。

运行时切换流程通过 `GameApp.ChangeProcedure<T>()` 调用 TEngine 内部 `_procedureFsm.ChangeState<T>()`（反射实现，见 [ADR-0004](adr/0004-hotfix-procedure-takeover.md)）。

### GameplayProcedureBase

所有游戏流程继承自 `GameplayProcedureBase`，统一负责：

- 管理 `CancellationTokenSource`，支持场景加载取消。
- 通过 `LoadSceneWithLoadingAsync` 在场景切换时显示/关闭 `LoadingUI` 并更新进度。
- 在 `OnLeave` 时执行 `CloseAll`、`RemoveAllTimer`、`UnloadUnusedAssets` 等清理。

子流程只需实现 `EnterAsync()`，声明目标场景名与加载完成后的业务逻辑。

## UI 规范

- 使用 TEngine `UIWindow` + `[Window(...)]` Attribute。
- 节点命名遵循 TEngine 约定前缀，方便 `ScriptGenerator` 绑定：
  - `m_text_`：Text 组件
  - `m_btn_`：Button 组件
  - `m_rect_`：RectTransform / 布局容器
  - `m_img_`：Image 组件
  - `m_slider_`：Slider 组件
- `LoadingUI` 位于 `UILayer.System`，保证覆盖在所有普通 UI 之上。
- `MainMenuUI`、`LobbyUI` 已改为 Prefab 驱动，布局与逻辑分离。

## 跨流程数据

当前使用静态类 `BattleContext.CurrentLevelId` 在 `LobbyUI` 与 `ProcedureBattle` 之间传递关卡 ID。这是临时方案，后续应迁移到正式运行时数据层。

## 编辑器工具

### Battle/Setup Battle Scene & Resources

路径：`Battle/Setup Battle Scene & Resources`

用于：

- 创建/更新战斗场景（`BattleScene`、`BattleScene_L01`）及所需系统根节点。
- 创建/更新 UI Prefab（`MainMenuUI`、`LobbyUI`、`LoadingUI`、`BattleMainUI`）。
- 执行 `EditorSimulateModeHelper.SimulateBuild("DefaultPackage")` 更新 YooAsset 模拟清单。

### Tools/Force Recompile

路径：`Tools/Force Recompile`

当热更 DLL 修改后编辑器没有及时刷新时，使用此菜单强制触发脚本编译。

## 已知技术债

1. **GameApp 反射切换流程**：`ChangeProcedure<T>()` 通过反射调用 TEngine 内部 FSM，脆弱且存在 AOT 风险。长期应推动 TEngine 暴露 `ChangeState<T>()`。
2. **BattleContext 静态状态**：跨流程数据使用全局可变状态，不利于测试与扩展。
3. **硬编码关卡表**：`LevelConfigMgr` 当前为硬编码，后续应接入 Luban 配置表。
4. **CameraSystem 生命周期**：战斗场景的主相机在 `ProcedureBattle` 中动态获取或添加 `CameraSystem`，离开战斗时销毁组件；需持续验证多场景切换稳定性。
5. **运行时验证依赖 Editor**：当前资源为 EditorSimulateMode，完整真机包体验需配置 YooAsset 真实构建流程。

## 最近重要变更

- 新增 `GameplayProcedureBase`，统一游戏流程的加载与清理。
- `ProcedureMainMenu`、`ProcedureLobby`、`ProcedureBattle` 改为继承 `GameplayProcedureBase`。
- 场景切换时统一显示 `LoadingUI` 并更新进度。
- `MainMenuUI`、`LobbyUI` 改为 Prefab 驱动，`ScriptGenerator` 绑定节点。
- `BattleSceneSetup` 改为非破坏性更新已有 Prefab。
- 新增/更新 ADR：`0001` ~ `0004`。

## 如何继续开发

1. 打开 `Assets/Scenes/main.unity`。
2. 进入 Play Mode，观察 Launcher 流程是否正常加载热更 DLL 并进入 `ProcedureMainMenu`。
3. 点击“开始游戏”进入 `ProcedureLobby`，选择关卡进入 `ProcedureBattle`。
4. 若热更代码修改后未生效，使用 `Tools/Force Recompile`。
5. 若修改了战斗场景或 UI Prefab，运行 `Battle/Setup Battle Scene & Resources` 后重新执行 `EditorSimulateModeHelper.SimulateBuild`。
