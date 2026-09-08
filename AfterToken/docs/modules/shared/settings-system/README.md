# Settings System

## 职责

负责玩家设置的持久化与运行时读取：
- 音量（主音量、音乐、音效）——已实现
- 画质（QualitySettings 档位）——已实现
- 操作（灵敏度、按键映射、狙击开镜模式）——已实现
- 其他（准星样式/颜色、语言）

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `Module/SettingModule/SensitivitySetting` | 准星/开镜灵敏度 |
| `Module/SettingModule/SniperAimModeSetting` | 狙击开镜模式（Hold/Toggle） |
| `Module/SettingModule/KeyBindingSetting` | 按键改绑 |
| `Module/SettingModule/CrosshairSetting` | 准星样式/颜色 |
| `Module/SettingModule/VolumeSetting` | 主/音乐/音效音量，写入即应用 `GameModule.Audio` 并落盘 |
| `Module/SettingModule/QualitySetting` | 画质档位，写入即应用 `QualitySettings` 并落盘 |
| `UI/SettingsUI/SettingsUI.cs` | 设置面板，四页签 General/Audio/Graphics/Input |

## 设置项统一模式

各设置项为 static 类：读穿透缓存 + setter 写 `SaveSystem.Data.settings.<field>`（带 `xxxInitialized` 标记区分"无存档"）+ `SaveSystem.Flush()` 变动即存 + `InvalidateCache()` 供切槽调用 + 可选 `OnChanged` 事件。

## 音量链路

- Master → `IAudioModule.Volume`（AudioListener.volume）。
- Music/Sound → `IAudioModule.MusicVolume/SoundVolume`，走 `AudioMixer.mixer` 的暴露参数 `MusicVolume`/`SoundVolume`（内部 `Log10(v)*20` dB 换算；参数已验证暴露，0 时钳到 0.0001 即 -80dB 近似静音）。
- 旧框架层 PlayerPrefs（`Setting.MusicVolume`/`Setting.SoundVolume`）在首次读取时一次性导入；`ProcedureLaunch.InitSoundSettings()` 仍读旧键作热更加载前兜底，最终值由热更侧 `VolumeSetting.ApplyAll()` 覆盖。

## 启动应用时机

`GameApp.EntranceAsync` 在 `LocalizationSystem` 初始化后调用 `VolumeSetting.ApplyAll()` + `QualitySetting.Apply()`（SaveSystem 首次访问懒加载，此时已可用）。无存档时：音量默认 1，画质默认引擎当前档位。

## 依赖关系

- 依赖：`SaveSystem`（持久化）、TEngine `IAudioModule`（音量应用）、`QualitySettings`（画质）
- 被依赖：InputSystem、CursorManager、CrosshairUpdater

---

> 状态：✅ 音量/画质/操作/准星均已实现并持久化。详细进度见 [progress.md](./progress.md)。
