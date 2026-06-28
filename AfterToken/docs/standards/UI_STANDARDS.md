# AfterToken UI 规范

> **状态**：项目级权威 UI 规范  
> **适用范围**：所有热更域 UI（`GameLogic` 程序集）及其 Prefab、脚本、资源地址  
> **关联文档**：
> - [代码规范](./CODING_STANDARDS.md)
> - [资源命名规范](./ASSET_NAMING_STANDARDS.md)
> - [代码审查清单](./CODE_REVIEW_CHECKLIST.md)
> - 详细 UI 前缀参考：`.claude/skills/tengine-dev/references/naming-rules.md`

---

## 1. 技术选型

- **UI 框架**：TEngine `UIWindow` + `[Window(...)]` Attribute。
- **文字渲染**：TextMeshPro（TMP）已全量替换 uGUI Text，新增 UI 必须使用 TMP。
- **异步**：所有 UI 加载接口均为 `UniTask`。

---

## 2. 目录与命名约定

### 2.1 三者一致原则

| 项 | 规则 | 示例 |
|----|------|------|
| Prefab 路径 | `Assets/AssetRaw/UI/{Name}/{Name}.prefab` | `Assets/AssetRaw/UI/BattleMainUI/BattleMainUI.prefab` |
| 脚本路径 | `Assets/GameScripts/HotFix/GameLogic/UI/{Name}/{Name}.cs` | `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs` |
| 类名 | `public class {Name}UI : UIWindow` | `public class BattleMainUI : UIWindow` |
| `[Window]` location | 与 Prefab 文件名一致（不含扩展名） | `[Window(UILayer.UI, location: "BattleMainUI")]` |

- **禁止**把 Prefab 直接放在 `Assets/AssetRaw/UI/` 根目录，避免 YooAsset 地址冲突。
- UI 脚本中 `ScriptGenerator()` 里的节点路径必须与 Prefab 实际层级完全一致。

### 2.2 新增 UI 标准流程

1. 人类按 [UI-Prefab-CoWork-Workflow.md](../CoWork/UI-Prefab-CoWork-Workflow.md) 的需求模板提出需求。
2. AI 使用菜单 `Tools/UI/Create UI Prefab` 生成脚本 + Prefab。
3. 人类在 **Prefab Mode** 调整布局、颜色、图片、字号；若调整节点路径，需重新提出需求。
4. AI 在脚本中补充事件监听、数据刷新、动画控制等业务逻辑。

---

## 3. UI 节点命名前缀

Prefab 节点前缀决定 `UIScriptGenerator` 自动生成的绑定类型。前缀匹配规则来自 `ScriptGeneratorSetting.asset` 的 `uiElementRegex` 字段，匹配时不带尾部下划线。

| 前缀 | 生成类型 | 示例节点名 |
|------|---------|----------|
| `m_go_` | `GameObject` | `m_go_Effect` |
| `m_item_` | `UIWidget`（子类）| `m_item_Slot` |
| `m_tf_` | `Transform` | `m_tf_Container` |
| `m_rect_` | `RectTransform` | `m_rect_Panel` |
| `m_text_` | `TextMeshProUGUI` | `m_text_Title` |
| `m_richText_` | `RichTextItem` | `m_richText_Desc` |
| `m_btn_` | `Button` | `m_btn_Start` |
| `m_img_` | `Image` | `m_img_Icon` |
| `m_rimg_` | `RawImage` | `m_rimg_Avatar` |
| `m_scroll_` | `ScrollRect` | `m_scroll_List` |
| `m_scrollBar_` | `Scrollbar` | `m_scrollBar_Vert` |
| `m_input_` | `InputField` | `m_input_Name` |
| `m_grid_` | `GridLayoutGroup` | `m_grid_Items` |
| `m_hlay_` | `HorizontalLayoutGroup` | `m_hlay_Tabs` |
| `m_vlay_` | `VerticalLayoutGroup` | `m_vlay_List` |
| `m_slider_` | `Slider` | `m_slider_Volume` |
| `m_toggle_` | `Toggle` | `m_toggle_Sound` |
| `m_group_` | `ToggleGroup` | `m_group_Tab` |
| `m_curve_` | `AnimationCurve` | `m_curve_Anim` |
| `m_canvasGroup_` | `CanvasGroup` | `m_canvasGroup_Fade` |
| `m_tmp_` | `TextMeshProUGUI` | `m_tmp_Name` |
| `m_tmpInput_` | `TMP_InputField` | `m_tmpInput_Search` |
| `m_tmpDropdown_` | `TMP_Dropdown` | `m_tmpDropdown_Lang` |
| `m_canvas_` | `Canvas` | `m_canvas_Overlay` |
| `m_dropdown_` | `Dropdown` | `m_dropdown_Select` |

### 3.1 前缀匹配顺序

由于 regex 按顺序匹配，**长前缀必须排在短前缀之前**。关键顺序：

- `m_scrollBar_` 必须在 `m_scroll_` 之前。
- `m_tmpInput_`、`m_tmpDropdown_` 必须在 `m_tmp_` 之前。
- `m_richText_` 必须在 `m_text_` 之前。

这些顺序已由 `ScriptGeneratorSetting.asset` 内置，新增前缀时需遵循同样规则。

