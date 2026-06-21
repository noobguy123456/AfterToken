# 0001. 场景切换时显示 LoadingUI

## 状态

已接受

## 背景

游戏流程需要在 `ProcedureMainMenu`、`ProcedureLobby`、`ProcedureBattle` 之间切换，每次切换都会异步加载新场景。场景加载期间如果没有任何遮挡，玩家会看到旧场景被清空、新场景尚未渲染的“黑屏/空屏”过程，体验较差。

约束：

- 必须使用 TEngine 的 `GameModule.Scene.LoadSceneAsync` 异步加载。
- 加载 UI 必须位于 `UILayer.System` 层，确保覆盖在所有普通 UI 之上。
- 切换流程可能被玩家快速触发，需要支持取消，避免旧加载任务继续执行并打开已失效的 UI。

## 决策

在每个 Gameplay 流程的 `OnEnter` 中：

1. 先打开 `LoadingUI`。
2. 调用 `LoadSceneAsync` 并在进度回调中更新 `LoadingUI.SetProgress(progress)`。
3. 场景加载完成后执行该流程的后续初始化（创建系统、打开主 UI 等）。
4. 最后关闭 `LoadingUI`。

统一抽象到 `GameplayProcedureBase` 中，子类只需声明场景名与“加载完成后”的逻辑，避免三个流程重复实现。

替代方案：在 `GameApp.ChangeProcedure` 中统一显示 LoadingUI。被拒绝，因为 `GameApp` 目前通过反射调用内部 FSM，LoadingUI 的进度需要与具体场景的加载进度绑定，放在基类中更局部、更容易维护。

## 后果

- 场景切换有明确反馈，体验提升。
- `LoadingUI` 的打开/关闭逻辑集中在 `GameplayProcedureBase`，子流程只关心业务。
- 需要保证 `LoadingUI` Prefab 的 `UILayer` 为 `System`，否则可能被其他 UI 遮挡。
