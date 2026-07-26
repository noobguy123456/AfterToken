# 生产系统

## 职责

管理生产队列、制造配方与产出结算。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `ProductionSystem` | 生产管理入口：配方校验、队列管理、产出结算 |
| `ProductionInstance` | 运行时生产实例数据 |

## 设计要点

- 生产配方使用 Luban `TbProduction`。
- 产出完成后更新 `InventorySystem` 并触发 `ISimulationEvent.OnProductionFinished`。
- 每个建筑实例拥有固定数量的生产槽位，生产占用槽位，完成后释放。
- 生产时间受 `SimTimeSystem` 推进的 `deltaTime * speed` 影响。

## 配置依赖

- `TbProduction`（新增）：配方输入、输出、耗时、适用建筑与等级。
- `TbBuilding`（新增）：建筑生产槽位数量。
- 共享系统：`InventorySystem`（消耗材料、产出物品）。

## 本期 MVP 范围

- 实现 `ProductionSystem` 的单队列生产逻辑：选择配方 → 校验材料 → 占用槽位 → 推进时间 → 产出入仓库。
- 多建筑、多配方、工人加速本期不实现。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
