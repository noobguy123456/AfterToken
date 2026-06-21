# 0004. 热更域接管主包流程管理器

## 状态

已接受（待框架化）

## 背景

主包流程负责启动器与资源更新（`ProcedureLaunch → ... → ProcedureLoadAssembly`）。当热更 DLL 加载完成后，需要进入热更域定义的游戏流程（主菜单、大厅、战斗）。

约束：

- 主包流程不可热更，不能包含游戏业务逻辑。
- 热更代码需要访问 `GameModule.Procedure` 注册自己的流程。
- TEngine 的 `IProcedureModule` 没有暴露运行时切换流程的公共接口。

## 决策

在 `GameApp.StartGameLogic` 中：

1. 调用 `GameModule.Procedure.Shutdown()` 关闭主包流程状态机。
2. 重新 `Initialize` 热更域流程（`ProcedureMainMenu`、`ProcedureLobby`、`ProcedureBattle`）。
3. 启动主菜单流程。
4. 运行时流程切换通过反射调用内部 `_procedureFsm.ChangeState<T>()`，封装在 `GameApp.ChangeProcedure<T>` 中。

替代方案：

- 在主包中预注册空壳流程，热更后通过替换流程实例实现切换。被拒绝，因为流程实例类型在热更程序集中，主包无法直接引用。
- 修改 TEngine 暴露 `ChangeState<T>`。这是长期目标，当前先以反射过渡。

## 后果

- 实现了主包与热更域的流程解耦。
- 反射调用内部 FSM 是脆弱点，也是 AOT 风险点。
- 建议后续推动 TEngine 在 `IProcedureModule` 增加 `ChangeState<T>()`，然后移除反射。
