# 音频系统

## 职责

统一管理背景音乐（BGM）、音效（SFX）、语音（Voice）与四路音量控制。业务代码只跟 `AudioSystem` 门面打交道，底层走 TEngine `GameModule.Audio`。

## 核心文件

| 类/文件 | 说明 |
|---|---|
| `System/AudioSystem.cs` | 音频门面（persistent 单例，`GameApp.EntranceAsync` 里 `EnsureCreated()`） |
| `System/IVoiceProvider.cs` | AI TTS 预留接口：按 speakerId+text 异步返回 `AudioClip`，注册到 `AudioSystem.VoiceProvider` |
| `Config/AudioConfigMgr.cs` | TbAudio 包装：按名字 / 按场景键取条目 |
| `Configs/GameConfig/Datas/audio.xlsx` | 音频表（id/name/audioType/volume/loop/spatial/minDist/maxDist/sceneKey） |
| `Assets/AssetRaw/Audios/{BGM,SFX,Voice}/` | 音频资源（当前为 `Tools/AudioGen/gen_placeholder_audio.py` 生成的占位 WAV） |

## 已实现能力

- **场景 BGM**：表驱动（`sceneKey`：MainMenu/Simulation/Battle/Combat），三个 Procedure 进场景后调 `PlaySceneBgm`；双 Music agent 交叉淡入淡出（1s）。
- **战斗态**：订阅 `IEnemyEvent` 聚合处于追击/攻击的敌人数；>0 进战斗 → 切 `bgm_combat` 并把 BGM duck 到 0.5 突出战场音效；归零后缓冲 3s 退出战斗、恢复场景 BGM。脱战计时走缩放时间，暂停时不推进。
- **SFX**：`Play2D`（玩家近场）/ `Play3D`（世界坐标 + spatialBlend=1 + 表配 minDist/maxDist 距离衰减）/ `PlayUI`。已接线：开火/换弹（weapon.xlsx 的 fireSound/reloadSound）、爆炸、拾取、敌人追击脚步（`EnemyChaseState` 0.45s 间隔）、UI 点击（主菜单按钮、设置页签）。
- **Voice**：Player/Npc 两个子通道独立音量（共用 Voice mixer 组，组音量固定 1，用户音量逐 agent 应用）；`PlayDialogueVoice` 读 dialoguenode 的 `voiceClip` 列；对话推进/选项/结束时 `StopVoice` 同步停语音；`PlayCompanionVoice` 走 `VoiceProvider`（TTS 预留，未实现时回退占位音）。
- **音量链路**：BGM/音效/玩家语音/NPC 语音四路，`VolumeSetting` 持久化到存档，SettingsUI 音频面板四条滑条。

## 使用要点

- 新场景加 BGM：audio.xlsx 加一行（填 sceneKey）+ 对应 Procedure 调 `PlaySceneBgm("键名")`。
- 音频地址 = YooAsset 文件名寻址，`AssetRaw/Audios` 下勿重名。
- Luban 注意：CSV 注释单元格不能含半角逗号；列名避开 `voice`/`voiceId`（触发 meta 误报），用 `voiceClip`；加列需同步 `__beans__.xlsx` 字段，否则静默忽略。
