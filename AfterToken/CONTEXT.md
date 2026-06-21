# AfterToken 领域上下文

本文档定义 AfterToken 项目中与热更流程、UI、关卡选择相关的领域术语。通用编程概念（如异步、反射、MonoBehaviour）不在此列出。

## 流程（Procedure）

游戏状态机中的一个状态，代表一个高层游戏阶段。

- **主包流程**：不可热更，负责启动、资源更新、加载热更 DLL。
- **热更流程**：位于 `GameLogic` 程序集，负责主菜单、大厅、战斗等业务阶段。
- **流程切换**：从一个 `Procedure` 进入另一个 `Procedure`，通常伴随场景加载与 UI 切换。

_Avoid_: State（太泛）、Scene（流程不等于场景）。

## 热更域（Hotfix Domain）

`GameLogic` + `GameProto` 两个程序集组成的可热更代码层。主包通过反射调用 `GameApp.Entrance` 进入热更域。

_Avoid_: 热更代码（口语可用，正式文档用“热更域”）。

## 关卡（Level）

玩家可选择的战斗配置单元。一个关卡包含：场景地址、敌人配置、玩家初始血量、默认武器等。

当前 `LevelConfig` 是硬编码占位实现，后续由 Luban 配置表替换。

_Avoid_: 地图（仅指场景，不含敌人/数值配置）。

## 关卡选择大厅（Lobby）

玩家选择关卡的流程与界面。从主菜单进入，选择关卡后进入战斗。

_Avoid_: 选关界面（仅指 UI，Lobby 包含流程与场景）。

## 战斗上下文（BattleContext）

用于在 `ProcedureLobby` 与 `ProcedureBattle` 之间传递当前选中关卡 ID 的临时静态上下文。

_Avoid_: 全局变量（这是实现方式，不是领域概念）。

## 全屏 UI（FullScreen UI）

覆盖整个屏幕的 `UIWindow`，如 `MainMenuUI`、`LobbyUI`、`LoadingUI`、`BattleMainUI`。它们嵌套在 `UIRoot/UICanvas` 下，不使用独立的 `CanvasScaler`。

_Avoid_: 全屏窗口（同义，但文档统一用“全屏 UI”）。

## 加载过渡 UI（LoadingUI）

在场景切换期间显示的 `UIWindow`，位于 `UILayer.System`，显示进度条与百分比。

_Avoid_: 加载界面（太泛）。

## 表现层（Presentation Layer）

负责渲染、动画、UI 布局、输入触发的代码层。在 TEngine 中由 `UIWindow`、`UIWidget`、`PlayerEntity` 等表现类构成。

表现层不直接执行业务逻辑，而是通过事件与系统层通信。

_Avoid_: UI 层（仅指 UI，表现层还包括场景实体）。

## 系统层（System Layer）

负责业务逻辑、状态管理、数据计算的代码层。如 `PlayerSystem`、`WeaponSystem`、`BattleSystem`。

系统层通过 `GameEvent` 与表现层解耦。

_Avoid_: Manager（旧称，项目统一用 System）。
