# AfterToken 资源框架文档

> 本文档整理 AfterToken 项目的资源与代码管线，方便新成员和 AI 协作者快速理解“资源从哪里来、如何被打包、如何被加载、如何热更”。

## 项目定位

AfterToken 是一款使用 Unity 6000.0.76f1 开发的俯视角 2D 射击游戏原型。整体框架采用：

- **TEngine**：程序框架（Procedure、UI、Scene、Resource、Timer 等）
- **HybridCLR**：C# 热更
- **YooAsset**：资源管理 / 热更资源包
- **UniTask**：异步编程
- **TextMeshPro**：UI 文字渲染（已全量迁移）

## 文档导航

| 文档 | 内容 |
|------|------|
| [01-Architecture.md](./01-Architecture.md) | 程序集划分、系统分层、核心编码红线 |
| [02-AssetPipeline.md](./02-AssetPipeline.md) | `Assets/AssetRaw/` 目录、YooAsset 收集器、模拟构建与正式构建 |
| [03-HotfixPipeline.md](./03-HotfixPipeline.md) | HybridCLR 配置、热更 DLL 编译与加载、AOT 元数据补充 |
| [04-SceneAndProcedure.md](./04-SceneAndProcedure.md) | 启动场景、主包流程、热更域流程、场景切换 |
| [05-UIWorkflow.md](./05-UIWorkflow.md) | UI Prefab 规范、UIWindow 生命周期、加载接口、编辑器工具 |
| [06-ConfigSystem.md](./06-ConfigSystem.md) | 当前硬编码配置、Luban 接入规划 |
| [07-BuildAndRelease.md](./07-BuildAndRelease.md) | TEngine 发布工具、一键构建流程、最小包模式 |

## 快速索引

### 关键路径

- 启动场景：`Assets/Scenes/main.unity`
- 热更代码：`Assets/GameScripts/HotFix/GameLogic/`
- 热更协议/数据结构：`Assets/GameScripts/HotFix/GameProto/`
- 热更资源根目录：`Assets/AssetRaw/`
- UI Prefab 根目录：`Assets/AssetRaw/UI/`
- YooAsset 收集器：`Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`
- 热更设置：`Assets/TEngine/Settings/UpdateSetting.asset`
- HybridCLR 设置：`ProjectSettings/HybridCLRSettings.asset`

### 常用菜单

| 菜单 | 作用 |
|------|------|
| `Battle/Setup Battle Scene & Resources` | 开发期一键补齐战斗 Prefab、场景、UI，并执行 SimulateBuild |
| `Tools/UI/Create UI Prefab` | 单个 UI 脚本 + Prefab 生成 |
| `Tools/Migration/Migrate UI Prefabs to TMP` | 将 UI Prefab 的 uGUI Text 迁移为 TextMeshProUGUI |
| `HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath` | 编译并拷贝热更 DLL |
| `TEngine/Build/一键打包AssetBundle` | 正式 AssetBundle 构建 |
| `Tools/Force Recompile` | 热更代码修改后强制刷新编译 |

## 与本项目其他文档的关系

- 人机协作规范：`../CoWork/UI-Prefab-CoWork-Workflow.md`
- UI / Prefab 避坑指南：`../notice/UI-and-Prefab-Pitfalls.md`
- 架构决策记录：`../adr/`
- 领域上下文与术语：`../CONTEXT.md`
- AI 编码强制工作流：`../../CLAUDE.md`
