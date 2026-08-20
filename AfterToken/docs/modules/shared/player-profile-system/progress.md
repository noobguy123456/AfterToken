# Player Profile System 进度

## 已完成
- [x] `PlayerProfileSystem` 等级/经验（升级公式 level*100，内存态起于经营系统需求）
- [x] `IPlayerProfileEvent` 事件接口（事件调用已加 `?.` 防空引用）
- [x] 与 `SaveSystem` 持久化对接（2026-08-06，变动即存，懒加载恢复）
- [x] 经验升级表配置化（2026-08-20，`TbPlayerLevel` playerlevel.xlsx，缺配置回退 level*100）
- [x] 通关记录（2026-08-20，`MarkLevelCompleted/IsLevelCompleted`，存档 profile 段 `completedLevels`）
- [x] 与 `UnlockSystem` 解锁判定对接（2026-08-20，等级条件 + 通关链条件）

## 待办
- [ ] 解锁记录（武器/关卡解锁）——已由 `UnlockSystem` 承担存档，本模块不再重复

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
