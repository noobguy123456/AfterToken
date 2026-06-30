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
