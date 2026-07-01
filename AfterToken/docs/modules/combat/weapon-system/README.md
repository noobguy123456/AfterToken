# 武器系统

## 职责

管理武器槽、开火调度、换弹、瞄准辅助等武器相关逻辑。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `WeaponSystem` | `Assets/GameScripts/HotFix/GameLogic/System/WeaponSystem.cs` | 武器系统 |
| `WeaponInstance` | `Assets/GameScripts/HotFix/GameLogic/System/WeaponInstance.cs` | 武器运行时实例 |
| `AimAssistSystem` | `Assets/GameScripts/HotFix/GameLogic/System/AimAssistSystem.cs` | 辅助瞄准与火箭锁定 |
| `WeaponConfig` / `WeaponConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 临时武器配置 |
| `IWeaponEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IWeaponEvent.cs` | 武器事件接口 |

## 设计要点

- 武器配置目前为硬编码，后续替换为 Luban `TbWeapon`。
- 开火后根据弹道类型分发到 `BallisticSystem`。
- 辅助瞄准支持普通瞄准和火箭锁定两种模式。
- **自动换弹**：`WeaponInstance.Fire()` 在弹匣打空后自动调用 `Reload()`。
- **换弹状态事件**：`IWeaponEvent.OnReloadStateChanged(ownerId, isReloading)` 在换弹开始/完成时广播，供 UI 展示换弹准星等反馈。

## 换弹中断

### 切换武器中断换弹

换弹过程中切换武器会**立即中断**当前武器的换弹：

1. `WeaponSystem.SwitchToSlot` 在切槽前检测当前武器是否处于 `IsReloading`。
2. 若正在换弹，调用 `WeaponInstance.CancelReload(ownerId)`：
   - 移除未完成的换弹 Timer。
   - 重置 `IsReloading = false`。
   - 广播 `IWeaponEvent.OnReloadStateChanged(ownerId, false)`，通知 UI 取消换弹表现。
3. 切换完成后，被切换走的武器弹药保持换弹前状态；切回该武器需要重新换弹。

### 玩家状态机同步

`PlayerReloadState` 监听 `IWeaponEvent.OnWeaponSwitched`：

- 换弹期间若收到切枪事件，且当前状态仍为 `PlayerReloadState`，则切换回 `PlayerIdleState`。
- `PlayerReloadState.OnLeave` 会自动移除状态机 Timer，避免状态机卡在 Reload。

### 自动换弹

`WeaponInstance.Fire()` 在弹匣打空后自动调用 `Reload(ownerId)`，此时玩家状态机不一定会进入 `PlayerReloadState`；武器实例自身的 Timer 仍可独立完成换弹。切换武器时同样会通过 `CancelReload` 中断。

