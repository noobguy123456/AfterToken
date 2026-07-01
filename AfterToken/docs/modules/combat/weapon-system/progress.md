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

## 进行中
- [ ] 接入 Luban `TbWeapon` 配置
- [ ] 替换硬编码 `WeaponConfigMgr`

## 待办
- [ ] 武器切换动画
- [ ] 武器开火/换弹/切换音效
- [ ] 武器特殊效果（如激光指示、追踪导弹）
- [ ] 换弹过程可被冲刺/受击等动作打断/加速（视玩法需求）

## 阻塞
- 等待 `TbWeapon` 表数据补充与 `WeaponConfigMgr` 替换完成。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
