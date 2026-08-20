# Cross Play Link 进度

## 已完成
- [x] 战斗撤离 → 货币/经验（2026-08-20）：`CrossPlayLink.OnBattleExtracted` 挂接 `PortalSystem` RETURN_TO_LOBBY 分支；奖励数值走 `TbLevel.rewardGold/rewardExp`（level.xlsx 已加列，`LevelConfig` 适配器已透传）
- [x] 通关记录 → 关卡链解锁：撤离时 `PlayerProfileSystem.MarkLevelCompleted`，`UnlockSystem` 的 `requireCompleteLevelId` 条件据此判定，`LobbyUI` 锁定展示

## 待办
- [ ] 经营产出 → 武器强化/角色训练（待强化/训练系统立项）
- [ ] 与正式 `RewardSystem`（TbDrop 分发 + 结算 UI）合并奖励入口

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
