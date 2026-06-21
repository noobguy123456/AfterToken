# AfterToken UI / Prefab 避坑指南

> **适用场景**：在 AfterToken 项目中使用 TEngine + YooAsset + Hotfix 模式开发 UI、生成或修改 Prefab、切换场景时出现界面不显示、显示错位、残留旧 UI、黑屏等问题。
>
> **相关路径**：
> - 热更 UI 代码：`Assets/GameScripts/HotFix/GameLogic/UI/`
> - UI 框架扩展：`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/`
> - UI Prefab 资源：`Assets/AssetRaw/UI/`
> - 场景资源：`Assets/AssetRaw/Scenes/`
> - 一键初始化工具：`Assets/Editor/BattleSetup/BattleSceneSetup.cs`

---

## 1. UI 面板加载后“消失”或只显示一小部分

### 现象

- 打开 UI 后 Game View 黑屏，或只有某个角落有一点点东西。
- `RectTransform.sizeDelta` 被驱动成 `(0, 0)`，子节点被挤到屏幕外。

### 根因

TEngine 的 `UIWindow` 会把 Prefab 实例挂到 `UIRoot/UICanvas` 下。很多 UI Prefab 的根节点在 Prefab 模式下是“根 Canvas”，其 `RectTransform` 会被 Unity 驱动成 `anchorMin/Max = (0,0)`、`sizeDelta = (0,0)`。实例化后如果不手动纠正，面板就会塌缩。

### 正确做法

在 `OnCreate()` 中调用 `FixFullScreenCanvas()` 时，必须显式把根 `RectTransform` 设成全屏拉伸：

```csharp
var rt = rectTransform;
if (rt != null)
{
    rt.localScale = Vector3.one;
    rt.anchorMin = Vector2.zero;
    rt.anchorMax = Vector2.one;
    rt.offsetMin = Vector2.zero;
    rt.offsetMax = Vector2.zero;
    rt.anchoredPosition = Vector2.zero;
    rt.sizeDelta = Vector2.zero;
    rt.pivot = new Vector2(0.5f, 0.5f);
}
```

> ⚠️ 只设 `sizeDelta = Vector2.zero` 不够，必须同时设 `anchorMin/Max`。

### 参考实现

`Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` 中的 `FixFullScreenCanvas()`。

---

## 2. Prefab 里残留旧 Demo / 模板节点

### 现象

- 战斗场景出现“HP/MP/Armor/EXP 条 + 世界 BOSS 按钮 + 快开始做游戏吧”这种看起来像是框架默认 UI 的内容。
- 实际上它是旧版 `BattleMainUI.prefab` 里残留的 Demo 节点，新的逻辑脚本只更新了其中一部分，其余节点原封不动地显示在画面上。

### 根因

`BattleSceneSetup.EnsureOrUpdatePrefab` 是**增量更新**：已存在的 Prefab 会被实例化后修改，再 `SaveAsPrefabAsset`。它不会主动删除已经存在的旧节点。如果 Prefab 结构发生大改，必须手动清理。

### 正确做法

在需要“重建结构”的 Prefab 构建函数里，先清空所有子节点：

```csharp
private static void DestroyAllChildren(GameObject parent)
{
    while (parent.transform.childCount > 0)
    {
        UnityEngine.Object.DestroyImmediate(parent.transform.GetChild(0).gameObject);
    }
}
```

示例（`CreateBattleMainUIPrefab`）：

```csharp
EnsureOrUpdatePrefab("Assets/AssetRaw/UI/BattleMainUI.prefab", go =>
{
    var canvas = go.EnsureComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 10;
    go.EnsureComponent<GraphicRaycaster>();

    // 关键：先清掉旧版 Demo UI 遗留节点
    DestroyAllChildren(go);
    var scaler = go.GetComponent<CanvasScaler>();
    if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);

    SetupFullScreenRect(go.EnsureComponent<RectTransform>());

    // 再重新创建当前需要的节点
    ...
});
```

### 建议

如果 Prefab 结构已经混乱，**直接删除 Prefab 文件再跑 `Battle/Setup Battle Scene & Resources`**，让工具重新生成，比手动修更快。

---

## 3. 嵌套 Canvas 与 CanvasScaler 的取舍

### 现象

- UI 在全屏面板里显示比例异常，或某些子面板被缩放得特别大 / 特别小。
- `RectTransform` 被驱动到 0，或位置对不上设计分辨率。

### 原则

