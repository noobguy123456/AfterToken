# Projectile System 进度

## 已完成
- [x] ProjectileEntity 飞行、命中、回收
- [x] ProjectileSystem 统一 Update
- [x] IProjectileEvent 事件
- [x] 对象池预加载与回收
- [x] 追踪弹、爆炸范围伤害、穿透计数支持
- [x] 运行时非分配迭代：字典直接遍历，避免每帧分配 List

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
