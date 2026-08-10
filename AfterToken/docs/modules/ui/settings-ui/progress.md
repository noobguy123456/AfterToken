# Settings UI 进度

## 已完成
- [x] `SettingsUI` 面板（灵敏度滑块、狙击开镜模式开关、关闭按钮、暂停游戏）
- [x] 准星灵敏度实时调整与 `SensitivitySetting` 保存
- [x] 狙击开镜模式（Hold/Toggle）开关与 `SniperAimModeSetting` 保存（PlayerPrefs `SniperAimMode`，仅对狙击枪生效）
- [x] 开镜灵敏度独立滑块（2026-08-08）：新增 `m_slider_ScopeSensitivity` + `m_text_ScopeSensitivityValue`（prefab 复制现有灵敏度行布局），走 `SensitivitySetting.ScopedValue`（存档 `settings.scopeSensitivity`）；未初始化时跟随普通灵敏度，避免与普通灵敏度值域脱节
- [x] 打开设置时显示光标，关闭后隐藏光标

## 进行中
- [ ] 音量、画质、操作页签
- [ ] 与 `SettingsSystem` 持久化对接

## 待办
- [ ] BGM / SFX 音量滑块
- [ ] 画质设置（分辨率、帧率、特效质量）
- [ ] 操作设置（按键映射、辅助瞄准开关）
- [ ] 多页签布局与 prefab 调整

## 阻塞
- 等待 `AudioSystem` 落地（音量应用）。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
