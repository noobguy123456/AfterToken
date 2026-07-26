# 经营总控

## 职责

协调模拟经营各子系统，管理经营场景生命周期与整体时间推进。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `SimulationSystem` | 经营总控，负责初始化/销毁子系统、广播经营事件 |
| `ProcedureSimulation` | 经营流程：加载场景、初始化系统、打开 UI、清理资源 |

## 设计要点

- `SimulationSystem` 负责初始化 `SimTimeSystem`、`BuildingSystem`、`ProductionSystem`、`OrderSystem`。
- 经营时间推进由 `SimTimeSystem` 驱动，其他系统监听 `ISimulationEvent.OnSimulationTimeAdvanced`。
- `ProcedureSimulation` 沿用 `GameplayProcedureBase` 的场景加载与 UI 流程。
- 离开经营场景时统一清理计时器、对象池、UI，并调用 `GamePauseManager.Reset()` 防止暂停状态泄漏。

## 本期 MVP 范围

- 实现 `ProcedureSimulation` 进入/离开流程。
- 实现 `SimulationSystem` 作为 `SimulationRoot` 上的总控组件。
- 农场系统（`FarmSystem`）与工人系统（`WorkerSystem`）本期不实现，详见 [`docs/Proposal/simulation/simulation-mvp.md`](../../../Proposal/simulation/simulation-mvp.md)。
