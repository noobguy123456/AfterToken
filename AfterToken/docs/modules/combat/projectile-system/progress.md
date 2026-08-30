# Projectile System 进度

## 已完成
- [x] ProjectileEntity 飞行、命中、回收
- [x] ProjectileSystem 统一 Update
- [x] IProjectileEvent 事件
- [x] 对象池预加载与回收
- [x] 追踪弹、爆炸范围伤害、穿透计数支持
- [x] 运行时非分配迭代：字典直接遍历，避免每帧分配 List
- [x] 爆炸范围伤害改用非分配物理查询：静态 `Collider2D[]` + `ContactFilter2D` 替代 `OverlapCircleAll`
- [x] 2026-08-30 修复 Update 枚举期异常：爆炸命中在遍历 `_activeProjectiles` 途中销毁弹体导致 `InvalidOperationException: Collection was modified` 每帧抛（火箭开火后游戏卡死的根因），改为快照数组遍历 + 遍历时校验 `IsActive`/字典存续
- [x] 2026-08-30 火箭弹寿命终点引爆：`Tick` 寿命耗尽时若 `explosionRadius > 0` 先在终点 `ApplyExplosionDamage` 再销毁（直射火箭飞满射程不再无声消失）
- [x] 2026-08-30 爆炸特效 shader 化（按 docs/Proposal/combat/explosion-shader-proposal.md 实施，URP 修正为 Built-in）：新增 `Assets/AssetArt/Shaders/ExplosionFireball.shader`（顶点噪声位移 + FBM 侵蚀 + 黑体色带 亮黄→橙→暗红，球体片元裁剪 y<0 取上半球，不透明+clip 避免半透明 overdraw）与 `ExplosionShockwave.shader`（贴地面片圆环亮环 + 加色混合淡出）；新增 `ExplosionEffectDriver`（`Entity/Projectile/`，运行时构建网格、0.5s 自驱动 `_Progress`/膨胀曲线、自毁），`SpawnExplosionVisual` 改为一行 `Create`，删除 `_explosionVisuals` 占位 Tick 逻辑。坑位：冲击波必须 ease-out 到 1.5 倍半径领先火球轮廓，同心同速会被不透明火球完全遮住（俯视实测）。打包需将两个 shader 加入 Always Included Shaders（运行时 `Shader.Find`）

## 进行中
- [ ] 逻辑层与视觉层解耦（为弹幕扩展做准备）
  - [ ] 将 `ProjectileSystem` 中的 `_activeProjectiles` 从 `Dictionary` 改为 `List`
  - [ ] 移除 `_entityMap` 与内部 GameObject 池，逻辑层不再直接管理视觉实体
  - [ ] `IProjectileEvent` 增加 `ProjectileVisualType` 参数

## 待办
- [ ] 新增 `ProjectileVisualSystem`
  - [ ] 关键弹（火箭/榴弹/追踪弹）对象池与实体管理
  - [ ] 普通弹 Particle System 批量渲染
- [ ] `WeaponConfig` / Luban `TbWeapon` 增加 `projectileVisualType` 字段
- [ ] 全局子弹数量上限与分帧发射机制
- [ ] 空间划分（Spatial Hash / Grid）优化碰撞查询
- [ ] GPU Instancing 方案评估（超大规模弹幕）

## 参考文档

- 详细演进方案：`docs/Proposal/combat/bullet-logic-visual-separation.md`

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
