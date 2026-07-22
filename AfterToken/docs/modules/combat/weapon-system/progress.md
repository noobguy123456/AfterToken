# Weapon System 进度

## 已完成
- [x] `WeaponSystem` 武器槽管理（3 槽位）
- [x] 开火、换弹、瞄准调度
- [x] 武器扩散、后坐力、移动速度系数
- [x] `AimAssistSystem` 辅助瞄准与火箭锁定
- [x] `WeaponInstance` 运行时实例
- [x] 狙击镜 `SniperScopeUI` RenderTexture 集成
- [x] 武器轮盘 `WeaponWheelUI`
- [x] 弹匣为空自动换弹
- [x] 换弹状态事件 `OnReloadStateChanged`
- [x] 切换武器中断当前武器换弹
- [x] `TbWeapon` 配置已接入（`WeaponConfigMgr` 从 Luban 表读取）
- [x] 硬编码 `WeaponConfigMgr` 已替换为 Luban 表驱动
- [x] 武器切换冷却从 `TbPlayer` 读取，不再硬编码
- [x] 辅助瞄准参数（半径/角度/锁定距离/角度/时间）从 `TbWeapon` 读取，不再硬编码

## 进行中
- [ ] 无

## 待办
- [ ] 武器切换动画
- [ ] 武器开火/换弹/切换音效（依赖 `audio-system`）
- [ ] 武器特殊效果（如激光指示、追踪导弹）
- [ ] 换弹过程可被冲刺/受击等动作打断/加速（视玩法需求）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
