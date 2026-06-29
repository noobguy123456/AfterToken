# 05 UI / Prefab 工作流

## UI 技术选型

- **UI 框架**：TEngine `UIWindow` + `[Window(...)]` Attribute。
- **文字渲染**：TextMeshPro（TMP）已全量替换 uGUI Text。
- **异步**：所有 UI 加载接口均为 `UniTask`。

## 目录与命名约定

| 项 | 约定 |
|----|------|
| Prefab 路径 | `Assets/AssetRaw/UI/{UI名称}/{UI名称}.prefab` |
| 脚本路径 | `Assets/GameScripts/HotFix/GameLogic/UI/{UI名称}/{UI名称}.cs` |
| 类名 | `public class XXXUI : UIWindow` |
| 特性 | `[Window(UILayer.UI, location: "XXXUI", fullScreen = true)]` |

`location` 必须与 Prefab 文件名一致，YooAsset 按文件名寻址。

## 节点命名前缀

| 前缀 | 组件 |
|------|------|
| `m_rect_` | RectTransform / 布局容器 |
| `m_text_` | TextMeshProUGUI |
| `m_btn_` | Button |
| `m_img_` | Image |
| `m_raw_` | RawImage |
| `m_slider_` | Slider |
| `m_toggle_` | Toggle |
| `m_input_` | TMP_InputField |

## UI 层级

`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/WindowAttribute.cs`：

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

## UIWindow 生命周期

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

全屏面板必须在 `OnCreate()` 中调用：

```csharp
FixFullScreenCanvas();
```

`FixFullScreenCanvas` 会将根 `RectTransform` 设为全屏拉伸：

```csharp
rt.anchorMin = Vector2.zero;
rt.anchorMax = Vector2.one;
rt.offsetMin = Vector2.zero;
rt.offsetMax = Vector2.zero;
rt.anchoredPosition = Vector2.zero;
rt.sizeDelta = Vector2.zero;
rt.pivot = new Vector2(0.5f, 0.5f);
```

## UI 加载接口

`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs`：

```csharp
public void ShowUIAsync<T>(params object[] userDatas) where T : UIWindow, new()
public async UniTask<T> ShowUIAsyncAwait<T>(params object[] userDatas) where T : UIWindow, new()
public void CloseUI<T>()
public void CloseAll(bool isShutDown = false)
```

底层通过 `IUIResourceLoader` → `IResourceModule.LoadGameObjectAsync` 加载 Prefab。

## 默认字体

- TMP 默认字体资产路径：`Assets/AssetRaw/Fonts/MainUIFont.asset`
- 运行时若未配置 `TMP_Settings.defaultFontAsset`，会动态创建一份 Arial 字体资产兜底。
- 代码中动态创建 TMP 文本时，使用 `TMPFontProvider.DefaultFont`。

## 战斗 HUD 准星设计

战斗主界面 `BattleMainUI` 中的准星采用**动态生成 Sprite + MonoBehaviour 独立更新**的方案。

### 文件位置

- `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/BattleMainUI.cs`
- `Assets/GameScripts/HotFix/GameLogic/UI/BattleMainUI/CrosshairUpdater.cs`

### 准星样式

代码运行时动态绘制 4 种准星 Sprite，不依赖外部美术资源：

| 样式 | 枚举值 | 视觉效果 |
|------|--------|----------|
| 十字线 | `CrosshairStyle.Cross` | 默认，绿色十字 |
| 圆圈 | `CrosshairStyle.Circle` | 绿色空心圆 |
| T 型 | `CrosshairStyle.TShape` | 绿色 T 字 |
| 点 | `CrosshairStyle.Dot` | 绿色圆点 |

运行时按 `C` 键可循环切换样式。

### 位置跟随算法

```csharp
var parent = _crosshair.parent as RectTransform;
Camera cam = _canvas != null ? _canvas.worldCamera : null;

if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
    parent, Input.mousePosition, cam, out Vector2 localPos))
{
    _crosshair.anchoredPosition = localPos;
}
```

