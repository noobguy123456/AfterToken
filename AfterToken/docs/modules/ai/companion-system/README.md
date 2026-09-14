# AI 队友系统（companion-system）

> 提案：[`docs/Proposal/ai/llm-companion.md`](../../../Proposal/ai/llm-companion.md)、[`docs/Proposal/ai/companion-affinity-memory.md`](../../../Proposal/ai/companion-affinity-memory.md)（好感度与记忆）
> 当前进度：M1-M6 + 好感度/记忆系统（M1 数值存档 / M2 面板赠礼 / M3 人格分档 / M4 记忆召回）全部落地并在线实测（2026-09-14，DeepSeek）。

## 职责

基于 LLM 的 AI 队友（能天使 Exusiai）。核心原则：**LLM 是"嘴和人格"，不是"身体"**——行为控制由本地确定性 FSM 承担（离线可完整玩），LLM 层（M3 起）只负责生成对话文本与白名单内的意图建议。

- 队友实体：生成/销毁、HP/受击/血条、占位视觉（胶囊 + 右侧武器盒）。
- 六态 FSM：Follow / Hold / PingMove / Engage / Retreat / Dead，驱动器按优先级仲裁（死亡 > 撤退 > 标点 > 交战 > 姿态）。
- 护主交战：订阅 `IEnemyEvent` 维护威胁集合（Chase/Attack 状态敌人），真实开火复用武器/弹道链路。
- 标点系统：鼠标中键标点（敌人 > 掉落物/容器 > 地面），世界标记 + 小地图菱形，队友按类型响应。
- 字幕与语音：底部字幕条（CompanionSubtitleUI）；本地 bark 与 LLM 台词统一从 `CompanionBrain.EmitSay` 发字幕并进入 TTS。语音失败只降级为占位音，不影响文字和行为。
- 双场景常驻：战斗（BattleRoot）与经营（SimulationRoot）均挂载，G 键切换跟随。
- 好感度：聊天/撤离/护驾/赠礼四来源（数值全走 `companionaffinity.xlsx`），4 档阈值分档，升降档头顶飘字 + 升档专属 bark。
- 记忆：内容与功能解耦，`companionmemoryrule.xlsx` 规则驱动（类型×档位→容量/写入概率/淘汰策略）；聊天记忆由 LLM 打标（memorable），召回走 LLM 驱动两阶段（recall 请求→注入记忆段二次请求）。
- 人格分档：`companionpersona.xlsx` 按档位叠加关系姿态（基底人设不变）；bark 池按 minTier 过滤，档位越高池子越大。
- 信息面板：CompanionInfoUI（档位/进度/人格描述/记忆列表/赠礼子面板），聊天窗 Info 按钮 + 靠近 3m 按交互键双入口。

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `Entity/Companion/CompanionEntity.cs` | 实体：IDamageable、占位视觉、血条（钉头顶 + billboard）、`GetMuzzleWorldPos()` 枪口挂点 |
| `FSM/Companion/CompanionStateMachineDriver.cs` | 感知聚合 + 期望状态仲裁（静态实例，每帧由 CompanionEntity.Update 驱动） |
| `FSM/Companion/CompanionPathMover.cs` | Follow/PingMove/Engage/Retreat 共用寻路移动（路径刷新/路径点跟随/SkipPassedWaypoints 防回抖/无导航直走） |
| `FSM/Companion/Companion*State.cs` | 六态：Follow（2.2m 停）/ Hold / PingMove / Engage（护主开火）/ Retreat（HP<30% 且有威胁）/ Dead |
| `System/CompanionSystem.cs` | 双场景挂载；生成队友、威胁集合维护、标点转发、G 键跟随开关、`Say()` 本地 bark（同键 4s 冷却） |
| `System/PingSystem.cs` | 仅战斗场景；中键标点分类（敌人 1.0m > 掉落/容器 1.2m > 地面钳制可走点）、15s 过期、世界标记 Quad |
| `IEvent/IPingEvent.cs` | `PingType`（Move/Loot/Attack）+ `OnPingCreated` / `OnPingCleared` |
| `IEvent/ICompanionEvent.cs` | `OnCompanionSay(speaker,text)` / `OnCompanionStateChanged` / `OnCompanionFollowToggled` |
| `UI/CompanionSubtitleUI/` | 底部字幕条（prefab 在 `AssetRaw/UI/CompanionSubtitleUI/`），4s 自动隐藏，新台词顶掉旧的 |
| `AI/Llm/LlmConfig.cs` | LLM 本地配置：`UserSettings/llm_config.json`（gitignore 保护，模板在项目根 `llm_config.example.json`），endpoint/apiKey/model/超时可配 |
| `AI/Llm/LlmClient.cs` | OpenAI 兼容 HTTP 封装（POST /chat/completions，非流式），失败返回 `LlmResult` 不抛异常 |
| `AI/Llm/PromptBuilder.cs` | 人设卡 + 世界观摘要 + 输出契约（system）；上下文快照（user），含最近 3 条台词防复读 |
| `AI/Llm/CompanionBrain.cs` | 队友大脑：LinkState 三态（Online/Degraded/Offline）+ 台词调度 + 意图裁决（白名单+TTL 3s）+ 闲聊定时器（25~40s）+ 离线 30s 探测恢复 |
| `AI/Voice/TtsConfig.cs` | 独立 TTS 配置：API Key 加密、默认声线、平静/战斗/受伤 profile；模板为项目根 `tts_config.example.json` |
| `AI/Voice/OpenAiTtsVoiceProvider.cs` | OpenAI Speech API 兼容 TTS，WAV 响应动态解码，内存 + `persistentDataPath/VoiceCache` 磁盘缓存 |
| `Config/CompanionConfigMgr.cs` | TbCompanion（人设卡+数值+LLM 调参）/ TbCompanionBark（本地台词表，权重+冷却+minTier 档位过滤）包装 |
| `Shared/CompanionAffinitySystem.cs` | 好感度静态系统：exp/4 档 tier/来源冷却与每局限次/赠礼，SaveData.companionAffinity 段持久化 |
| `Shared/CompanionMemorySystem.cs` | 记忆静态系统：规则表执行（容量/概率/淘汰）、礼物机读 token `gift:{itemId}x{count}`、FormatForPrompt/Display |
| `Config/CompanionAffinityConfigMgr.cs` / `CompanionPersonaConfigMgr.cs` / `CompanionMemoryRuleConfigMgr.cs` | 好感阈值与来源 / 档位人格姿态 / 记忆规则 三表包装 |
| `IEvent/ICompanionAffinityEvent.cs` | `OnAffinityChanged` / `OnAffinityTierUp`（CompanionSystem 订阅播飘字与升档 bark） |
| `UI/CompanionInfoUI/` | 信息面板 prefab（`AssetRaw/UI/CompanionInfoUI/`）：档位/进度/人格描述/记忆列表/赠礼 |
| `UI/WorldFloatText.cs` | 世界飘字公共工具（好感 +N ♥、升档、好奇提示共用，legacy Text 中文兼容） |

