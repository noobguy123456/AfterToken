# Settings UI

## 职责

负责游戏内设置面板：
- 准星灵敏度（已实现）
- 音量（主音量、BGM、SFX）
- 画质（分辨率、帧率、特效质量）
- 操作（按键映射、辅助瞄准开关）
- 关闭按钮与暂停逻辑

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `SettingsUI` | 设置面板 UIWindow |
| `SensitivitySetting` | 准星灵敏度读写（当前使用 PlayerPrefs） |
| `SettingsSystem` | 设置持久化总入口（待实现） |

## 依赖关系

- 依赖：`SettingsSystem`（持久化）、`AudioSystem`（音量应用）、`CursorManager`（光标显示）

---

> 状态：部分实现。详细进度见 [progress.md](./progress.md)。
