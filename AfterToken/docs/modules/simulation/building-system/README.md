# 建筑系统

## 职责

管理模拟经营中的建筑建造、升级、拆除与功能解锁。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `BuildingSystem` | 建筑管理入口：建造、升级、拆除、状态推进 |
| `BuildingEntity` | 建筑表现实体（本期可简化，场景摆放后续再补） |
| `BuildingInstance` | 运行时建筑实例数据 |

## 设计要点

- 建筑配置使用 Luban `TbBuilding`。
- 建造/升级消耗货币与材料，完成后触发 `ISimulationEvent.OnBuildingCompleted` / `OnBuildingUpgraded`。
- 建筑拥有生产队列槽位，槽位数量由配置 `productionSlotCount` 决定。
- 建筑状态：`Idle` / `Building` / `Upgrading`，监听 `OnSimulationTimeAdvanced` 推进进度。

## 配置依赖

- `TbBuilding`（新增）：建筑属性、消耗、耗时、队列槽位。
- `TbProduction`（新增）：可使用的建筑通过 `buildingId` 关联。
- 共享系统：`CurrencySystem`（消耗金币）、`InventorySystem`（消耗材料）。

## 本期 MVP 范围

- 实现 `BuildingSystem` 的建造与升级逻辑。
- 拆除功能、建筑场景实体摆放、建筑解锁链本期不实现。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
