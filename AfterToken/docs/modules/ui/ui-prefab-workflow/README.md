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

## UI 时间缩放

部分 UI 打开时需要暂停或减缓后台游戏进程（设置面板、主菜单、武器轮盘等），同时保持声音播放。统一通过 `Time.timeScale` 控制，由 `GamePauseManager` 管理。

### Inspector 配置

在 UI Prefab 根节点上添加 `UIWindowTimeScale` 组件：

```csharp
public class UIWindowTimeScale : MonoBehaviour
{
    [Range(0f, 1f)] [SerializeField]
    private float _timeScaleWhenVisible = 1f;
}
```

- `0`：完全暂停游戏进程。
- `0.2`：像武器轮盘一样的慢动作。
- `1`：不影响时间（默认）。

`UIWindow.Handle_Completed` 会自动读取该值并覆盖子类 `TimeScaleWhenVisible` 的代码默认值。

### 相关文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `GamePauseManager` | `Assets/GameScripts/HotFix/GameLogic/System/GamePauseManager.cs` | 时间缩放请求栈，取最小值 |
| `UIWindowTimeScale` | `Assets/GameScripts/HotFix/GameLogic/UI/UIWindowTimeScale.cs` | Inspector 配置组件 |
| `UIWindow` | `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` | 自动 Push/Pop 时间缩放 |

详见 `docs/framework/05-UIWorkflow.md` 的《UI 时间缩放》章节。

