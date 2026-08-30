# Settings UI 进度

## 已完成
- [x] `SettingsUI` 面板（灵敏度滑块、狙击开镜模式开关、关闭按钮、暂停游戏）
- [x] 准星灵敏度实时调整与 `SensitivitySetting` 保存
- [x] 狙击开镜模式（Hold/Toggle）开关与 `SniperAimModeSetting` 保存（PlayerPrefs `SniperAimMode`，仅对狙击枪生效）
- [x] 开镜灵敏度独立滑块（2026-08-08）：新增 `m_slider_ScopeSensitivity` + `m_text_ScopeSensitivityValue`（prefab 复制现有灵敏度行布局），走 `SensitivitySetting.ScopedValue`（存档 `settings.scopeSensitivity`）；未初始化时跟随普通灵敏度，避免与普通灵敏度值域脱节
- [x] 打开设置时显示光标，关闭后隐藏光标
- [x] 页签机制 + Input 按键改绑页签（2026-08-23）：prefab 新增 `m_btn_TabGeneral`/`m_btn_TabInput` 页签按钮与 `m_panel_General`/`m_panel_Input` 双面板（原控件移入 General 面板）；Input 页签运行时按 `KeyBindAction` 枚举克隆 `m_rect_BindingRowTemplate` 生成改绑行（动作名 + 当前键位按钮），点击按钮进入按键捕获（`OnUpdate` 轮询，ESC 取消、冲突拒绝并提示），`m_btn_ResetBindings` 一键恢复默认；捕获期间 `SettingsUI.IsCapturingKey=true` 屏蔽 InputSystem 的 ESC 关 UI 逻辑。Play 验证：8 行默认键位正确、页签切换、SetKey/Reset 持久化均通过
- [x] 准星样式/颜色设置（2026-08-23）：General 页签新增 `m_btn_CrosshairStyle`（点击循环 Dot/Cross/Circle/TShape，按钮文本显示当前样式）与 6 个预设色板按钮（`m_btn_Color0..5`：白/绿/红/青/黄/品红）；`CrosshairSetting`（`Module/SettingModule/CrosshairSetting.cs`）变动即存 + `OnChanged` 事件广播，`BattleMainUI` 订阅后即时刷新（准星精灵改为白色烘焙、由 Image.color 染色）；C 键循环样式与设置按钮共用 `CycleStyle()` 且都会写档；Close 按钮挪到右上角腾出布局空间。Play 验证：样式/颜色实时生效并写入存档
- [x] 准星样式可视化预览 + 改绑左键同帧防抖（2026-08-23）：准星精灵生成逻辑抽取为 `CrosshairSpriteFactory`（`UI/BattleMainUI/CrosshairSpriteFactory.cs`，静态 `Create(style,size,thickness)`），`BattleMainUI` 删除 5 个 Create*Sprite 实例方法改为调用工厂；General 页签样式按钮旁新增 `m_img_CrosshairPreview`（64x64），`SettingsUI.UpdateCrosshairViews()` 订阅 `CrosshairSetting.OnChanged` 统一刷新样式名文本与预览图（旧预览精灵连同贴图一起销毁防泄漏）。修复改绑 bug：绑定鼠标键时"按下完成绑定、同帧松开落在按钮上又触发 onClick 再进捕获"的死循环——`StartCapture` 开头加同帧防抖（`_captureEndFrame == Time.frameCount` 直接 return），`EndCapture`/`CancelCapture` 记录结束帧。Play 验证：预览图渲染正确、样式循环/色板换色即时同步、防抖拦截同帧重复捕获且不影响正常捕获
- [x] 预览图加衬底 + 设置面板屏蔽开火穿透（2026-08-23）：用户反馈"设置里同时出现两种鼠标"——截图定位为预览图外形=准星且无衬底被误读成游戏光标，已加 `m_img_CrosshairPreviewBg` 深灰衬底板（84x84，排在预览下层）使其读作色板；同时修复设置面板点击穿透：`IsMenuUIOpen()` 纳入 `SettingsUI`，点面板按钮不再触发开火；`CrosshairUpdater` 战斗态每帧断言光标隐藏+锁定，缓解编辑器下关 UI 后 OS 鼠标残留（编辑器需 Game 视图聚焦才完全生效，打包无此问题）
- [x] 返回主界面按钮（2026-08-30）：General 页签新增 `m_btn_ReturnMainMenu`（prefab 克隆 m_btn_Close 上移一行，文本 "Main Menu"）；点击走 `GameApp.ChangeProcedure<ProcedureMainMenu>()`（内部 CloseAll + `GamePauseManager.Reset()`，无需手动关设置/复位时间）；已在主菜单打开设置时仅关闭窗口（新增 `GameApp.IsCurrentProcedure<T>()` 判定），避免流程无意义重进。编译 0 错误，待 Play 验收

## 进行中
- [ ] 音量、画质页签
- [ ] 与 `SettingsSystem` 持久化对接

## 待办
- [ ] BGM / SFX 音量滑块
- [ ] 画质设置（分辨率、帧率、特效质量）
- [ ] 辅助瞄准开关

## 阻塞
- 等待 `AudioSystem` 落地（音量应用）。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
