# Ballistic System 进度

## 已完成
- [x] Raycast / Projectile 弹道分发
- [x] 射线参数配置：radius、hitLayers、debug 可视化
- [x] 命中/未命中 Debug.DrawRay

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-08-30 | 火箭筒直射化：`FireProjectile` 删除锁定追踪分支（不再查 `AimAssistSystem.GetLockedTarget`），一律 `CreateProjectile` 直射；`UpdateRocketLaser` 重构——火箭持枪时激光常开（无需右键瞄准），起点改从 `WeaponMountView.GetMuzzleWorldPos()` 枪口射出（原角色中心+0.5m），方向取准星水平方向，长度=武器 maxRange（兜底 20m），单一暗红配色（删除锁定态亮红） |

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`