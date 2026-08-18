# 战利品容器系统（Loot Container System）

> 所属模块：战斗系统（搜打撤）
> 关联：道具系统 `../../shared/item-system/`、背包系统 `../../shared/inventory-system/`、拾取系统 `../pickup-system/`

## 职责

- 关卡内摆放可交互容器（箱子），玩家靠近后按 E 打开面板，拿取箱内道具进临时背包。
- 箱内道具在首次打开时按权重表 `TbLootContainer` 掷点生成；拿空后容器变"已开"（变灰，不可再交互）。

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `GameLogic/System/LootContainerSystem.cs` | 管理当前可交互容器、E 键开箱、拿取逻辑；由 `ProcedureBattle` 挂载到 BattleRoot |
| `GameLogic/Entity/LootContainer/LootContainerEntity.cs` | 容器实体。trigger 检测玩家进出，占位视觉（0.9m 平躺方块，棕色→开后变灰），持有箱内 `ItemStack` 列表 |
| `GameLogic/System/Registry/LootContainerRegistry.cs` | 容器注册表（OnEnable/OnDisable 自动注册） |
| `GameLogic/Config/LootContainerConfigMgr.cs` | `Tables.TbLootContainer` 查询包装 |
| `GameLogic/UI/LootContainerUI/LootContainerUI.cs` | 开箱面板（点击格子拿取 / Take All / Close；不暂停游戏） |
| `Assets/AssetRaw/UI/LootContainerUI/LootContainerUI.prefab` | 开箱面板 prefab（复用 ItemSlot 格子） |
| `Configs/GameConfig/Datas/lootcontainer.xlsx` | 掉落权重表数据源：`id / containerId / itemId / weight(相对权重) / minCount / maxCount` |

## 数据流

```
玩家进入 trigger → LootContainerSystem 显示 "Press E to Open"（复用 InteractionPromptUI）
按 E → 首开按权重抽 3 次生成内容 → ShowUIAsync<LootContainerUI>(container)
点击格子 / Take All → RunInventory.TryAdd（满则 OnInventoryFull，道具留在箱内）
拿空 → 容器变灰已开；撤离时随临时背包一起入仓库（既有 Portal 链路）
```

## 设计要点

- 交互范式完全复用传送门：trigger 进出 + `InteractionPromptUI` + `IBattleInputEvent.OnInteractPressed`（含玩家死亡闸）。
- 开箱面板**不暂停游戏**（TimeScaleWhenVisible=1），保留搜打撤的开箱风险。
- 关闭途径：Esc / 再按 E / Close 按钮 / 走出触发区。
- 已知限制：与 PortalSystem 共用交互事件，触发区重叠时两者都会响应（摆放错开；统一 IInteractable 仲裁器待做，见 docs/TODO.md）。