| 面板类型 | 是否保留根 CanvasScaler | 说明 |
|---------|----------------------|------|
| 全屏 HUD（如 `BattleMainUI`） | 不保留 | 使用 `UIRoot/UICanvas` 的 `CanvasScaler`（参考 750×1334），避免多层缩放冲突 |
| 独立分辨率面板（如 `SniperScopeUI`、`WeaponWheelUI`） | 保留 | 需要 1920×1080 等设计分辨率时，根节点保留自己的 `CanvasScaler` |

### 正确做法

- 不要一刀切地销毁所有 `CanvasScaler`。`FixFullScreenCanvas()` 只调整 `RectTransform`，不再删除 `CanvasScaler`。
- 在 `BattleSceneSetup` 里为不同面板显式决定是否添加 `CanvasScaler`。

---

## 4. 2D 正交相机不要用 `fieldOfView`

### 现象

- 瞄准、狙击时相机 zoom 完全没效果。
- 或狙击镜相机渲染出来的画面比例不对。

### 根因

`Camera.fieldOfView` 只对透视相机有效。2D 项目使用的相机通常是 `orthographic = true`，需要修改 `orthographicSize`。

### 正确做法

```csharp
if (_mainCamera.orthographic)
{
    float targetSize = _defaultOrthographicSize * (_targetFov / _defaultFov);
    _mainCamera.orthographicSize = Mathf.SmoothDamp(
        _mainCamera.orthographicSize,
        targetSize,
        ref _currentOrthographicSizeVelocity,
        _fovSmoothTime);
}
else
{
    _mainCamera.fieldOfView = Mathf.SmoothDamp(...);
}
```

狙击镜相机拷贝自主相机后，也要同步设置 `orthographicSize`：

```csharp
_scopeCamera.orthographicSize = _defaultOrthographicSize * (25f / _defaultFov);
```

### 参考实现

`Assets/GameScripts/HotFix/GameLogic/System/CameraSystem.cs`。

---

## 5. UI 场景没有主相机导致黑屏 / 截图截不到 UI

### 现象

- Game View 全黑，但 UI 对象实际存在。
- `ScreenCapture` 或截图工具只能拍到黑色。

### 根因

`MainMenuScene`、`LobbyScene` 等纯 UI 场景如果没有 `Main Camera`，颜色缓冲不会被清除，Overlay UI 虽然仍会渲染，但很多截图/显示路径会表现为黑屏。

### 正确做法

所有被加载的场景都必须至少有一个 `Main Camera`：

- `tag = "MainCamera"`
- `clearFlags = CameraClearFlags.SolidColor`
- `backgroundColor = Color.black`（或项目统一的背景色）
- `orthographic = true`

如果场景已存在但没有相机，可以用编辑器工具补齐：

```csharp
private static void EnsureSceneHasMainCamera(string path)
{
    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    var camGo = GameObject.FindWithTag("MainCamera");
    if (camGo == null)
    {
        camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.transform.position = new Vector3(0, 0, -10);
    }
    EditorSceneManager.SaveScene(scene);
}
```

---

## 6. TMP 字体资产缺失导致文字不显示

### 现象

LoadingUI / 动态生成的文本框里没有文字，但对象存在；或控制台出现 TMP 相关 NullReference。

### 根因

项目已统一使用 `TextMeshProUGUI`。TMP 文本必须关联 `TMP_FontAsset`，若字体资产未导入或被意外清除，文本无法渲染。

### 正确做法

1. 确保存在默认字体资产：`Assets/AssetRaw/Fonts/MainUIFont.asset`。
2. 代码中动态创建 TMP 文本时，使用 `TMPFontProvider.DefaultFont`：

```csharp
_progressText.font = TMPFontProvider.DefaultFont;
```

3. 批量迁移工具：`Tools/Migration/Migrate UI Prefabs to TMP`。
4. 当前所有 UI 文本统一使用英文，避免中文字体兼容性问题。

---

## 7. UI Prefab 目录与 YooAsset 地址冲突

### 现象

移动 `BattleMainUI.prefab` 到 `Assets/AssetRaw/UI/BattleMainUI/BattleMainUI.prefab` 后，YooAsset 模拟构建报错：

```
System.Exception: The address is existed : BattleMainUI in collector : Assets/AssetRaw/UI
```

### 根因

YooAsset `Assets/AssetRaw/UI` 收集器原来使用 `CollectAll` + `AddressByFileName`。`CollectAll` 会同时收集文件夹资产和 Prefab 资产，而文件夹 `BattleMainUI` 与 Prefab `BattleMainUI.prefab` 会被解析成同一个地址 `BattleMainUI`，导致地址重复。

