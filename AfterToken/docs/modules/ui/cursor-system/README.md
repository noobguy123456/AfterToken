# 光标系统

## 职责

管理游戏内光标资源的显示、隐藏与样式切换：

- 战斗/Gameplay 界面隐藏系统光标，避免与游戏准星重叠。
- 打开菜单、武器轮盘等需要鼠标操作的 UI 时显示自定义光标。
- 支持不同 UI 配置不同的光标资源。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `CursorManager` | `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/CursorManager.cs` | 光标管理器单例 |
| `MainMenuUI` | `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuUI/MainMenuUI.cs` | 设置默认光标示例 |

## 设计要点

### 光标显示管理

- 使用引用计数管理光标显示请求：
  - `ShowCursor()`：引用计数 +1，首次增加到 1 时显示光标。
  - `HideCursor()`：引用计数 -1，归零时隐藏光标。
  - `ForceShowCursor()` / `ForceHideCursor()`：强制设置状态，用于流程切换。
- 支持两种锁定模式：
  - `Free`：自由光标，可见时不锁定。
  - `Locked`：隐藏光标时锁定在屏幕中心（射击模式）。
- 解锁延迟：Windows 下从 `CursorLockMode.Locked` 切回 `None` 需要约一帧才能真正释放鼠标。`CursorManager.ApplyCursorState` 在显示光标前会先解锁，并在必要时 `UniTask.Yield()` 等待一帧，避免光标短暂锁死在屏幕中心。

### 光标资源配置

`CursorManager` 支持设置默认光标和临时光标：

```csharp
// 设置默认光标（所有需要光标的 UI 共用）
CursorManager.Instance?.SetDefaultCursor(texture, hotSpot);

// 为当前 UI 临时切换光标（关闭后自动恢复默认）
CursorManager.Instance?.SetCursor(texture, hotSpot);
```

- `texture`：光标纹理（`Texture2D`），建议尺寸 32×32。
- `hotSpot`：点击热点，以纹理左上角为原点，单位像素。

当前 `MainMenuUI.OnCreate()` 中通过代码生成一个默认箭头光标。若后续希望替换为美术资源，可将纹理放到 `Assets/AssetRaw/Textures/Cursor_Default.png`，并在 `MainMenuUI.SetupDefaultCursor()` 中改为 YooAsset 加载：

```csharp
var texture = await GameModule.Resource.LoadAssetAsync<Texture2D>("Cursor_Default");
Vector2 hotSpot = new Vector2(texture.width * 0.1f, texture.height * 0.1f);
CursorManager.Instance?.SetDefaultCursor(texture, hotSpot);
```

## 需要光标的 UI

以下 UI 在 `OnCreate` 中调用 `CursorManager.Instance.ShowCursor()`，在 `OnDestroy` 中调用 `HideCursor()`：

- `MainMenuUI`：同时设置默认光标
- `LobbyUI`
- `WeaponWheelUI`

后续新增的需要鼠标操作的 UI（如设置 UI、经营 UI）也应遵循此约定。

## 不需要光标的 UI

以下 UI 不管理光标状态，保持当前流程设定：

- `BattleMainUI`（使用准星）
- `SniperScopeUI`
- `DamageNumberUI`
- `HitFeedbackUI`
- `LoadingUI`

## 从 UI 返回战斗后的光标隐藏

### 问题背景

Windows 下从菜单/设置等 UI 返回战斗时，即使代码设置了 `Cursor.visible = false` 与 `Cursor.lockState = Locked`，光标仍可能保持可见，需要玩家点击游戏画面后才会被隐藏并锁定。这是因为：

1. UI 打开时光标被解锁（`CursorLockMode.None`）并显示。
2. 关闭 UI 后重新设置 `Cursor.lockState = Locked` 时，若游戏窗口尚未获得焦点或鼠标不在窗口内，Windows 可能不会立即捕获光标。

### 解决方案

`CursorManager` 采用两层保障：

1. **延迟确认**：在 `ApplyCursorState` 隐藏分支中，设置隐藏/锁定后延迟一帧再次强制设置 `Cursor.visible = false` 与 `Cursor.lockState = Locked`，给 Unity/Windows 一个状态同步的机会。
2. **焦点补偿**：订阅 `Application.focusChanged` 事件，当窗口重新获得焦点且当前应处于战斗状态（`Locked` 模式 + 无 UI 请求显示光标）时，再次调用 `ApplyCursorState()`。

### 代码要点

```csharp
private async void ApplyCursorState()
{
    // ... 取消上一次延迟应用 ...

    bool visible = _showRefCount > 0;
    if (visible)
    {
        // 先解锁，必要时等待一帧，再显示光标
        // ...
    }
    else
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = false;
        if (_currentMode == GameCursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 延迟一帧再次确认
        try { await UniTask.Yield(ct); }
        catch (OperationCanceledException) { return; }

        if (_showRefCount == 0 && _currentMode == GameCursorLockMode.Locked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
```

> 注意：若关闭 UI 后游戏窗口本身未处于焦点（例如玩家点击了其他窗口），光标仍要等到窗口重新获得焦点才会隐藏。这是 Windows 的安全限制，应用无法强制无焦点的窗口捕获光标。

