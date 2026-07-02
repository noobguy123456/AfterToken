# MCP 游戏内容操作限制

## 触发场景

当任务涉及以下操作时，AI 应停止尝试用 MCP 工具自行完成，并明确通知用户：

- 进入 Play Mode 进行手玩验证
- 模拟玩家输入（键盘、鼠标、手柄）
- 触发战斗/死亡/闪避/换弹/掉落等实时玩法结果
- 在 Play Mode 中切换场景或加载关卡
- 通过截图、Game View 反馈判断玩法正确性

## 处理方式

1. **不强行执行**：不反复调用 `manage_editor.play`、不通过反射注入输入事件、不通过 `execute_code` 直接修改 HP/弹药为 0 来触发死亡/换弹。
2. **提供验证清单**：编写清晰的 `expected behavior` + `checklist`，交给人类在编辑器或真机中验证。
3. **专注代码与文档**：AI 完成代码实现、单元测试、文档更新，将运行态验证留给人。

## 已记录位置

- [CLAUDE.md](../../../CLAUDE.md) — 项目级注意事项
- [.claude/skills/tengine-dev/SKILL.md](../../../skills/tengine-dev/SKILL.md) — skill 核心红线
- [AGENT.md](../../../AGENT.md) — agent 入口提示
