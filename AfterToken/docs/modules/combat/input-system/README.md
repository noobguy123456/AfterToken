# 输入系统

## 职责

读取玩家输入（移动、瞄准、开火、换弹、切枪等），并将其转换为战斗事件，供其他系统消费。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `InputSystem` | `Assets/GameScripts/HotFix/GameLogic/System/InputSystem.cs` | 输入监听与事件发送 |
| `IBattleInputEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IBattleInputEvent.cs` | 输入事件接口 |

## 事件列表

- `OnMoveInput(Vector2 direction)`
- `OnAimInput(Vector2 worldPosition)`
- `OnFirePressed()` / `OnFireReleased()`
- `OnReloadPressed()`
- `OnWeaponSwitch(int slot)`

## 设计要点

- 输入系统只负责读取和广播事件，不直接修改玩家或武器状态。
- 后续可扩展为支持手柄/触屏的抽象输入层。

## 暂停 UI 打开时的输入屏蔽

当设置面板、背包等暂停游戏进程的 UI 打开时，`Time.timeScale` 会被设为 0。由于 `InputSystem` 直接读取 `Input` 状态，**不会被 UI 射线自动拦截**，因此需要在 `Update` 中主动跳过战斗输入，避免点击穿透到游戏场景触发开火、后坐力、相机抖动等问题。

```csharp
private void Update()
{
    // ESC 作为全局关闭键始终处理，优先关闭当前最上层可关闭 UI
    HandleEscapeInput();

    // 游戏暂停时跳过所有战斗输入
    if (Time.timeScale <= Mathf.Epsilon)
    {
        return;
    }

    HandleMoveInput();
    HandleAimInput();
    HandleFireInput();
    // ...
}
```

### ESC 键统一关闭逻辑

- `HandleEscapeInput` 在检测到 `Escape` 按下后，按 UI 层级从高到低尝试关闭最上层弹窗（一次 ESC 只关闭一个）。
- 当前可关闭 UI 包括：`SettingsUI` → `BattleBagUI`。
- 没有任何可关闭 UI 打开时，按 ESC 打开 `SettingsUI`。
- UI 内部关闭按钮与 ESC 全局关闭不冲突（均调用 `GameModule.UI.CloseUI<T>()`）。

### 影响

- 设置面板 / 背包 / 主菜单等 `Time.timeScale = 0` 的 UI 打开时：
  - 鼠标点击、键盘 WASD、开火、换弹等战斗输入不会传递到战斗系统。
  - `Escape` 键仍可关闭当前最上层 UI。
- 武器轮盘等 `Time.timeScale = 0.2` 的慢动作 UI 打开时：
  - 战斗输入继续处理，轮盘切换武器逻辑正常运行。

