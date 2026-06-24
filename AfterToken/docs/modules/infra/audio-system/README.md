# 音频系统

## 职责

统一管理背景音乐（BGM）、音效（SFX）和音量控制。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `AudioSystem` | 音频管理入口 |

## 设计要点

- 通过事件（如 `IWeaponEvent.OnFire`）触发 SFX。
- 区分 BGM 与 SFX 音量通道。
