# Procedure System 进度

## 已完成
- [x] GameplayProcedureBase 统一流程基类
- [x] ProcedureMainMenu / ProcedureSimulation（基地）/ ProcedureBattle
- [x] ProcedureLobby 废弃删除：大厅概念取消，经营场景=基地，选关窗口（LobbyUI）由基地内 Deploy 按钮打开
- [x] GameApp.ChangeProcedure 切流程

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-08-20 | B 方案：删除 `ProcedureLobby`（含 GameApp 注册与热更重进 switch，热更时旧 lastProcedure=Lobby 落回主菜单默认分支）；一局结束清理挪入 `ProcedureSimulation.EnterAsync`；`LobbyScene.unity` 保留磁盘但无代码引用，标记废弃 |

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`