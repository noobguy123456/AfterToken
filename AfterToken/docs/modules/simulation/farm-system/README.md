# 农场系统

## 职责

管理作物的种植、生长、收获与季节影响。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `FarmSystem` | 农场管理入口 |
| `CropEntity` | 作物表现实体 |

## 设计要点

- 作物配置使用 Luban `TbCrop`。
- 生长时间受 `SimTimeSystem` 影响。
- 作物生长与季节、土壤肥力等机制后续再扩展。

## 本期 MVP 范围

- **本期不做。** 农场系统在 MVP 最小闭环（建造 → 生产 → 订单）稳定后再投入。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
