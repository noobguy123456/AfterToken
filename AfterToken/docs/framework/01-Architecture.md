# 01 架构总览

## 技术栈

| 组件 | 版本/说明 | 用途 |
|------|-----------|------|
| Unity | 6000.0.76f1 | 引擎本体，2D 物理 + URP/内置管线 |
| TEngine | 嵌入 `Assets/TEngine/` | 程序框架（Procedure、UI、Scene、Resource、Timer 等） |
| HybridCLR | `Packages/com.code-philosophy.hybridclr/` | C# 代码热更 |
| YooAsset | `Packages/YooAsset/` | 资源打包、加载、热更新 |
| UniTask | `Packages/UniTask/` | 异步逻辑，替代 Coroutine/Task |
| TextMeshPro | Unity 内置 TMP | UI 文字渲染 |

## 程序集划分

```
主包（不可热更）
├── Assembly-CSharp      Assets/GameScripts/ 主包入口、GameEntry、Procedure（主包部分）
├── Launcher             Assets/Launcher/     启动器 UI、资源更新流程
└── TEngine.Runtime      Assets/TEngine/Runtime/ 框架核心模块

热更（可热更）
├── GameProto            Assets/GameScripts/HotFix/GameProto/ 协议/数据结构/Bean
└── GameLogic            Assets/GameScripts/HotFix/GameLogic/ 业务逻辑

编辑器
├── Assembly-CSharp-Editor / TEngine.Editor / HybridCLR.Editor 等
```

**依赖规则**：`GameLogic → GameProto → TEngine.Runtime`，禁止反向依赖。

## 系统分层

```
表现层（Presentation）
├── UIWindow / UIWidget（MainMenuUI、LobbyUI、BattleMainUI 等）
└── Entity（PlayerEntity、EnemyEntity、Projectile 等）

系统层（System）
├── PlayerSystem / WeaponSystem / CameraSystem / BattleSystem
└── 通过 GameEvent 或模块 API 与表现层解耦

数据层（Data）
├── LevelConfig / WeaponConfig（当前硬编码）
└── 运行时数据（BattleContext 等临时方案）

框架层（Framework）
└── TEngine 模块（Resource / UI / Scene / Timer / Audio / Fsm）
```

## 核心编码红线

1. **异步优先**：IO / 资源 / 网络操作用 `UniTask`，禁止同步加载、禁止 `Coroutine`。
2. **模块访问**：通过 `GameModule.XXX` 访问，不要直接 `ModuleSystem.GetModule<T>()`。
3. **资源释放**：`LoadAssetAsync` 必须对应 `UnloadAsset`；GameObject 用 `LoadGameObjectAsync` 并交还。
4. **热更边界**：`Assets/GameScripts/Main`（主包）不可热更；`Assets/GameScripts/HotFix/` 全部热更。
5. **事件解耦**：模块间用 `GameEvent`；UI 内部用 `AddUIEvent`。
6. **UI 全屏适配**：全屏 `UIWindow` 在 `OnCreate()` 中调用 `FixFullScreenCanvas()`。

## 关键入口

- **主包入口**：`Assets/GameScripts/GameEntry.cs`
- **热更域入口**：`Assets/GameScripts/HotFix/GameLogic/GameApp.cs`
- **业务模块总线**：`Assets/GameScripts/HotFix/GameLogic/GameModule.cs`
- **流程基类**：`Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs`
