# Event System 进度

## 已完成
- [x] 战斗事件接口：`IBattleInputEvent`、`IPlayerEvent`、`IProjectileEvent`、`IBattleEvent`、`IEnemyEvent`
- [x] 武器/相机/命中反馈事件：`IWeaponEvent`、`ICameraEvent`、`IHitFeedbackEvent`
- [x] 道具事件：`IItemEvent`
- [x] 热更事件分组与 `GameEvent` 监听模式

## 进行中
- [ ] 补充 `ILevelEvent`
- [ ] 补充 `IBattleResultEvent`

## 待办
- [ ] 补充共享层事件：`IPlayerProfileEvent`、`ICurrencyEvent`、`IInventoryEvent`、`IUnlockEvent`
- [ ] 补充经营事件：`ISimulationEvent`
- [ ] 补充特效/音频事件：`IEffectEvent`、`IAudioEvent`

## 阻塞
- `ILevelEvent` / `IBattleResultEvent` 阻塞关卡胜负与战斗结算；共享/经营事件阻塞 M3/M4 阶段系统实现。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
