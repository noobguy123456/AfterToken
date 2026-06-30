# Player System 进度

## 已完成
- [x] `PlayerEntity` 表现层、碰撞、HP、受伤、死亡
- [x] 玩家 FSM：Idle / Move / Reload / Dodge / Dead
- [x] 玩家状态事件 `IPlayerEvent`
- [x] 移动、闪避、换弹状态切换与动画触发

## 进行中
- [ ] 接入 Luban `TbPlayer` / `TbPlayerAttr` 配置
- [ ] 动画配置从硬编码切换到配置表驱动

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
