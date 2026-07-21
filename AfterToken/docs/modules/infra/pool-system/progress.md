# Pool System 进度

## 已完成
- [x] 通用 `GameObjectPool` 实现
- [x] `PoolSystem` 统一入口（`Get`/`Recycle`/`Clear`/`ClearAll`）
- [x] TEngine `MemoryPool` / `ObjectPoolModule` 接入
- [x] `ProjectileSystem` 内部已使用对象池复用 GameObject

## 进行中
- [ ] 按类型拆分 `ProjectilePool`、`EnemyPool`、`EffectPool`、`DamageTextPool`
- [ ] `Preload` / `ClearAll` 接口完善

## 待办
- [ ] 池容量自动扩缩容策略
- [ ] 对象池运行时统计与调试面板数据对接

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