- 以准星父级 `RectTransform` 为参考。
- `ScreenPointToLocalPointInRectangle` 会自动处理 `CanvasScaler` 的缩放。
- 屏幕中心 `(Screen.width/2, Screen.height/2)` 转换后对应父级局部坐标 `(0, 0)`。

### 为什么需要 `CrosshairUpdater`

`CrosshairUpdater` 是一个挂载在准星节点上的普通 `MonoBehaviour`，专门负责每帧刷新准星位置与样式切换输入。把它从 `BattleMainUI.OnUpdate` 中拆出来的原因见下一节《UI 栈显隐规则》。

## UI 栈显隐规则

TEngine 的 UI 栈在 `UIModule.OnSetWindowVisible()` 中维护窗口显隐：

```csharp
private void OnSetWindowVisible()
{
    bool isHideNext = false;
    for (int i = _uiStack.Count - 1; i >= 0; i--)
    {
        UIWindow window = _uiStack[i];
        if (isHideNext == false)
        {
            if (window.IsHide) continue;
            window.Visible = true;
            if (window.IsPrepare && window.FullScreen)
            {
                isHideNext = true;
            }
        }
        else
        {
            window.Visible = false;
        }
    }
}
```

### 规则说明

- 从栈顶向下遍历。
- 遇到的第一个非隐藏窗口设为 `Visible = true`。
- 如果该窗口是**全屏窗口**（`FullScreen = true`），则从它开始往下的所有窗口都会被设为 `Visible = false`。
- `Visible = false` 的实现是把窗口根节点的 `layer` 改成 `WINDOW_HIDE_LAYER`（即 `Ignore Raycast`）。

### 为什么这样设计

1. **性能**：被全屏窗口完全遮挡的窗口不需要渲染、不需要接收事件，减少 Overdraw 和 Raycast 开销。
2. **输入隔离**：避免下方窗口的按钮被误触。
3. **符合直觉的窗口堆叠**：手机/PC 游戏中，全屏弹窗打开时，背后的界面通常不可交互。

### 关键机制补充

#### `Visible = false` 只改根节点 layer

`UIWindow.Visible` 的 setter 会把窗口根 `Canvas.gameObject` 的 `layer` 设为 `WINDOW_HIDE_LAYER`（`Ignore Raycast`，值 2），子 `Canvas` 也会同步修改，但**普通子节点不会递归改 layer**。

- 被隐藏的窗口 GameObject 仍然是 `active` 的，内存和独立 `MonoBehaviour.Update` 仍会运行。
- 如果 UI 结构复杂，部分子节点可能仍保持可见，但 `UIWindow.OnUpdate()` 已经停止，逻辑层处于“冻结”状态。
- 因此需要持续运行的逻辑必须拆到独立 `MonoBehaviour`（见 `CrosshairUpdater`、`DamageNumberUpdater`），而不是依赖 `UIWindow.OnUpdate`。

#### `OnUpdate` 停止条件

`UIModule.OnUpdate()` 每帧遍历 `_uiStack` 并调用 `UIWindow.InternalUpdate()`，而 `InternalUpdate()` 第一行就是：

```csharp
if (!IsPrepare || !Visible)
    return false;
```

所以 `OnUpdate()` 会在以下情况停止：

1. 窗口尚未加载完成（`!IsPrepare`）。
2. 窗口被隐藏（`Visible == false`）：被全屏窗口遮挡、被 `HideUI()`、或栈规则判定隐藏。
3. 窗口已关闭并从 `_uiStack` 移除。

#### 子窗口 / `UIWidget` 更新规则

`UIWidget` 的更新依赖父级 `UIBase` 的更新列表：

- 若 `OnUpdate()` 没有把 `_hasOverrideUpdate` 置为 `true`，该 Widget 会被父级移出更新列表，后续不再调用其 `OnUpdate()`。
- 需要恢复更新时，手动调用 `SetUpdateDirty()`。
- Widget 更新不检查自身 `Visible` / `gameObject.activeSelf`，只依赖所属 `UIWindow` 的 `Visible`。

#### `HideUI` 的默认超时

`WindowAttribute` 默认 `hideTimeToClose = 10` 秒。调用 `HideUI()` 后，若 10 秒内没有重新打开，窗口会被自动 `CloseUI()` 销毁。

