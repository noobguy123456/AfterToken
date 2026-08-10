# Settings System 进度

## 已完成
- [x] `SensitivitySetting` 准星灵敏度（运行时可用）
- [x] `SniperAimModeSetting` 狙击开镜模式（Hold/Toggle）
- [x] `SettingsUI` 灵敏度滑块 + 狙击开镜模式开关 + 关闭按钮
- [x] 设置项从 `PlayerPrefs` 迁移到 `SaveSystem`（2026-08-06，旧 PlayerPrefs 值首次读取时一次性导入；`Save()` 保留空实现兼容面板调用）

## 进行中
- [ ] 音量、画质、操作设置 UI 页签

## 待办
- [ ] `SettingsSystem` 统一入口（目前各设置项独立静态类）
- [ ] 与 `AudioSystem` 音量联动
- [ ] 与 `CameraSystem` 画质/FOV 联动

## 阻塞
- 等待 `AudioSystem` 落地（音量应用）。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
