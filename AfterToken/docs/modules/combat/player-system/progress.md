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
- [x] 迁移 XZ 平面玩法：Rigidbody 3D 化（锁 Y 与 X/Z 旋转）、CapsuleCollider、贴图移至 Visual 子节点（X+90°）、`BattleBoundary` 地面边界钳制
- [x] `PlayerEntity` 移速/闪速/闪避时长不再在代码中写死，统一由 `PlayerSystem` 从 `TbPlayer` 应用
- [x] Play Mode 基础状态验证（Idle / Move / Dodge / Reload / Dead 已初步确认）
- [x] 玩家状态收敛（2026-08-08）：IsDead/IsDodging/MoveInput/AimInput/IsAiming 唯一 owner 归 `PlayerStateContext`，`PlayerEntity`/`WeaponSystem` 重复字段改转发属性，零行为变化，详见 README「状态归属」
- [x] 玩家 3D 化占位（2026-08-27）：`Player.prefab` 的 Visual 由平躺 Sprite 改为 3D 胶囊（材质 `M_Player_Placeholder`，青色，0.6x1.8m 与 NPC 一致）；移除 `ProcedureSimulation` 遗留的 Visual ×5 放大（0.2m 占位圆点时代的兜底，胶囊化后导致经营场景角色 10m 高）；新增 `WeaponMountView` 武器挂载（详见 weapon-system progress）

## 进行中
- [ ] 后续新状态（瞄准、交互、受击硬直等）的扩展与调试

## 待办
- [ ] 玩家成长系统（等级、经验、属性成长）
- [ ] 装备系统
- [ ] 技能槽系统

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
