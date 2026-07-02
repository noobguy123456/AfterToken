# Player System 进度

## 已完成
- [x] `PlayerEntity` 表现层、碰撞、HP、受伤、死亡
- [x] 玩家 FSM：Idle / Move / Reload / Dodge / Dead
- [x] 玩家 FSM 重构：黑板 `PlayerStateContext` + 拦截器 `PlayerStateMachineDriver`
- [x] 全局拦截器：Death / Dodge / ReloadStart
- [x] 玩家状态事件 `IPlayerEvent`
- [x] 移动、闪避、换弹状态切换与动画触发
- [x] 体力系统：恢复、闪避消耗、`CanDodge` 黑板标志
- [x] 战斗 HUD 血条与体力条（`BattleMainUI`）
- [x] `TbPlayer` 配置表接入（血量/体力/移速/闪避等属性）

## 进行中
- [x] Play Mode 基础状态验证（Idle / Move / Dodge / Reload / Dead 已初步确认）
- [ ] 接入 Luban `TbPlayer` / `TbPlayerAttr` 配置
- [ ] 动画配置从硬编码切换到配置表驱动
- [ ] 后续新状态（瞄准、交互、受击硬直等）的扩展与调试

## 待办
- [ ] 玩家成长系统（等级、经验、属性成长）
- [ ] 装备系统
- [ ] 技能槽系统

## 阻塞
- 等待 `TbPlayer` / `TbPlayerAttr` 表数据补充。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
