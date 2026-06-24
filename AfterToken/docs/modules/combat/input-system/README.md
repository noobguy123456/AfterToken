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
