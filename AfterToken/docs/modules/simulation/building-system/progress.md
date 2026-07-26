# 建筑系统进度

## 已完成
- [x] 建筑建造、升级、拆除
- [x] `BuildingEntity` 与配置（本期简化为 UI 列表形式）
- [x] `BuildingInstance` 运行时数据管理

## 实现说明
1. `TbBuilding` 配置建筑消耗、耗时、队列槽位、解锁等级。
2. `BuildingSystem.TryBuild` / `TryUpgrade` 消耗 `CurrencySystem` 金币与 `InventorySystem` 材料。
3. 监听 `ISimulationEvent.OnSimulationTimeAdvanced` 推进建造/升级进度，完成后广播事件。
4. 拆除功能已实现（`TryDemolish`），本期未在 UI 中暴露。
5. 建筑场景实体摆放本期未实现，后续可扩展 `BuildingEntity`。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
