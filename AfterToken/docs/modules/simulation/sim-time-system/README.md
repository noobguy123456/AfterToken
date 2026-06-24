# 经营时间系统

## 职责

推进模拟经营内的时间，驱动生产、作物生长、订单刷新等时间相关逻辑。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `SimTimeSystem` | 时间推进管理 |

## 设计要点

- 支持正常、加速、暂停三种状态。
- 时间推进触发 `ISimulationEvent.OnSimulationTimeAdvanced`。
