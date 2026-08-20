# Unlock System 进度

## 已完成
- [x] `TbUnlock` 配置表（unlock.xlsx + __beans__/__enums__/__tables__ 注册 + 两处 `_tableFiles` 白名单）：内容类型枚举 `UnlockContentType`（Level/Weapon）、玩家等级条件、通关关卡条件、金币价格（2026-08-20）
- [x] `UnlockSystem`：IsUnlocked（未配置默认开放）/ ConditionsMet / TryUnlock（消费金币+落存档+发事件）/ GetLockHint（英文提示）/ Reset
- [x] `IUnlockEvent.OnContentUnlocked` 事件接口
- [x] 存档段 `UnlockSaveData.unlockedIds`（旧存档兼容：字段缺失走默认值，无需版本迁移）
- [x] 消费方接入：`LobbyUI` 关卡按钮锁定置灰 + 条件提示；GM `unlock <id>` 命令

## 待办
- [ ] 武器解锁的战斗侧消费（武器获取/装备 gated，待武器强化或获取流程立项）
- [ ] 解锁 UI 面板（当前由 LobbyUI 按钮提示承担关卡解锁展示）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
