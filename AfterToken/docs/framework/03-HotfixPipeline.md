# 03 热更代码管线（HybridCLR）

## 热更程序集

| 程序集 | 路径 | 说明 |
|--------|------|------|
| `GameProto` | `Assets/GameScripts/HotFix/GameProto/` | 数据结构、协议、Luban 运行时库 |
| `GameLogic` | `Assets/GameScripts/HotFix/GameLogic/` | 业务逻辑；`UpdateSetting.LogicMainDllName` 指定为 `GameLogic.dll` |

主包程序集（不可热更）：`Assembly-CSharp`、`Launcher`、`TEngine.Runtime` 等。

## HybridCLR 配置

`ProjectSettings/HybridCLRSettings.asset` 关键项：

```yaml
enable: 1
hotUpdateAssemblies:
- GameProto
- GameLogic
patchAOTAssemblies:
- mscorlib.dll
- System.dll
- System.Core.dll
- TEngine.Runtime.dll
- UniTask.dll
- YooAsset.dll
- UnityEngine.CoreModule.dll
```

`Assets/TEngine/Settings/UpdateSetting.asset` 关键项：

```yaml
HotUpdateAssemblies:
- GameProto.dll
- GameLogic.dll
AOTMetaAssemblies:
- mscorlib.dll
- System.dll
- System.Core.dll
- TEngine.Runtime.dll
- UniTask.dll
- YooAsset.dll
- UnityEngine.CoreModule.dll
LogicMainDllName: GameLogic.dll
AssemblyTextAssetExtension: .bytes
AssemblyTextAssetPath: AssetRaw/DLL
```

## 编译与拷贝热更 DLL

核心工具：`Assets/TEngine/Editor/HybridCLR/BuildDLLCommand.cs`

菜单：`HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath`

流程：

1. `CompileDllCommand.CompileDll(target)`：编译 `GameProto` / `GameLogic`。
2. `CopyAOTAssembliesToAssetPath()`：将 AOT 补充元数据 DLL 拷贝为 `Assets/AssetRaw/DLL/{AssemblyName}.dll.bytes`。
3. `CopyHotUpdateAssembliesToAssetPath()`：将热更 DLL 拷贝为 `Assets/AssetRaw/DLL/{AssemblyName}.dll.bytes`。

## 运行时加载流程

### 主包流程

`Assets/GameScripts/Procedure/` 典型路径：

```
ProcedureLaunch
  → ProcedureSplash
  → ProcedureInitPackage
  → ProcedureInitResources
  → ProcedureCreateDownloader
  → ProcedureDownloadFile
  → ProcedureDownloadOver
  → ProcedureLoadAssembly
  → ProcedurePreload
  → ProcedureStartGame
```

### ProcedureLoadAssembly

`Assets/GameScripts/Procedure/ProcedureLoadAssembly.cs`：

1. 编辑器或关闭热更时：从当前 `AppDomain` 直接获取已加载的 `GameLogic.dll`。
2. 真机热更模式：
   - 通过 YooAsset 加载 `AssetRaw/DLL/GameLogic.dll.bytes`。
   - `Assembly.Load(bytes)` 加载热更程序集。
   - `HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly` 加载 AOT 补充元数据。
3. 反射调用 `GameApp.Entrance(object[] objects)`，将热更程序集列表传入热更域。

```csharp
var appType = _mainLogicAssembly.GetType("GameApp");
var entryMethod = appType.GetMethod("Entrance");
entryMethod.Invoke(appType, new object[] { new object[] { _hotfixAssemblyList } });
```

### 热更域接管

`GameApp.StartGameLogic`（`Assets/GameScripts/HotFix/GameLogic/GameApp.cs`）：

1. `GameModule.Procedure.Shutdown()` 关闭主包流程状态机。
2. 重新初始化热更域流程：`ProcedureMainMenu`、`ProcedureLobby`、`ProcedureBattle`。
3. 启动 `ProcedureMainMenu`。

### 流程切换

```csharp
GameApp.ChangeProcedure<ProcedureBattle>();
```

内部先 `GameModule.UI.CloseAll()`，再反射调用 TEngine FSM 的 `ChangeState<T>()`。

> 当前使用反射是因为 TEngine 未暴露公共的 `ChangeState<T>` 接口，长期建议推动框架侧暴露该 API。

## 开发期注意事项

- 修改热更代码后，若编辑器没有及时编译，使用 `Tools/Force Recompile` 强制刷新。
- 真机测试前必须执行 `HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath`，确保 `Assets/AssetRaw/DLL/` 是最新 DLL。
- 不要从主包直接引用热更程序集中的类型，否则会导致打包或运行时错误。