### 正确做法

UI 收集器只收集 Prefab，不对文件夹资产寻址：

| 配置项 | 值 | 说明 |
|--------|-----|------|
| `FilterRule` | `CollectPrefab` | 只收集 `.prefab` 文件，排除文件夹资产 |
| `PackRule` | `PackSeparately` | 每个 UI Prefab 独立打包，避免单个 bundle 过大 |
| `AddressRule` | `AddressByFileName` | 用 Prefab 文件名作为加载地址，保持与 `[Window(location)]` 一致 |

配置位置：
- `Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset`
- `Assets/Editor/AssetBundleCollector/AssetBundleCollectorConfig.xml`

### 建议

- 保持目录约定 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`。
- 不要同时保留同名文件和同名文件夹在 `Assets/AssetRaw/UI` 下。
- 新增 UI 后若改到 YooAsset 配置，要重新执行 `Battle/Setup Battle Scene & Resources` 让 `SimulateBuild` 生效。

---

## 8. UI 加载与场景加载流程速查

### UI 加载

```csharp
await GameModule.UI.ShowUIAsyncAwait<BattleMainUI>();
```

1. `UIModule.CreateInstance<T>()` 读取 `[Window]` 特性，拿到 `location` 和 `UILayer`。
2. `UIWindow.InternalLoad` 通过 YooAsset 加载 `Assets/AssetRaw/UI/{location}.prefab`。
3. 实例挂到 `UIRoot/UICanvas` 下。
4. `ScriptGenerator()` 绑定节点，`RegisterEvent()` 注册事件，`OnCreate()` 做最终初始化。
5. `OnSortWindowDepth` 设置 `sortingOrder`，`OnSetWindowVisible` 控制谁显示谁隐藏。

### 场景加载

```csharp
LoadSceneWithLoadingAsync("BattleScene", async ct =>
{
    // 初始化战斗系统
    await GameModule.UI.ShowUIAsyncAwait<BattleMainUI>();
});
```

1. 显示 `LoadingUI`。
2. `GameModule.Scene.LoadSceneAsync(sceneName)` 加载场景。
3. 关闭 `LoadingUI`。
4. 执行回调，打开该场景的 UI。

### 切换流程

```csharp
GameApp.ChangeProcedure<ProcedureBattle>();
```

内部先 `GameModule.UI.CloseAll()`，再反射调用 TEngine FSM 的 `ChangeState<T>`。

---

## 9. 开发 Checklist

新增 / 修改 UI 时，逐项确认：

- [ ] Prefab 放在 `Assets/AssetRaw/UI/`，不要放 `Assets/Resources/`。
- [ ] 热更脚本里的 `[Window]` 特性的 `location` 和 Prefab 文件名一致。
- [ ] `ScriptGenerator()` 里绑定的节点路径和 Prefab 节点路径完全一致。
- [ ] `OnCreate()` 里调用了 `FixFullScreenCanvas()`（全屏窗口）。
- [ ] 如果 Prefab 结构大改，运行 `Battle/Setup Battle Scene & Resources`，必要时先删旧 Prefab。
- [ ] 纯 UI 场景里要有 `tag = MainCamera` 且 `clearFlags = SolidColor` 的相机。
- [ ] 修改热更代码后，重新编译热更 DLL（`GameLogic.csproj` / HybridCLR 构建）。
- [ ] 2D 相机相关的 zoom 逻辑用 `orthographicSize`，不要只改 `fieldOfView`。

---

## 10. 相关文件索引

| 文件 | 作用 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIWindow.cs` | UI 窗口基类，`FixFullScreenCanvas` |
| `Assets/GameScripts/HotFix/GameLogic/Module/UIModule/UIModule.cs` | UI 栈管理、加载入口 |
| `Assets/GameScripts/HotFix/GameLogic/System/CameraSystem.cs` | 正交相机 zoom / 狙击镜 |
| `Assets/Editor/BattleSetup/BattleSceneSetup.cs` | Prefab / 场景一键初始化工具 |
| `Assets/GameScripts/HotFix/GameLogic/Procedure/GameplayProcedureBase.cs` | 带 Loading 的场景加载基类 |
| `Assets/GameScripts/HotFix/GameLogic/GameApp.cs` | 流程切换入口 |

---

## 11. 一句话总结

> **Prefab 增量更新会留旧节点，全屏 UI 必须设 anchor 拉伸，2D 相机调 orthographicSize，纯 UI 场景不能缺主相机。**
