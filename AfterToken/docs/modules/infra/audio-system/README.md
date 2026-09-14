# 音频系统

## 职责

统一管理背景音乐（BGM）、音效（SFX）、语音（Voice）与四路音量控制。业务代码只跟 `AudioSystem` 门面打交道，底层走 TEngine `GameModule.Audio`。

## 核心文件

| 类/文件 | 说明 |
|---|---|
| `System/AudioSystem.cs` | 音频门面（persistent 单例，`GameApp.EntranceAsync` 里 `EnsureCreated()`） |
| `System/IVoiceProvider.cs` | AI TTS 预留接口：按 speakerId+text 异步返回 `AudioClip`，注册到 `AudioSystem.VoiceProvider` |
| `AI/Voice/TtsConfig.cs` | 独立 TTS 配置、角色状态 profile 与 API Key 加密持久化 |
| `AI/Voice/OpenAiTtsVoiceProvider.cs` | OpenAI Speech API 兼容实现：请求 WAV、内存/磁盘缓存、断联降级 |
| `AI/Voice/WavAudioClipDecoder.cs` | WAV → Unity `AudioClip` 运行时解码 |
| `Config/AudioConfigMgr.cs` | TbAudio 包装：按名字 / 按场景键取条目 |
| `Configs/GameConfig/Datas/audio.xlsx` | 音频表（id/name/audioType/volume/loop/spatial/minDist/maxDist/sceneKey） |
| `Assets/AssetRaw/Audios/{BGM,SFX,Voice}/` | 音频资源；BGM 原型由 `Tools/AudioGen/gen_placeholder_audio.py` 生成，保持同名即可替换正式作曲版本 |

## 已实现能力

- **场景 BGM**：表驱动（`sceneKey`：MainMenu/Simulation/Battle/Combat），三个 Procedure 进场景后调 `PlaySceneBgm`；双 Music agent 交叉淡入淡出（1s）。
- **BGM 风格**：废土科幻与基地经营的混合基调；主菜单使用冷色 pad 与稀疏闪烁音，基地使用温暖琶音，战斗安全态使用低频机械脉冲，交战态使用更紧的工业鼓组。所有 BGM 均为无缝循环原型。
- **战斗态**：订阅 `IEnemyEvent` 聚合处于追击/攻击的敌人数；>0 进战斗 → 切 `bgm_combat` 并把 BGM duck 到 0.5 突出战场音效；归零后缓冲 3s 退出战斗、恢复场景 BGM。脱战计时走缩放时间，暂停时不推进。
- **SFX**：`Play2D`（玩家近场）/ `Play3D`（世界坐标 + spatialBlend=1 + 表配 minDist/maxDist 距离衰减）/ `PlayUI`。已接线：开火/换弹（weapon.xlsx 的 fireSound/reloadSound）、爆炸、拾取、敌人追击脚步（`EnemyChaseState` 0.45s 间隔）、UI 点击（主菜单按钮、设置页签）。
- **Voice**：Player/Npc 两个子通道独立音量；`PlayDialogueVoice` 播放预生成资源；AI 队友全部台词从 `CompanionBrain.EmitSay` 同步进入字幕与 TTS。云端失败时回退 `voice_blip`，晚到的旧请求不会覆盖新台词。
- **TTS 缓存**：缓存键包含端点、模型、voice、instructions、speed、speakerId 和文本；内存缓存用于本局复用，WAV 磁盘缓存位于 `Application.persistentDataPath/VoiceCache/`。调整任一声音参数会自然生成新缓存键。
- **音量链路**：BGM/音效/玩家语音/NPC 语音四路，`VolumeSetting` 持久化到存档，SettingsUI 音频面板四条滑条。

## 使用要点

- 新场景加 BGM：audio.xlsx 加一行（填 sceneKey）+ 对应 Procedure 调 `PlaySceneBgm("键名")`。
- 音频地址 = YooAsset 文件名寻址，`AssetRaw/Audios` 下勿重名。
- 启用动态语音：复制项目根 `tts_config.example.json` 为 `UserSettings/tts_config.json` 并填入 TTS API Key。Unity 编辑器始终优先读该 gitignored 文件，方便反复调音；运行时同时迁移加密副本到 `persistentDataPath/tts_config.json`，正式包只读后者。基础声线改 `voice`，总体语气改 `instructions`，语速改 `speed`，平静/战斗/受伤差异改 `profiles`。
- 固定剧情语音仍推荐提前生成 WAV 并填写 `dialoguenode.voiceClip`；AI 临时台词走云端 TTS + 本地缓存。
- 若使用 OpenAI TTS，对外发行前必须在设置页或首次启用语音时明确告知玩家该声音由 AI 生成、并非真人录音。
- Luban 注意：CSV 注释单元格不能含半角逗号；列名避开 `voice`/`voiceId`（触发 meta 误报），用 `voiceClip`；加列需同步 `__beans__.xlsx` 字段，否则静默忽略。
