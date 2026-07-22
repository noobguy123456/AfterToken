# AfterToken 代码审查清单

> **状态**：项目级审查清单  
> **适用范围**：所有提交到 `GameLogic`、`GameProto`、`TEngine.*`、`Launcher`、`Editor` 的 C# 代码与资源  
> **关联文档**：
> - [代码规范](./CODING_STANDARDS.md)
> - [UI 规范](./UI_STANDARDS.md)
> - [资源命名规范](./ASSET_NAMING_STANDARDS.md)

---

## 通用代码审查清单

### 命名与格式

- [ ] 类/接口/方法/属性/事件使用 `PascalCase`。
- [ ] 接口名以 `I` 开头。
- [ ] 常量使用 `UPPER_SNAKE_CASE`。
- [ ] 私有/受保护字段使用 `_camelCase`，**不**使用 `m_` 前缀（UI 节点绑定变量除外）。
- [ ] 属性顺序统一为 `get; private set;` / `protected set;`。
- [ ] 参数与局部变量使用 `camelCase`。
- [ ] 异步方法以 `Async` 结尾。
- [ ] 事件回调以 `On` 开头。
- [ ] 开括号使用 Allman 风格（独占一行）。
- [ ] 缩进为 4 个空格，无 Tab。
- [ ] 行宽不超过 180 字符。

### 程序集与目录

- [ ] `GameScripts/Main` 不引用 `GameScripts/HotFix/` 类型。
- [ ] 热更代码放在 `Assets/GameScripts/HotFix/GameLogic/` 或 `GameProto/`。
- [ ] 一个文件一个主要类/接口/枚举。

### using 与命名空间

- [ ] `using` 顺序：System → Unity/第三方 → 项目命名空间 → 别名。
- [ ] 不使用未使用的 `using`。
- [ ] 命名空间显式声明，不使用文件作用域命名空间。

### 异步与资源

- [ ] IO / 资源 / 耗时操作用 `UniTask`，不使用 `Task` / `Coroutine` / 同步加载。
- [ ] `LoadAssetAsync` 对应 `UnloadAsset`。
- [ ] `LoadGameObjectAsync` 实例化的对象销毁时自动释放，不手动 `UnloadAsset`。
- [ ] `UniTask` / `UniTaskVoid` 返回值必须 `await` 或 `.Forget()`。
- [ ] 需要时传递 `CancellationToken` 并在 `OnDestroy` 中取消/释放。
- [ ] 不静态持有 Asset 引用。

### 模块与事件

- [ ] 模块访问使用 `GameModule.XXX`，不直接 `ModuleSystem.GetModule<T>()`。
- [ ] 模块间通信用 `GameEvent`。
- [ ] UI 内部通信用 `AddUIEvent`。
- [ ] 禁止跨模块强引用。

### 红线禁止项

- [ ] 未使用 `Resources.Load`。
- [ ] 未直接 `Instantiate(prefab)` 加载资源。
- [ ] 未在运行时逻辑中使用 `FindObjectOfType` / `FindObjectsOfType`。
- [ ] `Update` / `FixedUpdate` 中未直接 `new` 对象（应使用 `MemoryPool`）。
- [ ] 未新增业务硬编码数值/动画名/颜色/层级/文本格式（应先落 Luban 配置表）。

### 配置表

- [ ] 新功能涉及可变更数值、动画名、颜色、层级、文本格式、Prefab 路径时，先在 `Configs/GameConfig/` 完成 Luban 配置表设计。
- [ ] 业务代码使用 `ConfigSystem.Instance.Tables.TbXxx` 读取配置，保留兜底值。
- [ ] 修改配置后执行 `Configs/GameConfig/gen_code_bin_to_project.bat` 重新生成代码与 JSON。
- [ ] 新增表已同步 `ConfigSystem.cs` 的 `_tableFiles` 与 `GameProto.csproj` 的 `Compile Include`。

### 注释与文档

- [ ] 公共 API、`GameModule`、事件接口、`XxxSystem` 公共方法写 `///` XML 注释。
- [ ] 复杂逻辑有中文注释说明“为什么”。
- [ ] TODO/FIXME 使用统一格式并关联进度/issue。

---

## UI 专项审查清单

### 目录与命名

- [ ] Prefab 路径为 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`。
- [ ] 脚本路径为 `Assets/GameScripts/HotFix/GameLogic/UI/{Name}/{Name}.cs`。
- [ ] 类名为 `{Name}UI`，继承 `UIWindow`。
- [ ] `[Window]` 的 `location` 与 Prefab 文件名一致。
- [ ] 节点名使用规范前缀（`m_rect_` / `m_text_` / `m_btn_` 等）。
- [ ] 脚本中绑定变量名为 `_camelCase` 去掉 `m_` 前缀。

### 生命周期

- [ ] `ScriptGenerator()` 中节点路径与 Prefab 实际层级一致。
- [ ] 全屏面板在 `OnCreate()` 中调用 `FixFullScreenCanvas()`。
- [ ] 点击事件在 `OnCreate()` 或 `BindEvents()` 中注册。
- [ ] 全局事件在 `RegisterEvent()` 中用 `AddUIEvent` 注册。
- [ ] `OnDestroy()` 中清理非自动释放的资源。

### 字体与渲染

- [ ] 使用 `TextMeshProUGUI`，不使用 `UnityEngine.UI.Text`。
- [ ] 动态创建 TMP 文本时使用 `TMPFontProvider.DefaultFont`。
- [ ] 字体 asset 路径为 `Assets/AssetRaw/Fonts/MainUIFont.asset`。

### Canvas 与层级

- [ ] 普通全屏面板不保留根 `CanvasScaler`，跟随 `UIRoot`。
- [ ] 独立分辨率面板保留 `CanvasScaler`。
- [ ] `UILayer` 选择符合 UI 类型（`UI` / `Top` / `Tips` / `System`）。

### 资源与构建

- [ ] 新增 UI 后运行 `Battle/Setup Battle Scene & Resources` 或手动 `SimulateBuild`。
- [ ] `SimulateBuild` 无地址冲突。
- [ ] UI 相关贴图/图集放在 `Assets/AssetRaw/UIRaw/` 下。

---

## 资源审查清单

- [ ] 资源路径符合 `ASSET_NAMING_STANDARDS.md`。
- [ ] 文件名使用 `PascalCase`，无空格/中文/特殊字符。
- [ ] 热更资源未放在 `Assets/Resources/`。
- [ ] 同一 YooAsset Group 内无同名资源。
- [ ] UI Prefab 未直接放在 `Assets/AssetRaw/UI/` 根目录。
- [ ] 修改收集器/资源结构后重新执行 `SimulateBuild`。
- [ ] 代码中 `location` 与资源地址一致。

---

## 提交流程建议

1. 自评：作者按本清单逐项勾选。
2. AI / 人工复核：重点检查命名、红线、资源释放、UI 三者一致。
3. 不通过项直接在 PR/提交说明中列出，并关联对应模块 `docs/modules/<category>/<module>/progress.md`。
