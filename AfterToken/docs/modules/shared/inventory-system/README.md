# 背包系统（Inventory System）

> 所属模块：共享系统
> 关联：道具系统 `../item-system/`、掉落拾取 `../../combat/pickup-system/`

## 职责

- 管理两类背包：**关卡临时背包**（一局战斗的生命周期）与**玩家仓库**（长期持有）。
- 容量均来自配置表（`cfg.InventoryConfig`），槽位制：一个道具堆叠占 1 槽。

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `GameLogic/Item/RunInventory.cs` | 关卡临时背包（静态）。`TryAdd / Clear / Items / UsedSlots / MaxSlots` |
| `GameLogic/Item/Warehouse.cs` | 玩家仓库（静态，本期内存态）。`TryAdd / AddAll / Items / UsedSlots / MaxSlots` |
| `GameLogic/IEvent/IItemEvent.cs` | 事件接口：`OnItemPickedUp / OnTempInventoryChanged / OnWarehouseChanged / OnInventoryFull` |
| `GameLogic/UI/BattleBagUI/` | 战斗内临时背包面板（B 键开关），显示 `当前容量/最大容量` 与格子 |
| `GameLogic/UI/WarehouseUI/` | 仓库面板（大厅 LobbyUI 的 Warehouse 按钮进入），含关闭按钮 |

## 生命周期规则

- **新一局开始**：`ProcedureLobby.OnEnter` 统一 `RunInventory.Clear()`（主菜单/死亡/胜利回大厅均覆盖）。
- **死亡**：`PlayerDeathHandler.OnPlayerDied` 清空临时背包（死亡全丢）。
- **胜利（RETURN_TO_LOBBY 传送门）**：`PortalSystem.ExecuteTransition` 先 `Warehouse.AddAll(RunInventory.Items)` 再切流程。
- **传送门跨战斗场景**（NEXT_LEVEL / CUSTOM_SCENE）：临时背包保留。

## 设计要点

- `TryAdd` 为**整批判定**：优先填充已有堆叠，剩余需要新槽位时若容量不足则整批失败（掉落物留在地上可稍后拾取），并发 `OnInventoryFull`。
- 仓库满时放入失败仅记警告日志（容量 200，设计上很难触达）。
- 仓库为内存态（重启清空），持久化由 save-system 模块统一实现。