若希望隐藏后长期保留，需显式设置 `hideTimeToClose = 0`，或避免使用 `HideUI()`。

#### 同层窗口数量上限

层级深度计算：`depth = layer * 2000 + index * 100`，同层间距 100。

- `Bottom`：0, 100, ...
- `UI`：2000, 2100, ...
- `Top`：4000, 4100, ...
- `Tips`：6000, ...
- `System`：8000, ...

同层超过 20 个窗口后，`sortingOrder` 会进入下一个 `UILayer` 的范围，可能导致渲染顺序混乱。

### 对后续 UI 开发的影响

1. **`OnUpdate` 会停止执行**
   
   `UIWindow.InternalUpdate()` 会在 `!IsPrepare || !Visible` 时直接返回，不再调用 `OnUpdate()`。因此：
   - 不要依赖 `UIWindow.OnUpdate()` 做**必须持续运行**的逻辑（例如准星跟随鼠标、倒计时动画、持续刷新数据）。
   - 如果确实需要持续更新，应把逻辑放到独立的 `MonoBehaviour`（如 `CrosshairUpdater`），或把窗口放到不会被遮挡的层级/顺序。

2. **`FullScreen = true` 具有传染性**
   
   只要栈中有一个全屏窗口在上方，它下方的所有窗口都会被隐藏。例如：
   - 打开 `BattleMainUI`（非全屏）
   - 打开 `DamageNumberUI`（非全屏）
   - 打开 `WeaponWheelUI`（全屏）
   - 结果：`WeaponWheelUI` 显示，`BattleMainUI` 和 `DamageNumberUI` 被隐藏。
   
   > 注意：`HitFeedbackUI` 曾是 `FullScreen = true`，导致战斗中常驻 HUD 被隐藏、武器轮盘/狙击镜无法显示，已修复为 `FullScreen = false`。

3. **层级和打开顺序共同决定显隐**
   
   `UILayer` 只影响 `sortingOrder`（渲染深度），不影响 `OnSetWindowVisible()` 的遍历顺序。遍历顺序是窗口**打开顺序**（即 `_uiStack` 的入栈顺序）。因此：
   - 后打开的窗口在上层。
   - 如果后打开的是全屏窗口，先打开的非全屏窗口会被隐藏。

4. **`Visible = false` 只改根节点 layer**
   
   TEngine 目前只把窗口根 `Canvas.gameObject` 的 `layer` 设为 `Ignore Raycast`，子节点不会递归修改。这会导致：
   - 子节点如果仍保持 `Default` 或 `UI` layer，可能仍然可见（例如 `BattleMainUI` 的文本子节点）。
   - 但 `UIWindow.OnUpdate()` 已经不执行，逻辑层处于“冻结”状态。
   - 设计 UI 时应避免依赖这种“半隐藏”状态，必要时手动递归设置子节点 layer，或把持续逻辑拆到独立 `MonoBehaviour`。

### 推荐做法

- 需要持续更新的视觉元素（准星、持续动画、全局提示）：使用独立 `MonoBehaviour` 或在最高层级（`UILayer.Top` / `UILayer.System`）打开。
- 普通 HUD：`FullScreen = false`，但要注意后续打开的全屏窗口会把它隐藏。
- 全屏弹窗：明确标记 `FullScreen = true`，并在关闭后由 `OnSetWindowVisible()` 自动恢复下方窗口显隐。

## 编辑器工具

| 工具 | 路径 | 菜单 | 用途 |
|------|------|------|------|
| `BattleSceneSetup` | `Assets/Editor/BattleSetup/BattleSceneSetup.cs` | `Battle/Setup Battle Scene & Resources` | 一键初始化/更新战斗 Prefab、场景、UI，并执行 SimulateBuild |
| `UIPrefabGenerator` | `Assets/Editor/UI/UIPrefabGenerator.cs` | `Tools/UI/Create UI Prefab` | 单个 UI 脚本 + Prefab 生成 |
| `TMPPrefabMigrator` | `Assets/Editor/TMPMigration/TMPPrefabMigrator.cs` | `Tools/Migration/Migrate UI Prefabs to TMP` | 将现有 UI Prefab 的 uGUI Text 迁移为 TextMeshProUGUI |

