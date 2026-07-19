# 传送门系统（Portal System）

> 所属模块：场景模块  
> 设计文档：[docs/portal-system-design.md](../../../portal-system-design.md)  
> 本文件为具体实现文档，开发过程中持续更新。

---

## 职责

- 在场景中提供可交互的传送门实体。
- 支持通过配置表定义传送门类型、目标场景/关卡、出现条件、玩家状态保留等规则。
- 统一管理传送门的激活、交互提示、转场效果与场景切换。

## 核心类与文件

| 类/文件 | 说明 |
|---------|------|
| `PortalEntity.cs` | 场景中的传送门实体，挂载在 Portal Prefab 上 |
| `PortalSystem.cs` | 扫描并管理所有 PortalEntity，监听全局事件评估出现条件 |
| `PortalConfig.cs` | Luban `cfg.Portal` 的运行时适配，含类型与条件常量 |
| `PortalConfigMgr.cs` | Portal 配置管理器 |
| `IPortalEvent.cs` | 传送门事件接口 |
| `IPortalCondition.cs` | 出现条件扩展接口 |
| `NoneCondition.cs` | 无条件激活 |
| `AllEnemiesDefeatedCondition.cs` | 所有敌人被击败后激活 |
| `PlayerStateContext.cs`（类名 `PortalPlayerState`） | 传送门玩家状态快照 |
| `PortalTransitionMgr.cs` | 转场效果管理 |
| `InteractionPromptUI.cs` | 靠近传送门时的交互提示 UI |
| `TransitionUI.cs` | 全屏灰色转场遮罩 UI |

## 配置表

- 源表：`Configs/GameConfig/Datas/portal.xlsx`
- Bean 定义：`Configs/GameConfig/Datas/__beans__.xlsx` 中 `cfg.Portal`
- 生成代码：`Assets/GameScripts/HotFix/GameProto/GameConfig/cfg/Portal.cs` 等
- 生成数据：`Assets/AssetRaw/Configs/json/cfg_tbportal.json`

## 输入

- 交互键：`E`
- 已扩展 `InputSystem` 与 `IBattleInputEvent.OnInteractPressed()`。

## 死亡与传送的判定

- **死亡优先（第一道闸）**：`PortalSystem.OnInteractPressed` 在响应交互前检查 `PlayerSystem.Instance?.GetPlayerEntity()?.IsDead`，玩家已死亡时直接忽略交互——死亡后必须走死亡确认（Restart / Back to Lobby），不允许通过传送门绕过。
- **转场中止（第二道闸）**：`PortalSystem.ExecuteTransition` 入口同样先查死亡；`PortalTransitionMgr.PlayAsync` 在转场渐暗完成后、执行场景切换前再次判定 `IsPlayerDead`——若玩家在转场期间死亡，则渐出、关闭 TransitionUI 并 `PortalPlayerState.Clear()` 清理已保存状态，放弃本次传送（覆盖「活着按 E、转场途中死亡」的竞态）。
- **时间状态兜底**：`GameApp.ChangeProcedure<T>()` 在每次流程切换时调用 `GamePauseManager.Reset()`，清空全部 `timeScale` 请求并恢复 `Time.timeScale = 1`，防止死亡暂停（`PushTimeScale(0)`）等状态跨流程泄漏。
- 背景：曾出现「死亡瞬间按 E 触发传送 → 新场景 timeScale=0 冻结、无法操作」的 bug（2026-07-19 修复）；随后补「死亡后仍能触发传送」的判定漏洞（同日修复）。

## 快速使用

1. 在 `portal.xlsx` 中新增一行配置。
2. 在场景中创建空物体，挂载 `PortalEntity` 脚本，填入 `ConfigId`。
3. 添加 `CircleCollider2D`（Trigger）作为触发区。
4. 添加可视化占位体（运行时会自动创建 SpriteRenderer 子节点）。
5. 进入场景后 `PortalSystem` 会自动扫描并初始化。

## 已知问题与后续计划

- 当前 `custom_scene` 类型复用 `ProcedureBattle` 加载指定场景。
- 占位 Portal Prefab 和 UI 后续替换为正式美术资源。
- 交互提示 UI 的并发管理后续优化。
- 出现条件后续扩展 `BossDefeatedCondition`、`ItemRequiredCondition` 等。

## 实现状态

详见 [progress.md](./progress.md)。
