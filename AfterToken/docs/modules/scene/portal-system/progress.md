# 传送门系统开发进度

## 当前状态

✅ 基础版完成，死亡判定防护已加固

## 已完成
- [x] 需求确认与设计方案
- [x] 设计文档整理：`docs/portal-system-design.md`
- [x] 实现文档初始化：`docs/modules/scene/portal-system/README.md`
- [x] 新增 Luban 配置表 `portal.xlsx` 并注册到 `__tables__.xlsx` / `__beans__.xlsx`
- [x] 运行 Luban 生成脚本，生成 `cfg.Portal` / `cfg.TbPortal`
- [x] 新增 `PortalConfig` / `PortalConfigMgr`
- [x] 新增 `IPortalEvent`
- [x] 扩展 `InputSystem` 与 `IBattleInputEvent`（`Interact` 键 `E`）
- [x] 新增 `IPortalCondition` 及内置条件（`None`、`AllEnemiesDefeated`）
- [x] 新增 `PortalPlayerState`（传送门触发时 HP/体力/武器状态保留）
- [x] 新增 `TransitionUI` / `InteractionPromptUI`
- [x] 新增 `PortalEntity` / `PortalSystem` / `PortalTransitionMgr`
- [x] `ProcedureBattle` 集成 `PortalSystem`，支持自定义场景名
- [x] 创建占位 Portal Prefab：`Assets/AssetRaw/Actor/Portal_Placeholder.prefab`
- [x] 在 `BattleScene` 中摆放测试 Portal（ConfigId=1001，返回大厅）
- [x] 编译通过
- [x] 死亡判定：玩家死亡后禁止与传送门交互，已死亡玩家无法被传送
- [x] 跨场景状态保留：胜利时临时背包转入仓库

## 待完成
- [ ] Play Mode 手动验证：靠近 Portal → 显示提示 → 按 E → 转场 → 返回大厅
- [ ] 验证 `portal_next_level` 与 `portal_custom_scene` 类型
- [ ] 验证 `keepPlayerState=true` 时 HP 与武器弹药跨场景保留
- [ ] 后续替换占位美术资源
- [ ] 后续扩展 `BossDefeatedCondition` / `ItemRequiredCondition`

## 阻塞/依赖
- 玩家状态保留依赖 `PlayerSystem` / `WeaponSystem` 当前已实现的状态字段。
- 物品/背包条件待共享层背包系统完成后扩展。

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-06-30 | 确认需求，整理设计文档与实现文档 |
| 2026-07-07 | 完成 Portal System 基础版：配置表、核心逻辑、UI、转场、场景摆放、编译通过；将跨场景状态类重命名为 PortalPlayerState 并归入 Portal 命名空间 |
| 2026-07-08 | 修复 `TransitionUI` / `InteractionPromptUI` 的 YooAsset location 错误：将 Prefab 从 `Assets/Resources/` 移回 `Assets/AssetRaw/UI/<Name>/<Name>.prefab`，`[Window]` 改回标准 location 加载；Editor Simulate Build 清单已确认包含两个地址；待 Play Mode 最终验证 |
| 2026-07-19 | 加固死亡判定：玩家死亡后禁止触发传送门，避免带着 `timeScale=0` 的暂停状态进入新场景 |
| 2026-08-20 | 职责重划（用户拍板）：传送门不再快照玩家属性——`PortalPlayerState` 瘦身为场景上下文（TargetLevelId/TargetSceneName/CarryPlayerState，`RecordTransition` 在转场时记录）；玩家血量/体力/武器弹药改由新建的 `PlayerAttrStore`（`Procedure/PlayerAttrStore.cs`）承担，订阅 `IPlayerEvent.OnHpChanged/OnStaminaChanged/OnAmmoChanged` 与 `IWeaponEvent.OnWeaponEquipped/OnWeaponSwitched` **变动即存**；`PlayerSystem`/`WeaponSystem` 恢复路径改读 store（恢复前先把 store 值拷到本地，避免初始化广播/装备广播覆盖待恢复值；`SetWeapon` 公开写口修正恢复中间值）；一局结束清 store 与转场记录：ProcedureLobby 进入时 + PlayerDeathHandler 死亡时 |
| 2026-08-20 | B 方案落地：`RETURN_TO_LOBBY` 改名 `RETURN_BASE`，portal.xlsx 1001 同步（prompt "Press E to return to base"）；撤离分支转仓库+发奖励后回 `ProcedureSimulation`（基地），两处错误兜底也改回基地；`PortalEntity.GetDestinationText()` 显示 "Base"；PlayerDeathHandler.ReturnToBase / PlayerDeathUI "Back to Base"。大厅概念废弃，选关挪进基地 |
| 2026-08-20 | 修复传送门完全无法交互的根因：`Portal_Placeholder.prefab` 挂的是 **CircleCollider2D**（2D 物理），2D 转 3D 后玩家是 3D Rigidbody+CapsuleCollider，`OnTriggerEnter(Collider)` 永不触发——prefab 改挂 SphereCollider（trigger，r=1.5），`PortalEntity.Awake` 增加运行时兜底（缺 SphereCollider 自动补，热更类型 RequireComponent 不可靠）。另修复 `ExecuteTransition` 的 SELECT_LEVEL 分支误放在 IsPlayerDead 之后（基地无玩家实体恒拦截），已前移到死亡判定之前。Play Mode 全链路验证通过：进入触发区 inside=True → 交互开 LobbyUI → Back 关闭 |
| 2026-08-20 | 新增选关传送门类型 `portal_select_level`（portal.xlsx 2001，prompt "Press E to deploy"）：交互时不切场景、无转场，直接打开 LobbyUI 选关窗口（`PortalSystem.ExecuteTransition` 前置分支，绕过死亡判定与 PortalPlayerState 记录；`OnInteractPressed` 对选关门放行——基地无战斗玩家实体，原死亡判定恒拦截）；`PortalEntity.GetDestinationText()` 显示 "Deploy"；经营场景接入：`ProcedureSimulation` 在 SimulationRoot 挂 `PortalSystem`，`SimulationInputSystem` 新增 E 键发布 `IBattleInputEvent.OnInteractPressed`；SimulationScene 摆放 Portal_Deploy（2001，(5,0,5)）。同批修复：ESC 关闭链补 LobbyUI（之前 ESC 只弹设置面板把它压在下层，表现为"关不掉"） |
| 2026-08-20 | 101 场景补放传送门 + 3D 关卡链配置：portal.xlsx 新增 1101（next level→102，全灭激活，保留玩家状态）/ 1102（→103）；`BattleScene_3D_L01` 摆放 Portal_Next_102（1101，(0,0,-5)）与 Portal_ReturnLobby（1001，(-6,0,-3)，沿用 prefab 默认 configId）。注意 3D_L02/L03 场景仍未放传送门，链到 102 后需用 GM 或补放 |

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
