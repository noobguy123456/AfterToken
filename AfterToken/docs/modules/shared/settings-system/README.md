# Settings System

## 职责

负责玩家设置的持久化与运行时读取：
- 音量（主音量、BGM、SFX）
- 画质（分辨率、帧率、特效质量）
- 操作（灵敏度、按键映射、辅助瞄准开关）
- 其他（光标样式、语言等）

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `SettingsSystem` | 设置总入口，提供 Get/Set/Apply 接口 |
| `SensitivitySetting` | 准星灵敏度（已实现，但未持久化） |
| `SettingsUI` | 设置面板 UI（已实现灵敏度页签） |
| `AudioSystem` | 音量设置的实际应用（待实现） |

## 对外接口

```csharp
public static class SettingsSystem
{
    public static float GetFloat(string key, float defaultValue = 0);
    public static void SetFloat(string key, float value);
    public static int GetInt(string key, int defaultValue = 0);
    public static void SetInt(string key, int value);
    public static void Save();
}
```

## 依赖关系

- 依赖：`SaveSystem`（持久化）
- 被依赖：AudioSystem、CameraSystem、InputSystem、CursorManager

## 设计要点

- `SettingsUI` 当前已实现准星灵敏度，后续补充音量/画质/操作页签。
- 设置变更时派发事件，由对应系统监听并应用。

---

> 状态：部分实现（灵敏度可用），待持久化。详细进度见 [progress.md](./progress.md)。
