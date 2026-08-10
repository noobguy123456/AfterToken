# Player Profile System 进度

## 已完成
- [x] `PlayerProfileSystem` 等级/经验（升级公式 level*100，内存态起于经营系统需求）
- [x] `IPlayerProfileEvent` 事件接口（事件调用已加 `?.` 防空引用）
- [x] 与 `SaveSystem` 持久化对接（2026-08-06，变动即存，懒加载恢复）

## 待办
- [ ] 解锁记录（武器/关卡解锁）
- [ ] 经验升级表配置化（当前硬编码 level*100）
- [ ] 与 `UnlockSystem` 解锁判定对接

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
