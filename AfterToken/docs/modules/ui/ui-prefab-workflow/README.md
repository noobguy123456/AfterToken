# UI Prefab 工作流

## 职责

统一热更域 UI 的开发、生成与加载流程。所有 UI 窗口使用 TEngine `UIWindow` + `[Window(...)]` Attribute，并通过 Prefab 驱动，布局与逻辑分离。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `UIWindow` | `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` | TEngine UI 窗口基类 |
| `WindowAttribute` | `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs` | 声明 UI 层级、Prefab 路径 |
| `ScriptGenerator` | 各 UI 类中的 `#region 脚本工具生成的代码` | 自动绑定 Prefab 节点引用 |
| `UIPrefabGenerator` | `Assets/Editor/UI/UIPrefabGenerator.cs` | 编辑器一键生成 UI Prefab |
| `UIScriptGenerator` | `Assets/Editor/UIScriptGenerator/` | 根据 Prefab 生成/更新 UI 脚本 |

## 已完成的 UI

- `MainMenuUI` / `LobbyUI` / `LoadingUI`
- `BattleMainUI` / `DamageNumberUI` / `HitFeedbackUI`
- `WeaponWheelUI` / `SniperScopeUI`

## 设计要点

- 所有热更域 UI Prefab 必须放在 `Assets/AssetRaw/UI/` 下。
- `WindowAttribute` 使用 `location` 参数，走 YooAsset 热更加载。
- 节点命名遵循 TEngine 前缀：`m_text_`、`m_btn_`、`m_rect_`、`m_img_` 等。
- `LoadingUI` 位于 `UILayer.System`，保证覆盖在所有普通 UI 之上。
