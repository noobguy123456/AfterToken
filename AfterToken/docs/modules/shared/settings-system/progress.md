# Settings System 进度

## 已完成
- [x] `SensitivitySetting` 准星灵敏度（运行时可用）
- [x] `SniperAimModeSetting` 狙击开镜模式（Hold/Toggle）
- [x] `SettingsUI` 灵敏度滑块 + 狙击开镜模式开关 + 关闭按钮
- [x] 设置项从 `PlayerPrefs` 迁移到 `SaveSystem`（2026-08-06，旧 PlayerPrefs 值首次读取时一次性导入；`Save()` 保留空实现兼容面板调用）
- [x] `VolumeSetting` 主音量/音乐/音效（2026-09-06）：写 SaveSystem 并即时应用 `GameModule.Audio`（Master→AudioListener.volume，Music/Sound→AudioMixer 的 MusicVolume/SoundVolume 暴露参数，已验证 mixer 参数可用）；旧框架层 `Setting.MusicVolume`/`Setting.SoundVolume` PlayerPrefs 一次性迁移导入
- [x] `QualitySetting` 画质档位（2026-09-06）：写 SaveSystem 并即时应用 `QualitySettings.SetQualityLevel`；无存档时以引擎当前档位为默认
- [x] `SettingsUI` 四页签结构（2026-09-06）：General/Audio/Graphics/Input，`ShowTab(SettingsTab)` 枚举切换；Audio 页签三条音量滑块、Graphics 页签左右箭头+当前档位文本循环切换
- [x] 启动读回：`GameApp.EntranceAsync`（LocalizationSystem 初始化后）调 `VolumeSetting.ApplyAll()` + `QualitySetting.Apply()`；`SaveSystem.SwitchSlot` 补两个新设置类的 `InvalidateCache`
- [x] Play 实测（2026-09-06）：页签切换/滑条/画质切换生效且 AudioModule、QualitySettings 实际值同步；save_1.json 写入 masterVolume/musicVolume/soundVolume/qualityLevel；重启 Play 后全部读回应用，Console 0 error

## 进行中
- 无

## 待办
- [ ] `SettingsSystem` 统一入口（目前各设置项独立静态类）
- [ ] UI 音效（UISoundVolume）页签项（AudioMixer UISoundVolume 参数已暴露，按需补）
- [ ] 与 `CameraSystem` FOV 联动

## 阻塞
- 无（音量链路经 TEngine AudioModule 已打通，不再依赖独立 AudioSystem）。

---

> 状态说明：
> - 当前总状态：✅（音量/画质/操作/准星均已持久化并实测）
> - 每次更新后同步 `docs/TODO.md`
