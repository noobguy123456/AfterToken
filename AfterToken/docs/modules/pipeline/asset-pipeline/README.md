# 资源管线

## 职责

规范项目资源目录，配置 YooAsset 收集器，支持编辑器模拟构建与正式构建。

## 核心文件

| 文件 | 路径 | 说明 |
|---|---|---|
| `AssetBundleCollectorSetting.asset` | `Assets/Editor/AssetBundleCollector/` | YooAsset 收集器配置 |
| `AssetBundleCollectorConfig.xml` | `Assets/Editor/AssetBundleCollector/` | 收集规则 XML |

## 已完成

- `Assets/AssetRaw/` 目录规范
- UI Prefab `CollectPrefab` + `PackSeparately`
- `EditorSimulateModeHelper.SimulateBuild("DefaultPackage")` 验证

## 设计要点

- 热更资源统一放在 `Assets/AssetRaw/`。
- 资源地址避免冲突，使用唯一路径命名。
