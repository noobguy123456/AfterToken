# 解锁系统

## 职责

管理游戏内容的解锁条件与状态，连接战斗、经营和玩家成长。

## 实现（2026-08-20）

| 类/文件 | 说明 |
|---|---|
| `Shared/UnlockSystem.cs` | 解锁判定（`IsUnlocked`）/ 付费解锁（`TryUnlock`）/ 锁定提示（`GetLockHint`） |
| `IEvent/IUnlockEvent.cs` | `OnContentUnlocked(unlockId)` 事件 |
| `Configs/GameConfig/Datas/unlock.xlsx` | `TbUnlock`：id / contentType(UnlockContentType: Level/Weapon) / targetId / requirePlayerLevel / requireCompleteLevelId / costGold / comment |
| `SaveSystem` unlock 段 | 付费解锁记录 `unlockedIds`（变动即存） |

## 解锁规则

- `TbUnlock` 中**没有配置的内容默认开放**，不影响既有内容；
- 免费项（`costGold=0`）满足条件即视为已解锁，不落存档；
- 付费项需 `TryUnlock` 消费金币后写入存档；
- 条件类型：玩家等级（`requirePlayerLevel`）+ 通关关卡（`requireCompleteLevelId`，依赖 `PlayerProfileSystem` 通关记录）。

## 消费方

- `LobbyUI`：关卡按钮锁定置灰 + 显示英文解锁条件（如 `Stage 2 [Clear Stage 1]`）；
- GM：`unlock <id>` 命令测试付费解锁。
- 武器（Weapon）类型已在配置层支持，战斗侧武器获取 gated 待武器强化/获取流程立项后接入。

## 新增解锁内容配置流程

1. `unlock.xlsx` 加一行（内容类型/目标 ID/条件/价格）；
2. 跑 `gen_code_bin_to_project.bat`（或 Luban 直跑命令）；
3. 消费方调 `UnlockSystem.IsUnlocked(contentType, targetId)` 判定。
