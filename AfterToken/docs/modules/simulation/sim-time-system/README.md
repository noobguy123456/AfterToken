# 经营时间系统

## 职责

推进模拟经营内的时间，驱动生产、作物生长、订单刷新等时间相关逻辑。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `SimTimeSystem` | 时间推进管理：当前时间、倍率、暂停/恢复、加速 |
| `ESimSpeed` | 时间倍率枚举：`Pause` / `Normal` / `Fast` / `Max` |

## 设计要点

- 支持正常、加速、暂停三种状态，倍率由 `TbSimTimeConfig` 配置。
- 时间推进触发 `ISimulationEvent.OnSimulationTimeAdvanced(float deltaTime, float totalTime)`。
- 速度切换时触发 `ISimulationEvent.OnSimulationSpeedChanged(ESimSpeed speed)`。
- 所有时间相关系统（`BuildingSystem`、`ProductionSystem`、`OrderSystem`）通过事件监听推进。

## 配置依赖

- `TbSimTimeConfig`（新增）：`baseSpeed`、`fastSpeed`、`maxSpeed`、`orderRefreshInterval`、`maxOrderCount`。

## 本期 MVP 范围

- 实现 `SimTimeSystem` 基础推进与暂停/加速切换。
- 昼夜、季节、作物生长相关时间刻度本期不实现。
- 详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
