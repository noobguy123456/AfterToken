# AfterToken 代码风格迁移计划

> **状态**：待人工逐步推进  
> **目标**：将现有代码逐步对齐 [代码规范](./CODING_STANDARDS.md) / [UI 规范](./UI_STANDARDS.md) / [资源命名规范](./ASSET_NAMING_STANDARDS.md)  
> **原则**：小步快跑、按模块/按风险分批、不破坏运行时行为、不一次性大规模重写。

---

## 当前已知不一致点

### 1. 私有字段前缀不统一

- **现状**：
  - 新 `GameLogic` 代码大量使用 `_camelCase`（如 `_animator`）。
  - 部分旧 TEngine/UIModule 代码仍使用 `m_` 前缀（如 `m_curFitRect`、`m_textError`）。
- **风险**：中等。修改字段名后需同步更新 `ScriptGenerator()` 路径或 Prefab 节点名，否则 UI 绑定会失效。
- **建议批次**：
  1. 先统一 `GameLogic` 内新建/正在改动的模块。
  2. 对 TEngine 框架代码，单独评估是否升级框架版本或本地 fork 维护。

### 2. 属性访问器顺序不一致

- **现状**：
  - 新代码：`public Vector2 MoveDirection { get; private set; }`
  - TEngine/UIModule 代码：`public string WindowName { private set; get; }`、`public bool IsPrepare { protected set; get; }`
- **风险**：低。仅风格问题，不影响运行时。
- **建议**：在重构相关类时顺手统一，不单独发起迁移。

### 3. 公共字段 vs 属性

- **现状**：配置/数据类存在 `public int damage;` 等公共字段。
- **风险**：低～中。Luban 接入后这些类可能被自动生成，届时一次性替换。
- **建议**：
  1. 接入 Luban 前的临时数据类可保留字段，但统一命名为 `PascalCase`（已符合规范）。
  2. 接入 Luban 后，以生成器输出为准，再评估是否改为属性。

### 4. UI 文档与代码现状不一致

- **现状**：
  - `docs/CoWork/UI-Prefab-CoWork-Workflow.md` 第 7 节仍写“使用 UGUI Text”。
  - `SKILL.md` 的 UI Prefab 路径速查表中 `BattleMainUI` 写成 `Assets/AssetRaw/UI/BattleMainUI.prefab`（缺少子目录）。
  - 不同文档中 UI 节点前缀表条目数量不一致（8 条 vs 20+ 条）。
- **风险**：低。
- **建议**：作为本次规范文档输出的一部分，立即修正这些文档冲突（已在 [代码规范](./CODING_STANDARDS.md) 中统一）。

### 5. `.editorconfig` 过薄

- **现状**：仅配置 ReSharper 行宽和 attribute 换行。
- **风险**：低。
- **建议**：本次已增强 `.editorconfig`，后续观察 IDE 自动格式化是否符合预期。

### 6. 缺少统一规范入口

- **现状**：规范分散在 `repowiki`、`.claude/skills/`、`docs/framework/` 等位置。
- **风险**：低。
- **建议**：本次已创建 `docs/standards/CODING_STANDARDS.md` 作为唯一权威入口，并在相关文档顶部添加迁移提示。

---

## 分阶段迁移计划

### 阶段 0：文档与基础设施（本次完成）

- [x] 创建 `docs/standards/` 目录及 6 份规范文档（含 README 索引）。
- [x] 增强 `.editorconfig`。
- [x] 修正 `docs/CoWork/UI-Prefab-CoWork-Workflow.md` 中 UGUI Text 段落。
- [x] 修正 `.claude/skills/tengine-dev/SKILL.md` 中 `BattleMainUI` 路径示例（.codex 副本无该表，仅更新文档路由）。
- [x] 在 `repowiki/.../代码规范与标准.md` 顶部添加权威来源提示。
- [x] 更新 `docs/framework/README.md`、`docs/modules/README.md` 的交叉引用。

### 阶段 1：新代码强制遵循

- 所有新增模块、新增 UI、新增脚本从创建日起必须 100% 符合规范。
- 每次代码审查使用 [代码审查清单](./CODE_REVIEW_CHECKLIST.md)。
- 禁止合并明显违反红线的代码。

### 阶段 2：按模块逐步改造旧代码

建议优先改造高频维护模块：

1. **UI 层**：统一 UI 脚本中的字段前缀、属性顺序、节点绑定变量命名。
2. **战斗系统**：`PlayerSystem`、`WeaponSystem`、`BattleSystem` 等核心类。
3. **基础设施**：事件系统、资源加载封装。
4. **TEngine 框架代码**：最后处理，或等上游 TEngine 升级后统一合并。

### 阶段 3：Luban 接入后的配置类统一

- 接入 Luban 配置表后，以生成器输出为准统一数据类命名与结构。
- 同步更新 `naming-rules.md` 中 Luban 相关说明。

### 阶段 4：自动化检查（可选）

- 考虑在 CI/编辑器中集成：
  - `.editorconfig` 格式化检查。
  - 命名规则静态扫描（如自定义 Roslyn analyzer 或 ReSharper CLI）。
  - 禁止模式扫描（`Resources.Load`、`FindObjectOfType` 等）。

---

## 迁移原则

1. **不破坏运行时**：任何字段重命名必须同步更新 `ScriptGenerator()`、反射调用、序列化字段。
2. **小步提交**：每个模块/每个系统独立提交，方便回滚。
3. **先写后改**：新模块先按规范落地，旧模块在维护时顺手重构。
4. **文档先行**：每完成一批改造，更新对应模块 `docs/modules/<category>/<module>/progress.md`。
5. **人类审阅**：所有迁移改动由人类最终提交，AI 不主动 `git commit/push`。
