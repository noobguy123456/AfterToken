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
| 特性 | `[Window(UILayer.UI, location: "XXXUI", fullScreen: true)]` |

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

## 编辑器工具

| 工具 | 路径 | 菜单 | 用途 |
|------|------|------|------|
| `BattleSceneSetup` | `Assets/Editor/BattleSetup/BattleSceneSetup.cs` | `Battle/Setup Battle Scene & Resources` | 一键初始化/更新战斗 Prefab、场景、UI，并执行 SimulateBuild |
| `UIPrefabGenerator` | `Assets/Editor/UI/UIPrefabGenerator.cs` | `Tools/UI/Create UI Prefab` | 单个 UI 脚本 + Prefab 生成 |
| `TMPPrefabMigrator` | `Assets/Editor/TMPMigration/TMPPrefabMigrator.cs` | `Tools/Migration/Migrate UI Prefabs to TMP` | 将现有 UI Prefab 的 uGUI Text 迁移为 TextMeshProUGUI |

## 人机协作规则

详见 `../CoWork/UI-Prefab-CoWork-Workflow.md`：

- AI 负责：生成 C# 脚本骨架、生成基础 Prefab 结构、按需求更新节点绑定。
- 人类负责：在 Prefab Mode 调整布局、颜色、图片、提出逻辑需求。
- 人类**不直接修改** `ScriptGenerator()` 里的节点路径；改路径需重新提需求给 AI。

常见坑点参考 `../notice/UI-and-Prefab-Pitfalls.md`。
