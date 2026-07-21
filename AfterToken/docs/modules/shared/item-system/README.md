# 道具系统（Item System）

> 所属模块：共享系统
> 关联：背包系统 `../inventory-system/`、掉落拾取 `../../combat/pickup-system/`、Luban 配置 `../../pipeline/luban-config-system/`

## 职责

- 道具的静态配置（Luban `cfg.Item`）与运行时堆叠表示（`ItemStack`）。
- 稀有度体系：4 档 `蓝(Blue) < 紫(Purple) < 黄(Yellow) < 红(Red)`（`cfg.EQuality`），稀有度颜色映射供世界掉落物与 UI 稀有度框共用。

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `GameLogic/Item/ItemStack.cs` | 道具堆叠 struct：`{ ItemId, Count }`，背包/仓库最小存储单元 |
| `GameLogic/Item/RarityColors.cs` | 稀有度 → 颜色映射（占位期染色；接美术后可换框图） |
| `GameLogic/Config/ItemConfigMgr.cs` | `Tables.TbItem` 包装：`Get / GetName / GetQuality / GetStackLimit` |
| `GameLogic/Config/DropConfigMgr.cs` | `Tables.TbDrop` 包装：`GetDropsForEnemy(enemyId)` |
| `GameLogic/Config/InventoryConfigMgr.cs` | `Tables.TbInventoryConfig` 包装：临时背包/仓库槽位上限 |
| `GameLogic/UI/Widget/ItemSlotWidget.cs` | 道具格子 Widget：稀有度框 + 图标 + 数量 |
| `Assets/AssetRaw/UI/ItemSlot/ItemSlot.prefab` | 稀有度框 Prefab，供各背包 UI 复用 |

## 配置表

- `cfg.Item`（`Datas/item.xlsx`）：`id / name / desc / price / quality(cfg.EQuality) / itemType(cfg.EItemType) / icon / stackLimit / upgradeToItemId / expireTime / batchUseable / exchangeList`
- `cfg.EQuality`（`__enums__.xlsx`）：`Blue=0 / Purple=1 / Yellow=2 / Red=3`
- `cfg.EItemType`：`Material=0 / Consumable=1`（使用效果逻辑后续接入）
- `cfg.InventoryConfig`（`Datas/inventory.xlsx`）：`tempBagCapacity / warehouseCapacity`
- `cfg.Drop`（`Drop@battle.xlsx`）：`enemyId / itemId / dropRate(万分之一) / minCount / maxCount`

> **改表约定**：用户会多次迭代 item 表结构。业务代码只允许通过三个 ConfigMgr 访问配置；改表后跑 `Configs/GameConfig/gen_code_bin_to_project.bat` 并同步对应 ConfigMgr 即可。

## 设计要点

- 图标字段 `icon` 已预留（YooAsset location，走 `SetSprite` 缓存池）；当前为占位白图 + 稀有度染色。
- `stackLimit` 未配置或异常时按 1 处理（`ItemConfigMgr.GetStackLimit` 兜底）。
- **悬浮提示**：`ItemSlotWidget.SetItem` 会为格子挂 `ItemSlotHoverHandler`（转发鼠标进入/离开），`ItemTooltipUI`（UILayer.Tips，跟随鼠标）展示配置表信息：名称/稀有度（染色）/类型/价格/描述。提示内容可后续完善，数据均来自 `ItemConfigMgr`。