### 3.2 命名禁止项

- 禁止无意义节点名：`Text`、`Image`、`Button`、`Panel`、`GameObject`。
- 禁止一个类控制多个面板的节点。
- 禁止通过 `GameObject.Find("OtherUI/xxx")` 跨 UI 访问节点。

---

## 4. UIWindow 生命周期

```
InternalLoad(location)
  → Handle_Completed(panel)
  → InternalCreate()
      → Inject()
      → ScriptGenerator()      // 绑定节点
      → BindMemberProperty()
      → RegisterEvent()        // 注册 UI 事件
      → OnCreate()             // 初始化
```

常用重写点：

| 方法 | 用途 |
|------|------|
| `ScriptGenerator()` | 绑定 Prefab 节点到字段 |
| `OnCreate()` | 初始化、绑定点击事件、全屏面板调用 `FixFullScreenCanvas()` |
| `OnRefresh()` | 根据传入数据刷新界面 |
| `OnUpdate()` | 每帧更新，仅在窗口 `Visible` 且 `IsPrepare` 时调用 |
| `OnSetVisible(bool visible)` | 显隐切换回调 |
| `RegisterEvent()` | 使用 `AddUIEvent` 注册全局/接口事件 |
| `OnDestroy()` | 清理非自动释放的资源 |

示例：

```csharp
[Window(UILayer.UI, location: "PauseUI", fullScreen: true)]
public class PauseUI : UIWindow
{
    private TextMeshProUGUI _textTitle;
    private Button _btnResume;

    protected override void ScriptGenerator()
    {
        _textTitle = FindChildComponent<TextMeshProUGUI>("m_rect_Content/m_text_Title");
        _btnResume = FindChildComponent<Button>("m_rect_Content/m_btn_Resume");
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        FixFullScreenCanvas();
        BindEvents();
    }

    private void BindEvents()
    {
        _btnResume?.onClick.AddListener(() => GameModule.UI.CloseUI<PauseUI>());
    }

    protected override void RegisterEvent()
    {
        base.RegisterEvent();
        AddUIEvent<int, int>(IPlayerEvent_Event.OnHpChanged, OnHpChanged);
    }

    private void OnHpChanged(int current, int max)
    {
        // ...
    }
}
```

---

## 5. 全屏面板与 CanvasScaler

### 5.1 全屏面板

所有全屏面板在 `OnCreate()` 中必须调用：

```csharp
FixFullScreenCanvas();
```

`FixFullScreenCanvas()` 会将根 `RectTransform` 设为全屏拉伸：

```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
rt.anchoredPosition = Vector2.zero;
rt.sizeDelta = Vector2.zero;
rt.pivot = new Vector2(0.5f, 0.5f);
```

### 5.2 CanvasScaler

| 面板类型 | 是否保留根 CanvasScaler | 原因 |
|---------|----------------------|------|
| 普通全屏 HUD / 菜单 | 否 | 使用 `UIRoot/UICanvas` 的 750×1334 缩放 |
| 需要独立设计分辨率的面板 | 是 | 如 `SniperScopeUI`、`WeaponWheelUI` 按 1920×1080 设计 |

生成工具会根据“是否需要独立 CanvasScaler”自动决定是否添加。

---

## 6. UI 层级

```csharp
public enum UILayer : int
{
    Bottom = 0,
    UI = 1,
    Top = 2,
    Tips = 3,
    System = 4,
}
```

- `LoadingUI` 位于 `UILayer.System`，保证覆盖在所有普通 UI 之上。
- 层级深度计算：`depth = layer * 2000 + index * 100`。

---

## 7. 字体

- TMP 默认字体资产路径：`Assets/AssetRaw/Fonts/MainUIFont.asset`
- 运行时若未配置 `TMP_Settings.defaultFontAsset`，会动态创建一份 Arial 字体资产兜底。
- 代码中动态创建 TMP 文本时，使用 `TMPFontProvider.DefaultFont`。

---

## 8. UI 加载接口

```csharp
// 显示 UI（不等待）
GameModule.UI.ShowUIAsync<T>(params object[] userDatas);

// 显示 UI 并等待准备完成
var ui = await GameModule.UI.ShowUIAsyncAwait<T>();

// 关闭
GameModule.UI.CloseUI<T>();
GameModule.UI.CloseAll();
```

---

## 9. UI 分类

| 类型 | 例子 | 打开者 |
|------|------|--------|
| Procedure 全屏面板 | MainMenuUI、LobbyUI、BattleMainUI | Procedure |
| 全局服务面板 | LoadingUI、DamageNumberUI、HitFeedbackUI | Procedure / GameModule |
| 弹窗/二级面板 | PauseUI、SettingsUI、TipUI | 当前活跃 UI / 系统 |

---

## 10. 与人类协作

- AI 负责：生成 C# 脚本骨架、生成基础 Prefab 结构、按需求更新节点绑定、跑 `Create UI Prefab` 工具。
- 人类负责：在 Prefab Mode 调整布局、颜色、字号、图片资源。
- 人类**不直接修改** `ScriptGenerator()` 里的节点路径；改路径需重新提需求给 AI。

详见 [UI-Prefab-CoWork-Workflow.md](../CoWork/UI-Prefab-CoWork-Workflow.md)。
