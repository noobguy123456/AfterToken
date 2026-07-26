# 生产系统进度

## 已完成
- [x] 生产队列管理
- [x] 产出结算
- [x] 配方与建筑等级校验

## 实现说明
1. `TbProduction` 配置配方输入/输出/耗时/适用建筑与等级。
2. `ProductionSystem.TryStartProduction` 校验建筑类型、等级、材料并占用槽位。
3. 监听时间推进，生产完成后产物入 `InventorySystem` 并广播 `OnProductionFinished`。
4. 每个建筑实例拥有固定生产槽位，槽位占用/释放由 `BuildingSystem` 管理。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
