# AI 队友系统（companion-system）

> 提案：[`docs/Proposal/ai/llm-companion.md`](../../../Proposal/ai/llm-companion.md)
> 当前进度：M1（队友本体）/ M2（标点系统）/ M3（LLM 链路，离线链路已实测）已落地；M4（断联降级补全 + 设置面板 API 配置）未启动。

## 职责

基于 LLM 的 AI 队友（呼号 Rook）。核心原则：**LLM 是"嘴和人格"，不是"身体"**——行为控制由本地确定性 FSM 承担（离线可完整玩），LLM 层（M3 起）只负责生成对话文本与白名单内的意图建议。

- 队友实体：生成/销毁、HP/受击/血条、占位视觉（胶囊 + 右侧武器盒）。
- 六态 FSM：Follow / Hold / PingMove / Engage / Retreat / Dead，驱动器按优先级仲裁（死亡 > 撤退 > 标点 > 交战 > 姿态）。
- 护主交战：订阅 `IEnemyEvent` 维护威胁集合（Chase/Attack 状态敌人），真实开火复用武器/弹道链路。
- 标点系统：鼠标中键标点（敌人 > 掉落物/容器 > 地面），世界标记 + 小地图菱形，队友按类型响应。
- 字幕呈现：底部字幕条（CompanionSubtitleUI），本地 bark 与未来的 LLM 台词统一走 `ICompanionEvent.OnCompanionSay`。
- 双场景常驻：战斗（BattleRoot）与经营（SimulationRoot）均挂载，G 键切换跟随。

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
| `Config/CompanionConfigMgr.cs` | TbCompanion（人设卡+数值+LLM 调参）/ TbCompanionBark（本地台词表，权重+冷却）包装 |

## 对外接口

- `CompanionSystem.Instance` / `.Companion` / `.Threats` / `.PlayerTransform`
- 玩家侧输入：中键标点（`KeyBindAction.Ping`）、G 键跟随开关（`KeyBindAction.CompanionFollow`，直接 `Input.GetKeyDown` 轮询，经营场景同样可用）
- UI/表现消费 `ICompanionEvent`；队友行为消费 `IPingEvent` / `IEnemyEvent`

## 依赖关系

- 复用：敌人 FSM 模式（FsmState + Context + Driver）、`AStarNavigationSystem`、武器/弹道链路（`WeaponInstance.Fire` + ownerId 区分敌我）、`MinimapUI` 图标投影。
- `BallisticSystem.OnFire` 按 `isPlayerFire`（ownerId == 玩家 OwnerId）区分：枪口修正/枪口火焰/开镜曳光抑制只对玩家开火生效，队友开火不污染玩家表现。

## 设计要点

- **FSM 不知道 LLM 存在**：LLM 意图经白名单校验 + TTL（TbCompanion.intentTtl）后以 `Context.PendingRequest` 形式进入，可被死亡/撤退/标点任意本地来源顶掉。
- **战斗台词不走 LLM**（硬决策）：交战/标点/跟随切换/死亡等即时反馈全部走 TbCompanionBark 本地表秒回；LLM 只负责安全期闲聊（safe_idle，25~40s 随机）。
- **断联降级**：LinkState 三态——未配置直接 Offline；已配置未验证先 Degraded；连续失败 ≥3 进 Offline 播 link_lost，后台每 offlineProbeInterval 探测，成功恢复 Online 播 link_recovered。Offline 时闲聊继续走本地表，行为零损失。
- **费用/频率控制**：全局最小请求间隔 5s + 单局限额 60 次（TbCompanion.llmRequestInterval / llmBudgetPerRun）。
- **输出契约**：`{"say","intent","mood"}` JSON；say 缺失或解析失败整包丢弃转本地兜底；say 超 80 字截断；请求体带 `response_format=json_object` + system prompt 契约双保险。
- 经营场景无导航网格：`CompanionPathMover` 在 `nav == null` 时直走；玩家定位走 `SimulationPlayerController` 组件查找（玩家实例名是 `Player(Clone)`，按名字 `GameObject.Find("Player")` 找不到）。
- 贴脸交战保护：`TbCompanion.engageMinDist`（默认 1.5m），贴脸边退边打（枪口 SphereCast 在敌人碰撞体内会丢命中）。
- UI prefab 根节点**必须带 Canvas 组件**（UIWindow.Handle_Completed 强校验，缺失则 IsPrepare=false，窗口 OnUpdate 永不驱动且场景初始化报错）。
