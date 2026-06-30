# Hotfix Pipeline 进度

## 已完成
- [x] HybridCLR 环境配置
- [x] `GameLogic` / `GameProto` 热更程序集划分
- [x] 热更 DLL 编译与加载流程
- [x] 主包流程 `ProcedureLoadAssembly` 加载热更程序集
- [x] 热更域流程接管：`GameApp.StartGameLogic` 启动热更域 `ProcedureMainMenu`

## 待办
- [ ] AOT 元数据补充验证（打包后无 ExecutionEngineException）
- [ ] 配置表热更验证（修改 Luban 表后 SimulateBuild + 运行时加载）
- [ ] 真机构建流程验证

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