## 对外接口

- `CompanionSystem.Instance` / `.Companion` / `.Threats` / `.PlayerTransform`
- 玩家侧输入：中键标点（`KeyBindAction.Ping`）、G 键跟随开关（`KeyBindAction.CompanionFollow`，直接 `Input.GetKeyDown` 轮询，经营场景同样可用）
- UI/表现消费 `ICompanionEvent`；好感变化消费 `ICompanionAffinityEvent`；队友行为消费 `IPingEvent` / `IEnemyEvent`
- 信息面板入口：`CompanionChatUI` Info 按钮，或靠近队友 3m 出现提示后按交互键（NPC 提示激活时避让；ESC 链接入双场景输入系统）

## 依赖关系

- 复用：敌人 FSM 模式（FsmState + Context + Driver）、`AStarNavigationSystem`、武器/弹道链路（`WeaponInstance.Fire` + ownerId 区分敌我）、`MinimapUI` 图标投影。
- `BallisticSystem.OnFire` 按 `isPlayerFire`（ownerId == 玩家 OwnerId）区分：枪口修正/枪口火焰/开镜曳光抑制只对玩家开火生效，队友开火不污染玩家表现。

## 设计要点

- **FSM 不知道 LLM 存在**：LLM 意图经白名单校验 + TTL（TbCompanion.intentTtl）后以 `Context.PendingRequest` 形式进入，可被死亡/撤退/标点任意本地来源顶掉。
- **战斗台词不走 LLM**（硬决策）：交战/标点/跟随切换/死亡等即时反馈全部走 TbCompanionBark 本地表秒回；LLM 只负责安全期闲聊（safe_idle，25~40s 随机）。
- **TTS 与人格/行为解耦**：TTS 只朗读 `EmitSay` 已确认的文字，不参与意图裁决。`speakerId` 按玩法状态映射为 `companion_{id}_calm/combat/hurt`，语速、voice 与声音指令全部在 `tts_config.json` 调整。
- **语音降级与竞态**：TTS 未配置、网络失败、非 WAV 响应或解码失败时回退 `voice_blip`；新台词会取消旧请求并用版本号拒绝晚到结果，跳过/固定剧情语音也会停止动态语音。
- **断联降级**：LinkState 三态——未配置直接 Offline；已配置未验证先 Degraded；连续失败 ≥3 进 Offline 播 link_lost，后台每 offlineProbeInterval 探测，成功恢复 Online 播 link_recovered。Offline 时闲聊继续走本地表，行为零损失。
- **费用/频率控制**：全局最小请求间隔 5s + 单局限额 60 次（TbCompanion.llmRequestInterval / llmBudgetPerRun）。
- **输出契约**：`{"say","intent","mood"}` JSON；say 缺失或解析失败整包丢弃转本地兜底；say 超 80 字截断；请求体带 `response_format=json_object` + system prompt 契约双保险。
- 经营场景无导航网格：`CompanionPathMover` 在 `nav == null` 时直走；玩家定位走 `SimulationPlayerController` 组件查找（玩家实例名是 `Player(Clone)`，按名字 `GameObject.Find("Player")` 找不到）。
- 贴脸交战保护：`TbCompanion.engageMinDist`（默认 1.5m），贴脸边退边打（枪口 SphereCast 在敌人碰撞体内会丢命中）。
- UI prefab 根节点**必须带 Canvas 组件**（UIWindow.Handle_Completed 强校验，缺失则 IsPrepare=false，窗口 OnUpdate 永不驱动且场景初始化报错）。
- **人格分档不割裂**：PromptBuilder 三个 Build 入口在基底人设后追加 `companionpersona` 档位姿态段（AppendTierPersona，逐档向下回退）；档位只改变关系姿态，不改人设基底。
- **记忆解耦**：记什么（类型）与怎么记（容量/概率/淘汰）全在 `companionmemoryrule.xlsx`，代码不硬编码档位语义；聊天记忆由 LLM 打标 memorable，写入提示统一含蓄化（"对你的话/礼物感到好奇"，不暴露随机判定）。
- **记忆召回两阶段**：聊天契约 `recall` 字段非空且本会话未用过 → `BuildChatSystemWithRecall` 注入 "You recall:" 记忆段二次请求并禁再次召回；详细召回规则待用户方案（提案 §10 挂起）。
- **赠礼**：`item.xlsx` 的 `affinityValue`/`companionAccept` 两列驱动，accept=0 拒收不扣物品；礼物记忆存机读 token（`gift:{itemId}x{count}`），显示与 prompt 注入分别走 FormatForDisplay/FormatForPrompt。
