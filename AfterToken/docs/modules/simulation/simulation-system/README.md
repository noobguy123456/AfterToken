# 经营总控

## 职责

协调模拟经营各子系统，管理经营场景生命周期与整体时间推进。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `SimulationSystem` | 经营总控 |
| `ProcedureSimulation` | 经营流程 |

## 设计要点

- `SimulationSystem` 负责初始化建筑、生产、工人、订单等子系统。
- 经营时间推进由 `SimTimeSystem` 驱动。