## 已修复的框架相关 Bug

### 1. 准星在鼠标移动时停止跟随

**原因**：`BattleMainUI` 不是全屏窗口，当上层打开全屏弹窗时被隐藏，`UIWindow.OnUpdate()` 停止执行，导致准星位置不再刷新。

**修复**：新增 `CrosshairUpdater : MonoBehaviour`，挂载在准星节点上，脱离 `UIWindow.OnUpdate()` 独立驱动。

### 2. 伤害数字不消失

**原因**：`DamageNumberUI` 的飘字淡出逻辑写在 `UIWindow.OnUpdate()` 中，当 `DamageNumberUI` 被上层全屏窗口遮挡导致 `Visible = false` 时，`OnUpdate()` 停止，数字永远停留在最后一帧。

**修复**：在 `DamageNumberUI` 面板上挂载内部类 `DamageNumberUpdater : MonoBehaviour`，在 `MonoBehaviour.Update()` 中驱动飘字动画，不再受 UI 栈显隐规则影响。

### 3. 玩家附近出现白圈

**原因**：`DamageNumberUI` 的模板节点 `m_text_Template` 在 Prefab 中默认 active，且 `DamageNumberUI` 代码原本引用 `TextMeshProUGUI` 而 Prefab 上是 `UnityEngine.UI.Text`，导致 `_textTemplate` 绑定失败，`InitializePool()` 未执行，模板节点保持显示，在屏幕中央显示一个白色的 "0"。

**修复**：
- `DamageNumberUI.cs` 中 `TextMeshProUGUI` 全部改为 `UnityEngine.UI.Text`，与 Prefab 匹配。
- `OnCreate()` 中兜底隐藏 `m_text_Template`。
- `DamageNumberUI.prefab` 中 `m_text_Template` 的 `m_IsActive` 改为 0。

### 4. `HitFeedbackUI` 误标全屏导致 HUD 被隐藏

**原因**：`HitFeedbackUI` 是常驻 HUD（命中标记 + 受击方向指示），却标记为 `[Window(UILayer.System, ..., fullScreen: true)]`，导致：
- 战斗中 `BattleMainUI`、`DamageNumberUI` 等下层窗口被隐藏；
- 打开 `WeaponWheelUI` / `SniperScopeUI` 时，由于 `HitFeedbackUI` 在 `System` 层且是 FullScreen，弹窗也会被隐藏。

**修复**：将 `HitFeedbackUI` 的 `fullScreen` 改为 `false`，保留 `UILayer.System` 以确保渲染在最上层。

## 后续 UI 开发检查清单

新增 UI 窗口前，请确认：

- [ ] 是否真正需要全屏独占？透明/半透明 HUD 不要标记 `FullScreen = true`。
- [ ] 该 UI 是否需要在被遮挡时继续运行动画/计时器？若是，使用独立 `MonoBehaviour`。
- [ ] 是否放在合适的 `UILayer`？
  - `Bottom`：场景底层 UI
  - `UI`：普通 HUD、主界面
  - `Top`：弹窗、轮盘、狙击镜
  - `Tips`：提示 Toast
  - `System`：Loading、全局受击反馈等最顶层元素
- [ ] 是否需要 `HideUI()` 长期隐藏？若是，设置 `hideTimeToClose = 0`。
- [ ] 资源路径 `location` 是否与 Prefab 文件名一致？
- [ ] 全屏窗口是否在 `OnCreate()` 中调用了 `FixFullScreenCanvas()`？

## 人机协作规则

详见 `../CoWork/UI-Prefab-CoWork-Workflow.md`：

- AI 负责：生成 C# 脚本骨架、生成基础 Prefab 结构、按需求更新节点绑定。
- 人类负责：在 Prefab Mode 调整布局、颜色、图片、提出逻辑需求。
- 人类**不直接修改** `ScriptGenerator()` 里的节点路径；改路径需重新提需求给 AI。
