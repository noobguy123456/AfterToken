# 掉落与拾取系统（Pickup System）

> 所属模块：战斗系统
> 关联：道具系统 `../../shared/item-system/`、背包系统 `../../shared/inventory-system/`、敌人系统 `../enemy-system/`

## 职责

- 敌人死亡时按掉落表掷点，在死亡位置生成世界掉落物。
- 玩家触碰掉落物后拾取进关卡临时背包。

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `GameLogic/System/DropSystem.cs` | 监听 `IEnemyEvent.OnEnemyDied`，按 `cfg.Drop` 掷点生成掉落物；由 `ProcedureBattle` 挂载到 BattleRoot |
| `GameLogic/Entity/Pickup/PickupEntity.cs` | 世界掉落物实体。代码自建占位视觉（占位 sprite + 稀有度染色），trigger 碰撞，玩家触碰即拾取 |
| `GameLogic/Config/DropConfigMgr.cs` | `Tables.TbDrop` 查询包装 |

## 数据流

```
敌人死亡 (IEnemyEvent.OnEnemyDied)
  → DropSystem：EnemyRegistry 取 configId/位置（事件触发时实体尚未销毁）
  → DropConfigMgr.GetDropsForEnemy：逐条掷点 Random[0,10000) < dropRate
  → 数量 Random[minCount, maxCount]
  → PickupEntity.Spawn(itemId, count, 位置+0.5m 内随机偏移)
玩家触碰 PickupEntity
  → RunInventory.TryAdd：成功 → 销毁掉落物 + OnItemPickedUp；失败（满）→ 留在地上 + OnInventoryFull
```

## 设计要点

- 不改动 `EnemyDeadState` 与事件接口签名：掉落仅消费现有 `OnEnemyDied(enemyId)` 事件 + 注册表查询，对现有监听零影响。
- 掉落物无需 Prefab（仿 `PortalEntity` 代码自建视觉），接入美术后改为配置化 Prefab。
- 掉落物视觉颜色 = 道具稀有度颜色（`RarityColors`），与 UI 稀有度框一致。
