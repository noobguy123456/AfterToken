# Audio System 进度

## 已完成（更新至 2026-09-15）
- [x] `AudioSystem` 门面（persistent）：BGM / SFX / Voice 统一入口
- [x] 场景 BGM 表驱动切换 + 双 agent 交叉淡入淡出（主菜单/经营/战斗三场景实测通过）
- [x] 战斗态：切战斗 BGM + duck 0.5，脱战缓冲 3s 恢复（刷怪→追击→killall→恢复 全链路实测通过）
- [x] SFX 事件绑定：开火/换弹/爆炸/拾取/敌人脚步/UI 点击；3D 空间音效（spatialBlend=1 + 表配衰减距离，实测 fire spatial=1.0）
- [x] 四路音量（BGM/SFX/玩家语音/NPC 语音）持久化 + SettingsUI 四条滑条（实测存在、默认 0.8）
- [x] 对话语音：`voiceClip` 列 + 跳过/推进/结束同步停语音
- [x] `IVoiceProvider` + OpenAI Speech API 兼容 TTS：WAV 动态解码、状态声线、双层缓存、失败回退、晚到请求抑制
- [x] 占位 WAV ×14（程序生成，待正式资源替换）
- [x] 音频稳定性加固：换场景清空战斗威胁/duck；快速三连切 BGM 不再卡 pending load；淡出改为固定起点线性插值
- [x] 战斗 SFX 扩为 24 agent；非 BGM 短音频启动预加载并复用句柄；修复 Loading agent 被重复分配和预加载句柄竞态
- [x] 音量链路修正：UI 音效跟随 Sound、播放中语音实时调音；滑条写盘采用 300ms 防抖并在设置页关闭/应用退出时提交
- [x] 四条 BGM 资源改为 Streaming + Load In Background

## 待办
- [ ] 正式音频资源替换占位 WAV
- [ ] `audio.xlsx` 增加 preload/priority/maxInstances 字段，正式音效扩量后按类别做并发优先级与同类限流
- [ ] 增加音频 EditMode 配置校验与 PlayMode 快速切歌/并发回归测试
- [ ] 设置页增加 TTS endpoint/key/model/voice/speed 可视化编辑（当前通过 `tts_config.json` 调整）
- [ ] 设置页/首次启用流程增加“AI 生成语音、非真人录音”明确披露
- [ ] 更多 UI/交互音效铺量（按钮 hover、背包操作等）

---

> 状态说明：
> - 当前总状态：核心链路 ✅；正式资源、混音与听感验收 🟡
> - 每次更新后同步 `docs/TODO.md`
