# Simulation Ui 进度

## 已完成
- [x] SimulationMainUI（2026-08-01 重设计）
  - 顶部常驻 HUD 条：金币 / 等级 / 时间 / 暂停·1x·2x / `[Tab] Panel` 提示
  - 管理面板默认隐藏，Tab 切换：建筑列表 + 订单列表 + Build / Upgrade / Back to Menu
  - 渲染：**框架 UIRoot（Screen Space - Overlay）下**（2026-08-01 统一，见下「渲染架构统一」）；`SimulationUIRoot` 为纯 RectTransform 容器（无 Canvas），按设计分辨率 1920x1080 固定尺寸 + 反向缩放抵消框架 CanvasScaler（750x1334 按宽适配）影响，正式 Prefab 化后移除
  - 滚动列表裁剪用 `RectMask2D`（`Mask` 模板缓冲在 SSC 渲染路径下会导致内容全不可见）
- [x] 建筑头顶牌子：World Space（`BuildingEntity.CreateLabel`）
- [x] UI 渲染架构统一（2026-08-01，`docs/Proposal/ui/ui-render-architecture.md`）：
  - 拆除旧 `SimulationUIRoot` 自建 SSC Canvas（挂 Main Camera 透视相机导致面板倾斜/陷地），经营 UI 回归框架 UIRoot（Overlay）
  - `UIRoot.prefab` 的 UICanvas 由 SSC 对齐改为 Overlay；UICamera 设 inactive 备用（未来 UI 层 3D 特效时启用并锚定远区）
  - 战斗/经营场景相机 `cullingMask` 剔除 UI 层（防未来特效物体被场景相机透视重渲）
  - Play Mode 实测：主菜单/经营（含移动至 z=-24 边界）/战斗 HUD 与世界空间元素均正常，Console 0 报错
- [x] 修复经营 UI 全部无法点击（2026-08-02）：根因——`SimulationMainUI`/`BuildingSelectionUI` 共用占位 `TestUI.prefab`，其带 Canvas + 全屏半透明 Image 但**无 GraphicRaycaster**；窗口图形注册在窗口自身 Canvas 下，框架 UICanvas 的 Raycaster 管不到，全窗口射线 0 命中。修复：`TestUI.prefab` 补挂 `GraphicRaycaster`（所有 TestUI 占位窗口一并修复）。实测：Build 按钮点击打开选择 UI → 点选建筑进入摆放模式，链路全通
- 注意：框架 `UIWindow.OnPrepare` 要求每个窗口面板**自带 Canvas + GraphicRaycaster**，后续代码动态创建窗口若换新占位 Prefab 必须带上这两个组件
- [x] 建造交互统一为"点击后玩家自己放置"（2026-08-02）：管理面板建筑列表项原点击即在原点 `TryBuild`（无预览无选址），改为 `EnterPlacement`——关闭面板 + `BuildingPlacementSystem.StartPlacement`，与 `BuildingSelectionUI` 点选行为一致；同时修复建筑 `icon` 占位地址导致的资源加载 ERROR（`BuildingEntity` 加载前 `CheckLocationValid` 前置校验，无效地址直接走占位模型）
- [x] 三个经营窗口 Prefab 化（2026-08-03）：`SimulationMainUI`/`BuildingSelectionUI`/`BuildingInfoUI` 从"TestUI 占位 + 代码拼装"迁移为正式 Prefab（`Assets/AssetRaw/UI/{Name}/{Name}.prefab`，由一次性编辑器工具生成后已删除，Prefab 为唯一事实来源）；脚本改为 `ScriptGenerator()` 绑定节点（HUD/面板/按钮/滚动列表静态结构全在 Prefab，建筑/订单/配方**列表项仍运行时生成**，属正常模式）；1920x1080 容器 + 反向缩放保留在 OnCreate（根 CanvasScaler 横屏修正后移除）；Play 实测：HUD、Management 面板（含 Unlock 按钮/提升条件行）、Select Building、信息面板升级失败红字全部正常
- [x] 面向用户文本全部改英文（2026-08-03 用户决策，恢复中文需用户明确许可）：`CanBuild`/`CanUpgrade`/`TryPurchaseSlot` 失败原因、解锁失败默认原因等全部英文化；`BuildingInfoUI` 失败红字由 legacy Text 改回 TMP（英文无需中文字体兜底）；规则已写入 `docs/standards/UI_STANDARDS.md` §1

## 待办
- [ ] BuildingWidget / ResourceWidget / OrderWidget（列表项拆分为正式 Prefab 组件；窗口本体已 Prefab 化 2026-08-03）
- [ ] UI 特效接入（首选序列帧在 Overlay 内播放；3D 粒子需求再启用 UICamera 特效层，待美术资源）
- [ ] CanvasScaler 参考分辨率 750x1334（竖屏默认）→ 1920x1080 横屏（单独做，影响全部 UI 缩放）

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
