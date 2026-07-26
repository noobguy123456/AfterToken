# 工人系统

## 职责

管理工人的分配、工作与属性成长。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `WorkerSystem` | 工人管理入口 |
| `WorkerEntity` / `WorkerData` | 工人表现与数据 |

## 设计要点

- 工人可分配到建筑、农场、生产队列。
- 工人属性影响生产效率与建筑建造速度。
- 工人数量上限、招募成本、成长曲线后续再扩展。

## 本期 MVP 范围

- **本期不做。** 工人系统在 MVP 最小闭环（建造 → 生产 → 订单）稳定后再投入。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
