# 02 资源管线

## 目录组织

所有需要进入热更资源包的资源统一放在 `Assets/AssetRaw/`：

```
Assets/AssetRaw/
├── Actor/              # 角色 Prefab
├── Audios/             # 音频
├── Configs/            # 配置数据（当前少量文本）
├── DLL/                # 热更 DLL .bytes
├── Effects/            # 特效 Prefab
├── Fonts/              # 字体（含 TMP FontAsset）
├── Materials/          # 材质
├── Prefabs/            # 通用 Prefab（Player、Projectile、Enemy 等）
├── Scenes/             # 可寻址场景
├── Shaders/            # Shader
├── Textures/           # 贴图 / RenderTexture
├── UI/                 # UI Prefab，按 UI 名称子目录存放
│   ├── BattleMainUI/BattleMainUI.prefab
│   ├── DamageNumberUI/DamageNumberUI.prefab
│   ├── HitFeedbackUI/HitFeedbackUI.prefab
│   ├── LoadingUI/LoadingUI.prefab
│   ├── LobbyUI/LobbyUI.prefab
│   ├── MainMenuUI/MainMenuUI.prefab
│   ├── SniperScopeUI/SniperScopeUI.prefab
│   └── WeaponWheelUI/WeaponWheelUI.prefab
└── UIRaw/              # UI 原始资源
    ├── Atlas/          # 图集
    └── Raw/            # 原始图片
```

> 不要把热更资源放在 `Assets/Resources/`；`Resources/` 仅保留 Launcher 阶段必须的最小资源。

## UI Prefab 目录约定

- 路径：`Assets/AssetRaw/UI/{UI名称}/{UI名称}.prefab`
- 脚本：`Assets/GameScripts/HotFix/GameLogic/UI/{UI名称}/{UI名称}.cs`
- `[Window]` 的 `location` 必须等于 Prefab 文件名（不含扩展名）。
- 不要把 Prefab 直接放在 `Assets/AssetRaw/UI/` 根目录，避免 YooAsset 地址冲突。

## YooAsset 收集器

配置文件：

- `Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`
- `Assets/Editor/AssetBundleCollector/AssetBundleCollectorConfig.xml`（保持与 .asset 一致）

### DefaultPackage 分组

| Group | 路径 | AddressRule | PackRule | FilterRule |
|-------|------|-------------|----------|------------|
| Actor | `Assets/AssetRaw/Actor` | AddressByFileName | PackDirectory | CollectAll |
| Audios | `Assets/AssetRaw/Audios` | AddressByFileName | PackDirectory | CollectAll |
| Configs | `Assets/AssetRaw/Configs` | AddressByFileName | PackDirectory | CollectAll |
| DLL | `Assets/AssetRaw/DLL` | AddressByFileName | PackDirectory | CollectAll |
| Effects | `Assets/AssetRaw/Effects` | AddressByFileName | PackDirectory | CollectAll |
| Fonts | `Assets/AssetRaw/Fonts` | AddressByFileName | PackDirectory | CollectAll |
| Materials | `Assets/AssetRaw/Materials` | AddressByFileName | PackDirectory | CollectAll |
| Prefabs | `Assets/AssetRaw/Prefabs` | AddressByFileName | PackDirectory | CollectAll |
| Scenes | `Assets/AssetRaw/Scenes` | AddressByFileName | PackDirectory | CollectAll |
| UI | `Assets/AssetRaw/UI` | AddressByFileName | PackSeparately | CollectPrefab |
| UIRaw | `Assets/AssetRaw/UIRaw/Atlas`<br>`Assets/AssetRaw/UIRaw/Raw` | AddressByFileName | PackDirectory | CollectAll |

### UI 收集器关键设置

```xml
<Collector CollectPath="Assets/AssetRaw/UI"
           AddressRule="AddressByFileName"
           PackRule="PackSeparately"
           FilterRule="CollectPrefab" />
```

- `CollectPrefab`：只收集 `.prefab` 文件，不收集文件夹资产。
- `PackSeparately`：每个 UI Prefab 独立成包。
- 这样既能使用 `Assets/AssetRaw/UI/{Name}/{Name}.prefab` 目录约定，又不会因文件夹与 Prefab 同名导致地址重复。

## 模拟构建（开发期）

编辑器下资源加载走 `EditorSimulateMode`，每次资源结构变更后需要刷新模拟清单：

```csharp
var result = YooAsset.EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
```

开发期菜单：

- `Battle/Setup Battle Scene & Resources`：自动补齐 Prefab / 场景，并执行 SimulateBuild。

## 正式构建

构建入口在 `Assets/TEngine/Editor/ReleaseTools/ReleaseTools.cs`：

- `TEngine/Build/一键打包AssetBundle`
- `TEngine/Build/一键打包Window / Android / IOS`
- `TEngine/Build/打包工具窗口`

默认参数：

- 管线：`ScriptableBuildPipeline`
- 压缩：`LZ4`
- 包名：`DefaultPackage`
- 版本号：`yyyy-MM-dd-{当日分钟}`

完整流程（`ReleaseTools.BuildWithConfig`）：

1. 可选：编译热更 DLL（`BuildDLLCommand.BuildAndCopyDlls`）
2. `AssetDatabase.Refresh()`
3. YooAsset 构建 DefaultPackage
4. 可选：最小包处理（按 tag 保留/删除 bundle）
5. `AssetDatabase.Refresh()`
6. 可选：构建 Player

## 热更新资源配置

`Assets/TEngine/Settings/UpdateSetting.asset`：

- 资源下载地址：`http://127.0.0.1:8081`
- 备用下载地址：`http://127.0.0.1:8082`
- 更新方式：`UpdateStyle.Force`
- 运行模式：编辑器下为 `EditorSimulateMode`，真机为 `HostPlayMode`

最终下载 URL：`{ResDownLoadPath}/{projectName}/{platform}/`
