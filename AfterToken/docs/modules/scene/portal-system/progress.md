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

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
