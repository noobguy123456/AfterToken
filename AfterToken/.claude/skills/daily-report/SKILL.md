---
name: daily-report
description: AfterToken 项目日报规范。每次任务结束、阶段完成或用户提及“日报”时，按当日日期在 docs/DailyRecord/ 下生成/更新日报。
triggers:
  - 日报
  - 记录
  - 今天工作内容
  - 今日工作
---

# 日报规范

## 触发时机

以下情况必须写/更新日报：

1. 用户主动要求“生成日报”、“记录今天”、“写日报”。
2. 一个任务/阶段完成后，需要向用户汇总时。
3. 当天结束对话前（如果还没有写过当日报）。
4. 发生需要人类接手的问题时（按 `tengine-dev` 的“卡住即上报”原则）。

## 核心规则

1. **以当日日期为准**  
   日报文件名必须是 `docs/DailyRecord/YYYY-MM-DD.md`，其中 `YYYY-MM-DD` 为当前日期。

2. **没有则新建，有则追加**  
   - 如果当天日报文件不存在，使用 `WriteFile` 新建。
   - 如果当天日报文件已存在，使用 `WriteFile` 以 `append` 模式追加到末尾，不要覆盖已有内容。

3. **格式统一**

```markdown
# YYYY-MM-DD

## 工作内容

1. **简要标题**
   - 具体改动/实现点。
   - 涉及文件路径：`Assets/...` / `docs/...` / `.claude/skills/...`

2. **另一项工作**
   - ...

## 未完成 / 待验证

- ...
```

4. **记录内容要求**
   - 代码改动要写明关键文件路径。
   - Prefab/场景/资源变更要写明 Prefab 路径或场景路径。
   - 文档变更要写明文档路径。
   - Unity/MCP 操作结果（如 `Battle/Setup Battle Scene & Resources` 成功/失败）要简要记录。
   - 未完成的项、阻塞点、待人类验证的内容单独列出。

5. **不要修改旧日期的日报**  
   历史日报只读不写。所有新内容必须写入当天文件。

## 示例

```markdown
# 2026-06-21

## 工作内容

1. **UI Prefab 热更路径规范化**
   - 将 `WeaponWheelUI` / `SniperScopeUI` 从 `Assets/Resources/` 迁移到 `Assets/AssetRaw/UI/`。
   - 修改 `WeaponWheelUI.cs` / `SniperScopeUI.cs` 的 `WindowAttribute` 使用 `location` 参数走 YooAsset。
   - 更新 `BattleSceneSetup.cs` 在新路径生成 Prefab，并删除旧 Resources 路径文件。
   - 重新运行 `Battle/Setup Battle Scene & Resources`，`SimulateBuild` 成功。

2. **更新 tengine-dev skill**
   - 在 `.claude/skills/tengine-dev/SKILL.md` 中新增 UI Prefab 路径规范与速查表。

## 未完成 / 待验证

- Play Mode 验证武器轮盘、狙击镜、命中反馈等 UI 是否正常显示。
```
