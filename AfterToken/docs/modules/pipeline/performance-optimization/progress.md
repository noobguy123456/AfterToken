# Performance Optimization 进度

## 已完成
- [x] A* 寻路 `PathResult` 池化，减少每 path 的 `List<Vector2>` 分配
- [x] A* 路径平滑改为原地优化，避免额外 List 创建
- [x] `NavigationSystem` 缓存失效时释放 `PathResult`，避免占用
- [x] `EnemyChaseState` 路径刷新间隔通过 `TbEnemy.pathRefreshInterval` 配置，并按玩家距离动态缩放
- [x] 敌人接入 `PoolSystem` 预加载/回池，`EnemyEntity` 复用时自动恢复物理状态
- [x] `ProjectileSystem` 爆炸范围伤害改用非分配物理查询
- [x] `PoolSystem` 新增 `Preload` 接口
- [x] 建立 `docs/modules/pipeline/performance-optimization/` 模块文档

## 进行中
- [ ] 默认画质等级调整（从 Ultra 降至 Medium/Low）
- [ ] 血条 Draw Call 优化方案评估与实施
- [ ] 构建/编辑器迭代速度优化（Mono 开发构建、Enter Play Mode Options）

## 待办
- [ ] 集成运行时 Profiler 数据面板，持续监控内存与 GC
- [ ] 敌人数量大时的 A* 分帧/FlowField/动态网格更新
- [ ] 包体清理：移除未使用的重型 Package
- [ ] 真机低端机型性能基线测试

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
