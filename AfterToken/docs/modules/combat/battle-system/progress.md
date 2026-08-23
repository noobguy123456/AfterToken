# Battle System 进度

## 已完成
- [x] 伤害计算与死亡判定
- [x] `IBattleEvent` 事件
- [x] 命中反馈触发（伤害飘字、受击指示、命中标记）
- [x] 敌人死亡掉落触发（`DropSystem` 监听死亡事件）
- [x] 命中反馈按攻击者过滤（2026-08-22）：`OnEntityDamaged` 中命中标记（`OnHitTarget`）与伤害飘字仅在攻击者为玩家时触发（此前敌人打玩家也会点亮狙击镜 X 标记，表现为"瞄准即出现"）；顺带将写死的 `false` 改为透传 `DamageInfo.IsCritical`。Play 验证：敌人攻击玩家无标记，玩家命中出白色 X

## 进行中
- [ ] 暴击、闪避计算
- [ ] Buff/Debuff 系统（`TbBuff` 数据已存在，业务未接入）
- [ ] 战斗结果 `IBattleResultEvent`

## 待办
- [ ] 战斗结算数据（击杀数、耗时、奖励）
- [ ] 战斗暂停/继续
- [ ] 战斗内统计面板

## 阻塞
- 等待 `IBattleResultEvent` / `ILevelEvent` 事件接口补齐；等待 `TbBuff` / `TbWave` 表接入。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
