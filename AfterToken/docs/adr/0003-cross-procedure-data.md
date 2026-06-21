# 0003. 跨流程数据通过 BattleContext 传递（临时方案）

## 状态

已接受（临时）

## 背景

`ProcedureLobby` 需要把玩家选择的关卡 ID 传递给 `ProcedureBattle`。TEngine 的流程切换接口当前不支持向目标流程传入自定义参数。

约束：

- 不能修改 `GameApp` 的反射切换逻辑（当前任务要求忽略 GameApp 问题）。
- 数据传递方式必须简单、可快速替换。

## 决策

使用静态类 `BattleContext.CurrentLevelId` 作为临时跨流程数据通道。

- `LobbyUI` 在玩家选择关卡时写入 `BattleContext.CurrentLevelId`。
- `ProcedureBattle.OnEnter` 读取该值，找不到时回退到默认关卡 1。

替代方案：

- 在 `IProcedureModule` 上扩展带参数的状态切换。被拒绝，因为涉及 TEngine 框架接口修改。
- 使用 `RuntimeGameData` 单例。与当前方案本质相同，等后续玩家数据层完善后统一迁移。

## 后果

- 实现简单，满足当前流程。
- 全局可变状态难以测试，也不支持同时存在多个“待进入关卡”的场景。
- 后续接入玩家档案 / Luban 配置后，应迁移到正式的运行时数据层，并弃用本 ADR。
