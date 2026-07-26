# 订单系统

## 职责

管理订单的生成、交付、刷新与奖励发放。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `OrderSystem` | 订单管理入口：生成、交付、刷新、奖励 |
| `OrderInstance` | 运行时订单实例数据 |

## 设计要点

- 订单配置使用 Luban `TbOrder`。
- 交付时校验 `InventorySystem` 库存，发放 `CurrencySystem` 金币与 `InventorySystem` 物品奖励。
- 订单按 `TbSimTimeConfig.orderRefreshInterval` 定时刷新，数量上限由 `maxOrderCount` 控制。
- 完成订单后触发 `ISimulationEvent.OnOrderCompleted`。

## 配置依赖

- `TbOrder`（新增）：订单需求、奖励、权重、时限、等级限制。
- `TbSimTimeConfig`（新增）：订单刷新间隔与订单板上限。
- 共享系统：`InventorySystem`（校验/扣除需求物品、发放奖励物品）、`CurrencySystem`（发放金币奖励）。

## 本期 MVP 范围

- 实现 `OrderSystem` 的定时生成与交付逻辑。
- 订单倒计时、超时惩罚、订单品质分级本期不实现。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
