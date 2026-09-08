# MCP 游戏内容操作限制（2026-09-06 修订）

## 触发场景

AI 使用 MCP 工具操作 Unity 编辑器时的行为边界。**本文件为历史版本的修订：Play Mode 自动化验证现已允许**（用户 2026-08 起多次明确授权），旧版"不得进 Play Mode / 不得触发战斗结果"条款作废。

## 现行边界

### 允许（自动化验证）

- 进 / 退 Play Mode，读 Console（反射 `UnityEditor.LogEntries`/`LogEntry`）
- 反射读写运行时状态、切换流程（`GameApp.ChangeProcedure`）、执行 GM 命令
- 截图核对画面；编辑器模式下用 AssetDatabase API 创建/修改 prefab、材质、场景对象
- 资产移动用 `AssetDatabase.MoveAsset` 保持 GUID 不丢引用

### 禁止

- **打开 / 关闭 Unity 编辑器本体**（用户明确要求）
- 模拟玩家实时输入（WASD/鼠标/按键注入）替代手玩——手感、实时判定类验证留给用户
- 验证污染存档后不还原（动了货币/仓库/任务等数据，测完必须恢复并 Flush）
- 一次失败后反复重试同一 MCP 调用绕过限制

## 验证责任划分

- AI 完成：编译零错误、Console 零异常、状态/数据断言、截图核对
- 用户完成：手感、节奏、视觉细节的真人确认

## 已记录位置

- [CLAUDE.md](../../../CLAUDE.md) — 「🎮 MCP 操作边界」与「🤖 子代理使用约定」（2026-09-06 修订版，以该文件为准）
- [.claude/skills/tengine-dev/SKILL.md](../../../skills/tengine-dev/SKILL.md) — skill 红线第 10 条已同步修订
- [AGENT.md](../../../AGENT.md) — agent 入口提示已同步修订
