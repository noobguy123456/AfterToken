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
- **文本语言（2026-08-03 用户决策）**：**后续所有面向用户的文本一律先用英文**（按钮、标签、提示、失败原因、飘字等），直到用户明确表示可以使用中文为止。在英文-only 期间不再需要 legacy `UnityEngine.UI.Text` 的中文兜底（TMP 对英文完整支持）；中文回退方案（legacy Text + `LegacyRuntime.ttf`）仅作为历史记录保留，恢复中文时仍需走该方案或接入中文 TMP 字库。
- **异步**：所有 UI 加载接口均为 `UniTask`。

### 1.1 渲染模式与特效接入约定

> **强制规则**：AI 在创建任何新 UI 之前，必须先询问人类选择以下哪种类型，并主动说明三类的特效接入差异，由人类确认后再动工。

| 类型 | Canvas Render Mode | 特效接入方式 | 特效差异说明 |
|------|-------------------|-------------|-------------|
| **A. 标准界面**（默认） | Screen Space - Overlay | **帧动画**（美术序列帧，`Image` 换图播放） | Overlay 下场景粒子永远被 UI 盖住不可见；帧动画是普通 UI 控件，层级/遮罩/适配直接可用，覆盖约 80% 装饰性特效需求（流光、飞溅、结算烟花等） |
| **B. 重特效界面**（个案审批） | Screen Space - Camera | **真粒子**（ParticleSystem 摆在 canvas plane 前方，用 layer + plane distance 控制遮挡） | 效果上限最高（拖尾、随机性、物理感），但绑定相机、FOV 变化会影响 UI 占比，遮挡排序需人工管理；仅限抽卡、开箱、结算等特效重的界面，需说明理由 |
| **C. 3D 内容展示** | 任意（通常叠加在 A 类界面上） | **RenderTexture + RawImage**（独立相机渲染隔离舞台，RT 作为 UI 控件显示） | 用于装备/角色 3D 模型展示、复杂演出；成本最高（RT 显存 + 额外一次渲染），复用狙击镜的 RenderTexture 经验 |

**约定**：

1. **默认 A**：无法判断时一律 Screen Space - Overlay，特效用帧动画。
2. **B 需审批**：选择 B 必须说明"为什么帧动画满足不了"，禁止泛滥。
3. **不引入第三方 UI 粒子插件**（如 ParticleEffectForUGUI），特效只走帧动画 / 真粒子 / RenderTexture 三条路。
4. 现有 5 个 Screen Space - Camera 的 UI（BattleBagUI、ItemTooltipUI、LoginUI、TestUI、WarehouseUI）保留不动；修改这些 UI 时按需评估是否迁回 Overlay。
   - **已定**：背包（BattleBagUI）的物品特效框采用**帧动画**方案（A 类做法，slot 上叠 `Image` 播序列帧），不使用真粒子——粒子不被 RectMask2D 裁剪且需逐 slot 坐标转换。下次改动 BattleBagUI 时可将 Canvas 迁回 Overlay。
5. 世界空间信息（敌人血条、伤害飘字）不走 uGUI Canvas，用 World Space Canvas 或 SpriteRenderer 占位方案，单独评审。

---

## 2. 目录与命名约定

> **强制规则（2026-08-03 用户决策）**：**所有新 UI 必须做成正式 Prefab**（走 2.2 流程：`Tools/UI/Create UI Prefab` 生成 Prefab + 脚本，`ScriptGenerator()` 绑定节点）。**禁止**再出现"挂 `TestUI` 占位 Prefab、运行时代码拼装界面"的写法。
>
> 历史遗留已全部清零（2026-08-03）：`SimulationMainUI`、`BuildingInfoUI`、`BuildingSelectionUI` 已迁移为正式 Prefab（静态结构在 Prefab、动态列表项仍运行时生成，属正常模式）。

### 2.1 三者一致原则

| 项 | 规则 | 示例 |
|----|------|------|
| Prefab 路径 | `Assets/AssetRaw/UI/{Name}/{Name}.prefab`；同模块 UI 可收进模块子目录 `Assets/AssetRaw/UI/{Module}/{Name}/{Name}.prefab` | `Assets/AssetRaw/UI/BattleMainUI/BattleMainUI.prefab`、`Assets/AssetRaw/UI/Simulation/SimulationMainUI/SimulationMainUI.prefab` |
| 脚本路径 | `Assets/GameScripts/HotFix/GameLogic/UI/{Name}/{Name}.cs`；同模块 UI 可收进模块子目录 `.../UI/{Module}/{Name}/{Name}.cs` | `.../UI/BattleMainUI/BattleMainUI.cs`、`.../UI/Simulation/SimulationMainUI/SimulationMainUI.cs` |
| 类名 | `public class {Name}UI : UIWindow` | `public class BattleMainUI : UIWindow` |
| `[Window]` location | 与 Prefab 文件名一致（不含扩展名；地址按文件名解析，模块子目录不影响） | `[Window(UILayer.UI, location: "BattleMainUI")]` |

- **禁止**把 Prefab 直接放在 `Assets/AssetRaw/UI/` 根目录，避免 YooAsset 地址冲突。
- 现有模块子目录：`Simulation/`（SimulationMainUI、BuildingSelectionUI、BuildingInfoUI，2026-08-03 归拢）。
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
