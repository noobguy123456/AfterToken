# AfterToken UI / Prefab 人机协作工作流

> **文档状态**：已对齐  
> **适用对象**：策划、程序、美术、AI 工具  
> **目标**：让 AI 负责可标准化的 Prefab 骨架和脚本搭建，人类只负责调整位置、颜色、图片和提出逻辑需求。

---

## 一、总体分工

| 角色 | 负责内容 | 不负责内容 |
|------|---------|-----------|
| **AI** | 生成 C# 脚本骨架、生成基础 Prefab 结构、按需求更新节点绑定、跑 `Create UI Prefab` 工具 | 最终决定美术位置、颜色、字号、图片资源 |
| **人类** | 在 Prefab Mode 调整布局、提出业务逻辑需求、验收效果 | 不直接修改 `ScriptGenerator()` 里的节点路径 |

---

## 二、目录与命名规范

### 2.1 目录结构

```
Assets/AssetRaw/UI/
├── BattleMainUI/
│   └── BattleMainUI.prefab
├── MainMenuUI/
│   └── MainMenuUI.prefab
├── LobbyUI/
│   └── LobbyUI.prefab
├── WeaponWheelUI/
│   └── WeaponWheelUI.prefab
├── SniperScopeUI/
│   └── SniperScopeUI.prefab
├── DamageNumberUI/
│   └── DamageNumberUI.prefab
├── HitFeedbackUI/
│   └── HitFeedbackUI.prefab
├── LoadingUI/
│   └── LoadingUI.prefab
```

> 所有 UI Prefab 统一放在 `Assets/AssetRaw/UI/{UI名称}/{UI名称}.prefab`，不再直接放在 `Assets/AssetRaw/UI/` 根目录。
>
> ⚠️ YooAsset 的 `Assets/AssetRaw/UI` 收集器必须使用 `CollectPrefab` + `AddressByFileName`。若使用 `CollectAll`，文件夹资产和同名 Prefab 会产生重复地址（如 `BattleMainUI`），导致 `SimulateBuild` 失败。

### 2.2 热更脚本目录

```
Assets/GameScripts/HotFix/GameLogic/UI/{UI名称}/{UI名称}.cs
```

### 2.3 三者一致原则

| 项 | 规则 |
|----|------|
| 类名 | `public class BattleMainUI : UIWindow` |
| 特性 location | `[Window(UILayer.UI, location: "BattleMainUI")]` |
| Prefab 文件名 | `BattleMainUI.prefab` |

### 2.4 节点命名前缀

| 前缀 | 组件类型 |
|------|---------|
| `m_rect_` | RectTransform 容器 |
| `m_text_` | TextMeshProUGUI |
| `m_btn_` | Button |
| `m_img_` | Image |
| `m_raw_` | RawImage |
| `m_slider_` | Slider |
| `m_toggle_` | Toggle |
| `m_input_` | InputField |

禁止出现 `Text`、`Image`、`Button` 这种无意义命名。

---

## 三、新增 UI 的标准流程

### 步骤 1：人类提出需求

使用固定格式，例如：

```
UI 名称：PauseUI
层级：UILayer.UI（全屏面板）
是否需要独立 CanvasScaler：否（跟随 UIRoot 750×1334）
基础节点：
- m_rect_Content
  - m_text_Title（显示“暂停”）
  - m_btn_Resume（继续游戏）
  - m_btn_Settings（设置）
  - m_btn_Quit（返回主菜单）

逻辑需求：
1. 点击 m_btn_Resume 关闭 PauseUI
2. 点击 m_btn_Settings 打开 SettingsUI
3. 点击 m_btn_Quit 切换到 ProcedureMainMenu
```

### 步骤 2：AI 使用工具生成基础结构

使用菜单：

```
Tools/UI/Create UI Prefab
```

输入 UI 名称、层级、是否全屏、是否独立 CanvasScaler，点击生成。工具会自动创建：

1. `Assets/GameScripts/HotFix/GameLogic/UI/PauseUI/PauseUI.cs`
2. `Assets/AssetRaw/UI/PauseUI/PauseUI.prefab`

生成的脚本包含：

```csharp
[Window(UILayer.UI, location: "PauseUI", fullScreen: true)]
public class PauseUI : UIWindow
{
    private TextMeshProUGUI _textTitle;
    private Button _btnResume;
    private Button _btnSettings;
    private Button _btnQuit;

    protected override void ScriptGenerator()
    {
        _textTitle = FindChildComponent<TextMeshProUGUI>("m_rect_Content/m_text_Title");
        _btnResume = FindChildComponent<Button>("m_rect_Content/m_btn_Resume");
        _btnSettings = FindChildComponent<Button>("m_rect_Content/m_btn_Settings");
        _btnQuit = FindChildComponent<Button>("m_rect_Content/m_btn_Quit");
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        FixFullScreenCanvas();
        BindEvents();
    }

    private void BindEvents()
    {
        _btnResume?.onClick.AddListener(() => UIModule.Instance.CloseUI<PauseUI>());
        _btnSettings?.onClick.AddListener(() => GameModule.UI.ShowUIAsync<SettingsUI>());
        _btnQuit?.onClick.AddListener(() => GameApp.ChangeProcedure<ProcedureMainMenu>());
    }
}
```

### 步骤 3：人类调整 Prefab

在 **Prefab Mode** 打开 `PauseUI.prefab`，调整：

- 位置、大小、锚点
- 颜色、字体大小、图片
- 新增/删除/重命名节点

