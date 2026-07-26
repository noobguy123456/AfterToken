# 经营总控进度

## 已完成
- [x] `SimulationSystem` 经营总控
- [x] `ProcedureSimulation` 经营流程
- [x] 经营场景加载与清理

## 实现说明
1. `ProcedureSimulation` 加载 `LobbyScene` 作为经营场景背景，初始化 `SimulationSystem`，打开 `SimulationMainUI`。
2. `SimulationSystem` 在 `SimulationRoot` GameObject 上挂载 `SimTimeSystem`、`BuildingSystem`、`ProductionSystem`、`OrderSystem`。
3. 离开经营场景时调用 `SimulationSystem.Leave()` 清理子系统数据。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
