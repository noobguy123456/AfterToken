# 0002. 主菜单/大厅 UI 采用 Prefab 驱动布局

## 状态

已接受

## 背景

`MainMenuUI` 与 `LobbyUI` 最初为了快速验证流程，在 `OnCreate` 中动态创建所有 UI 节点（标题、按钮、列表）。这导致：

- 布局、字体、颜色等业务逻辑混在一起，违反表现层与系统层分离。
- 美术和策划无法在不改代码的情况下调整界面。
- 运行时创建 UI 节点不利于后续使用 TMP、图集、动画等高级功能。

约束：

- 继续使用 `UIWindow` + `ScriptGenerator` 的 TEngine UI 规范。
- 必须兼容当前 `UIRoot/UICanvas` 嵌套结构，避免再出现 scale=0 问题。
- 保持占位实现的可替换性，后续美术只需替换 Prefab 与节点绑定。

## 决策

将 `MainMenuUI` / `LobbyUI` 改为 Prefab 驱动：

1. `BattleSceneSetup` 创建/更新对应 Prefab，节点使用 TEngine 命名规范前缀（`m_text_`、`m_btn_`、`m_rect_` 等）。
2. 脚本中通过 `ScriptGenerator` 绑定节点引用。
3. `OnCreate` 只负责注册按钮事件与动态数据填充（如关卡列表）。
4. 按钮文本、颜色等视觉属性优先在 Prefab 中维护。

替代方案：保留代码动态生成并引入 UI DSL。被拒绝，因为项目已有 TEngine UI 管线，Prefabs + ScriptGenerator 是团队最熟悉的路径。

## 后果

- 表现层与逻辑层分离，UI 脚本更薄。
- 美术可以直接在 Prefab 上工作，无需修改 C#。
- 需要保证 Prefab 中节点名称与 `ScriptGenerator` 绑定路径一致。
