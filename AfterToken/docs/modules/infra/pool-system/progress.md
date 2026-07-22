# Pool System 进度

## 已完成
- [x] 通用 `GameObjectPool` 实现
- [x] `PoolSystem` 统一入口（`Get`/`Recycle`/`Clear`/`ClearAll`/`Preload`）
- [x] TEngine `MemoryPool` / `ObjectPoolModule` 接入
- [x] `ProjectileSystem` 内部已使用对象池复用 GameObject
- [x] `EnemySpawnSystem` 已接入 `PoolSystem`：按 Prefab 地址分池、预加载、死亡回收

## 进行中
- [ ] 按类型拆分 `ProjectilePool`、`EnemyPool`、`EffectPool`、`DamageTextPool`
- [ ] 池容量自动扩缩容策略
- [ ] 对象池运行时统计与调试面板数据对接

## 待办
- [ ] 场景切换/战斗重启时清理敌人对象池，避免旧敌人残留

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