**如果调整了节点路径，必须回到步骤 1 的格式重新提出需求，由 AI 更新 `ScriptGenerator()`。**

### 步骤 4：AI 补充业务逻辑

根据人类提出的具体逻辑需求，AI 在 `PauseUI.cs` 里补全事件监听、数据刷新、动画控制等。

---

## 四、UI 独立性原则

### 4.1 每个 UI 独立

- 每个 UI 一个 Prefab 文件
- 每个 UI 一个 C# 类，继承 `UIWindow`
- 不允许一个类控制多个面板的节点
- 不允许通过 `GameObject.Find("OtherUI/xxx")` 跨 UI 访问节点

### 4.2 可以共享的内容

- 通用工具方法（`SetupText`、`GetUIFont` 等）
- 全局服务 UI：`LoadingUI`、`DamageNumberUI`、`HitFeedbackUI`
- 通用事件定义：`IPlayerEvent`、`IWeaponEvent`、`ICameraEvent` 等
- 未来可抽象的公共 UIWidget

### 4.3 UI 分类

| 类型 | 例子 | 打开者 |
|------|------|--------|
| Procedure 全屏面板 | MainMenuUI、LobbyUI、BattleMainUI | Procedure |
| 全局服务面板 | LoadingUI、DamageNumberUI、HitFeedbackUI | Procedure / GameModule |
| 弹窗/二级面板 | PauseUI、SettingsUI、TipUI | 当前活跃 UI / 系统 |

---

## 五、CanvasScaler 使用规则

| 面板类型 | 是否保留根 CanvasScaler | 原因 |
|---------|----------------------|------|
| 普通全屏 HUD / 菜单 | 否 | 使用 `UIRoot/UICanvas` 的 750×1334 缩放 |
| 需要独立设计分辨率的面板 | 是 | 如 SniperScopeUI、WeaponWheelUI 按 1920×1080 设计 |

生成工具会根据“是否需要独立 CanvasScaler”自动决定是否添加。

---

## 六、全屏面板必须做的事

所有全屏面板在 `OnCreate()` 中必须调用：

```csharp
FixFullScreenCanvas();
```

`FixFullScreenCanvas()` 必须包含：

```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
rt.sizeDelta = Vector2.zero;
```

否则实例化后面板会被 Unity 驱动成 `sizeDelta = 0`，导致 UI 不显示。

---

## 七、Text 方案

**当前阶段使用 UGUI Text（`UnityEngine.UI.Text`），不使用 TextMeshPro。**

原因：

- 项目当前已全部基于 UGUI Text
- HUD 类界面以数字和短文本为主，UGUI 够用
- 切换 TMP 需要批量替换所有 Text 节点和代码，改动大
- 等后续多语言或复杂字体效果需求明确后，再统一评估迁移

---

## 八、工具说明

### 8.1 现有工具

`Battle/Setup Battle Scene & Resources`：一键全量初始化战斗相关资源（玩家、敌人、所有 UI、场景等）。

### 8.2 新增轻量工具

`Tools/UI/Create UI Prefab`：只生成单个 UI 的脚本和 Prefab，不影响其他资源。

适用场景：

- 新增一个 UI 时
- 只需要重建某个 UI 的基础结构时
- 不想跑全量 `BattleSceneSetup` 时

### 8.3 什么时候跑全量工具

- 项目初始化
- 大量 Prefab 结构需要统一重建
- 改了 `BattleSceneSetup` 的公共构建逻辑后

---

## 九、人类提需求模板

```markdown
UI 名称：XXXUI
层级：UILayer.UI / System / Top
c是否全屏：是 / 否
是否需要独立 CanvasScaler：是 / 否（如否，跟随 UIRoot 750×1334）

基础节点：
- m_rect_Xxx
  - m_text_Xxx
  - m_btn_Xxx
  - ...

逻辑需求：
1. ...
2. ...
3. ...

参考/备注：
- 类似 BattleMainUI 的布局
- 按钮图片后续替换
```

---

## 十、Checklist

新增 / 修改 UI 时确认：

- [ ] Prefab 路径为 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`
- [ ] 脚本路径为 `Assets/GameScripts/HotFix/GameLogic/UI/{Name}/{Name}.cs`
- [ ] `[Window]` 的 `location` 与 Prefab 文件名一致
- [ ] 节点命名符合 `m_rect_` / `m_text_` / `m_btn_` 等前缀规范（`m_text_` 对应 TextMeshProUGUI）
- [ ] 新增 UI 后运行 `Battle/Setup Battle Scene & Resources`，确保 `SimulateBuild` 无地址冲突
- [ ] `ScriptGenerator()` 路径与 Prefab 实际路径一致
- [ ] 全屏面板在 `OnCreate()` 中调用 `FixFullScreenCanvas()`
- [ ] 修改热更代码后重新编译 `GameLogic.csproj`
- [ ] 纯 UI 场景里有 `tag = MainCamera` 且 `clearFlags = SolidColor` 的相机
- [ ] 2D 相机 zoom 使用 `orthographicSize`，不使用 `fieldOfView`

---

## 十一、相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Editor/UI/UIPrefabGenerator.cs` | 新增单个 UI 工具 |
| `Assets/Editor/BattleSetup/BattleSceneSetup.cs` | 全量初始化工具 |
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` | UI 窗口基类 |
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs` | UI 栈管理 |
| `Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs` | 场景加载基类 |
| `docs/notice/UI-and-Prefab-Pitfalls.md` | 已踩过的坑 |
