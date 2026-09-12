# Audio System 进度

## 已完成（2026-09-12）
- [x] `AudioSystem` 门面（persistent）：BGM / SFX / Voice 统一入口
- [x] 场景 BGM 表驱动切换 + 双 agent 交叉淡入淡出（主菜单/经营/战斗三场景实测通过）
- [x] 战斗态：切战斗 BGM + duck 0.5，脱战缓冲 3s 恢复（刷怪→追击→killall→恢复 全链路实测通过）
- [x] SFX 事件绑定：开火/换弹/爆炸/拾取/敌人脚步/UI 点击；3D 空间音效（spatialBlend=1 + 表配衰减距离，实测 fire spatial=1.0）
- [x] 四路音量（BGM/SFX/玩家语音/NPC 语音）持久化 + SettingsUI 四条滑条（实测存在、默认 0.8）
- [x] 对话语音：`voiceClip` 列 + 跳过/推进/结束同步停语音
- [x] `IVoiceProvider` TTS 预留接口
- [x] 占位 WAV ×14（程序生成，待正式资源替换）

## 待办
- [ ] 正式音频资源替换占位 WAV
- [ ] AI 队友 TTS 实现 `IVoiceProvider`（接 LLM 厂商语音接口）
- [ ] 更多 UI/交互音效铺量（按钮 hover、背包操作等）

---

> 状态说明：
> - 当前总状态：✅（占位资源阶段，听感待用户验收）
> - 每次更新后同步 `docs/TODO.md`
