# 07 构建与发布

## 发布工具

| 文件 | 说明 |
|------|------|
| `Assets/TEngine/Editor/ReleaseTools/ReleaseTools.cs` | 核心构建静态类 |
| `Assets/TEngine/Editor/ReleaseTools/BuildPipelineWindow.cs` | 可视化打包窗口 |
| `Assets/TEngine/Editor/ReleaseTools/BuildConfig.cs` | 构建参数配置 |

## 菜单入口

- `TEngine/Build/一键打包AssetBundle`
- `TEngine/Build/一键打包Window`
- `TEngine/Build/一键打包Android`
- `TEngine/Build/一键打包IOS`
- `TEngine/Build/打包工具窗口`

## BuildConfig 关键参数

```csharp
public class BuildConfig
{
    public BuildTarget BuildTarget;
    public EBuildPipeline BuildPipeline = EBuildPipeline.ScriptableBuildPipeline;
    public ECompressOption CompressOption = ECompressOption.LZ4;
    public EncryptionType EncryptionType = EncryptionType.None;
    public string PackageVersion = "";
    public string OutputRoot = "./Builds/";

    public bool MinimalPackage;           // 最小包模式
    public string RetainTags = "";        // 保留 tag

    public bool BuildHotFixDll = true;    // 构建前编译热更 DLL
    public bool BuildPlayer;              // 是否构建 Player
    public BuildTarget PlayerPlatform;
    public string PlayerOutputPath = "";
}
```

## 一键构建流程

`ReleaseTools.BuildWithConfig`：

1. 编译热更 DLL（`BuildDLLCommand.BuildAndCopyDlls`）
2. `AssetDatabase.Refresh()`
3. YooAsset 构建 DefaultPackage
4. 最小包处理（可选）：删除 StreamingAssets 中未被 `RetainTags` 保留的 bundle
5. `AssetDatabase.Refresh()`
6. 构建 Player（可选）

## 开发期构建

编辑器下不需要构建 AssetBundle，资源通过 `EditorSimulateMode` 直接读取。修改资源结构后执行：

```csharp
YooAsset.EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
```

常用菜单：

| 菜单 | 作用 |
|------|------|
| `Battle/Setup Battle Scene & Resources` | 补齐 Prefab / 场景，并执行 SimulateBuild |
| `HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath` | 编译并拷贝热更 DLL |
| `Tools/Force Recompile` | 热更代码修改后强制刷新编译 |

## 最小包模式

- 开启 `MinimalPackage` 后，只保留 `RetainTags` 指定的 bundle 在包体内。
- 其余 bundle 通过热更新下载。
- 发布前务必确认 `RetainTags` 设置正确，避免首次启动缺少必要资源。

## 发布检查清单

- [ ] 执行 `HybridCLR/Build/BuildAssets And CopyTo AssemblyTextAssetPath` 生成最新 DLL
- [ ] 确认 `Assets/AssetRaw/DLL/` 已更新
- [ ] 执行 YooAsset 构建或一键打包
- [ ] 确认 `UpdateSetting.asset` 中的下载地址与 CDN 一致
- [ ] 真机测试首次安装与热更新流程
