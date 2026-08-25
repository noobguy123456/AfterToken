# Minimap System 进度

## 已完成
- [x] `MinimapSystem`（正交俯视相机 + 512×512 RenderTexture + LateUpdate 跟随玩家 + TryWorldToMap 投影），`ProcedureBattle.InitializeBattleSystems` 挂载
- [x] `MinimapUI` 窗口（UILayer.UI、fullScreen:false，220×220 右下角面板；红点对象池按 EnemyRegistry.All 逐帧刷新），`ProcedureBattle` 入场流程在 BattleMainUI 后打开
- [x] `MinimapUI.prefab`（`Assets/AssetRaw/UI/MinimapUI/`，按 InteractionPromptUI 根结构手写：根 Canvas + 底衬 Panel + RawImage + IconRoot + 红点模板 + 玩家中心标记）

## 进行中
（无）

## 已验证
- [x] Play 验证（2026-08-24，101 关）：烘焙 ready、uvRect 观察窗口随玩家居中、右上角面板显示地形/敌人红点/玩家绿标；开镜窗口在栈中时 BattleMainUI 与 MinimapUI 均保持可见、小地图不变灰（Tips 层压过狙击镜蒙版）。真实右键开镜流程的准星隐藏配对待用户实测

## 变更记录

| 日期 | 变更内容 |
|------|----------|
| 2026-08-25 | M 键大地图模式：`KeyBindAction.Map`（默认 M，纳入设置 Input 页签改绑）；`MinimapUI.SetBigMapMode` 切换右上角 220² 小图 ↔ 屏幕居中 636² 大地图，观察窗口 45% ↔ 整幅地图；`IsBigMapOpen` 计入 `InputSystem.IsMenuUIOpen`（开图屏蔽射击/瞄准/冻结准星），InputSystem 拆出 `IsWindowMenuOpen()`（仅四窗口）供小地图自身判断压盖、防循环依赖；其它菜单打开自动退出大地图；`OnDestroy` 复位静态状态。Play 验证：开→居中整图+输入屏蔽，关→布局恢复+屏蔽解除 |
| 2026-08-24 | 菜单遮挡修复 + 平面化烘焙：①MinimapUI.OnUpdate 监听 `InputSystem.IsMenuUIOpen()`（背包/开箱/纸条/设置），菜单打开时隐藏整个小地图面板（保留 Tips 层压狙击镜蒙版，菜单与开镜互不冲突），设置面板 Close 按钮不再被挡；②烘焙改用内置 `Unlit/Color` 替换 shader（`SetReplacementShader` 渲一次后 Reset），材质按 _Color 平色输出、无光影阴影，呈真地图平面感——**打包需把 Unlit/Color 加入 Graphics Settings → Always Included Shaders**。Play 验证：设置开→小地图隐藏 Close 可点，关→恢复；小地图纯色平面无光影；开镜小地图不变灰 |
| 2026-08-24 | 返工为传统 2D 平面缩略图方案（用户反馈：①跟随相机露场景外 ②开镜被灰色蒙版压灰 ③不应实时渲 3D 画面，参考 Apex/三角洲行动）：MinimapSystem 从逐帧跟随渲染改为入场一次性烘焙整图（`BattleBoundary.Bounds` 定范围，`Camera.Render()` 后停用，新增 `BattleBoundary.Bounds` 属性）；MinimapUI 改 uvRect 观察窗口平移（45% 视野、钳制在 `MapUvMin/Max` 内不露图外），玩家/敌人图标按窗口投影、玩家近边缘不再居中；层级 UI→Tips 压过狙击镜蒙版 |
| 2026-08-24 | 修复小地图空白：`FindChildComponent` 走 `transform.Find` 精确相对路径，ScriptGenerator 里 `"m_raw_Map"` 等漏了 `m_rect_Panel/` 前缀导致绑定全 null（地图/红点都不显示）；面板从右下角挪到右上角（anchor/pivot 改 (1,1)，偏移 (-16,-16)） |
