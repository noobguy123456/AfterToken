# AI 队友系统 进度

## 已完成

- [x] M1 队友本体（2026-09-07）：`CompanionEntity`（胶囊 + 武器盒占位、血条、IDamageable）+ 六态 FSM + 优先级仲裁驱动器 + `CompanionPathMover` 共用寻路
- [x] M1 护主交战：威胁集合（订阅 `IEnemyEvent` Chase/Attack）→ Engage 真实开火（步枪 1003，复用弹道链路），击杀闭环实测
- [x] M1 双场景：战斗/经营均生成队友；G 键跟随开关（Follow ↔ Hold）双场景实测
- [x] M2 标点系统：中键标点三类分类、15s 过期、世界标记 Quad 脉冲；Move/Loot 标点队友响应实测（到位→消费标点→驻守）；小地图队友绿点 + 标点菱形
- [x] M2 字幕 UI：CompanionSubtitleUI prefab 化，4s 自动隐藏实测（战斗 + 经营场景均验证）
- [x] 实测修 bug：Engage 到位阈值误传射程（原地停死）→ 0.8m 逼近阈值；贴脸 SphereCast 失效 → 1.5m 最小交战距离边退边打；字幕不隐藏 → prefab 根缺 Canvas 组件
- [x] `BallisticSystem` 按 isPlayerFire 区分：枪口修正/枪口火焰/曳光抑制只对玩家开火生效

- [x] M3 配表（2026-09-08）：`companion.xlsx`（人设卡+数值+LLM 调参 16 字段）+ `companionbark.xlsx`（18 条本地台词，权重+冷却）+ 19 个 `companion.*` 词条（en+zh_cn）+ ConfigSystem 白名单
- [x] M3 LLM 链路：`LlmConfig`（UserSettings/llm_config.json，gitignore 保护，根目录 example 模板）+ `LlmClient`（OpenAI 兼容 POST chat/completions，response_format=json_object，失败结构化返回）+ `PromptBuilder`（人设+契约 system / 快照+防复读 user）+ `CompanionBrain`（LinkState 三态、闲聊 25~40s、意图白名单+TTL、5s 请求间隔+单局 60 次限额、离线 30s 探测）
- [x] M3 接线：`CompanionSystem.Say` 改走 Brain.SayLocal（TbCompanionBark）；队友数值（HP/移速/武器/跟随距离/交战距离/撤退阈值）全部 TbCompanion 配置化
- [x] M3 离线实测：未配置→Offline；错 endpoint→Degraded→3 连败→Offline 播 link_lost；闲聊离线走本地表；契约解析单测（合法/垃圾/缺 say）；Console 0 业务 error
- [x] M3 收尾（2026-09-08）：temperature 0.8→0.4（LlmConfig 可调，人格稳定优先）+ system prompt 加 8 条 few-shot 示例（快照格式↔契约回答一一对应，固化 Rook 语气）
- [x] M4 加密存储：`SecretStore`（AES-256-CBC + PBKDF2(deviceUniqueIdentifier+应用盐+productName)，单套代码跨 Windows/Android，弃 DPAPI）；`LlmConfig` 密文/明文双模读取 + 旧明文自动迁移 + `Save()`（加密不可用时拒绝明文落盘）；魔数前缀区分密文
- [x] M4 设置面板 AI 页签（代码侧）：endpoint/apiKey(密码框)/model 输入 + 保存（apiKey 留空=不改动）+ 链路状态显示 + LLM 操控开关；保存后 `CompanionBrain.ReloadConfig()` 热生效；16 个 `ui.settings.llm.*` 词条
- [x] M5a 技能表接线：Context 加 LLM 指令槽（动作/目标/TTL 过期）；驱动器仲裁插入 LLM 层（死亡/低血撤退/玩家标点永远压过它）；PingMove 复用为 move_to/loot/extract 执行体；Engage 目标链 标点→LLM→最近威胁；`PickupEntity` 静态注册表
- [x] M5b 决策节拍器：`DecisionDriver`（安全 8s/战斗 3s/超时 3s/限额 120，全部 TbCompanion 可配）；战场快报 prompt（敌人/掉落物/撤离点/当前动作，不变区域格式）+ 动作契约 + 3 条 few-shot；校验（白名单/目标存活/超时作废）；断联自动回退 FSM
- [x] M5c 遥测：每次决策追加 `Logs/companion_control.csv`（时间/战况/延迟/合法性/动作/token）；GM `companion stats` 现场汇总；`LlmResult` 带 token 用量
- [x] M4/M5 在线实测（2026-09-08，DeepSeek deepseek-chat）：明文 key 加载自动迁移 `ATENC1:` 密文；设置面板 AI 页签 prefab 建成（5 页签均分，输入框锚点修正），UI 保存→加密落盘→局内热生效；链路 Online；闲聊通道出 LLM 台词；操控通道一局 47 决策 100% 合法、平均延迟 ~855ms；combat→engage（有效目标）→击杀→safe→loot→到位驻守全链路实测；玩家死亡 timeScale=0 决策正确停摆
- [x] 主动开火（2026-09-08）：Context 加 `NearestVisibleEnemy`（警戒半径 `proactiveEngageDist`=12m + Obstacle 视线，敌人 Idle/巡逻也算，先发制人）；`WantsEngage` 纳入它；仲裁顺序改为 死亡>低血撤退>标点>**交战>LLM 指令**>姿态（修掉 M5 回归：LLM 说 follow 时放弃护主）；实测：传送队友到敌人 6m 处，threats=0 全程下清光 12m 内 4 个敌人
- [x] 人设强化（2026-09-08）：`companion.xlsx` personaPrompt 重写（退役军用安防 AI、叫玩家 "boss"、冷幽默、11 个 sector 背景、说话规则+示例）；闲聊/操控 few-shot 全换新语气；实测台词 "Loot spotted. Securing it, boss."
- [x] M6 自由对话（2026-09-08）：T 键（可改绑 `KeyBindAction.CompanionChat`）开 `CompanionChatUI`（prefab：底部输入条在字幕上方，Enter 发送/ESC 关闭/不暂停）；`CompanionBrain.RequestChatReply` 独立通道（多轮记忆 4 轮、独立计数与 1s 防抖、断联/战斗中播 chat_busy bark 拒聊）；回复 intent 仍走白名单裁决；**意图应用收窄：只有玩家直接对话（player_chat）可带 follow/hold，安全闲聊不再应用意图**（修掉 LLM 闲聊随口 hold 把队友钉原地）；输入抑制：聊天打开时 InputSystem/SimulationInputSystem/SimulationPlayerController/BuildingPlacementSystem 全部让位；实测：安全开聊/战斗拒聊/回复上字幕/"stay here"→hold→"follow me"→follow 全通；修经营场景 PlayerSystem 缺失导致 player_hp 0/0 被 LLM 误读"boss 已倒下"（血量未知时不进 prompt）

## 进行中

- [ ] M2 收尾：Attack 标点集火未单独实测（与 Loot 共用分类代码 + Engage 目标覆盖，风险低）

## 待办

- [ ] Degraded 态字幕干扰样式（M4 尾巴）
- [ ] kill 触发台词未接线（当前无法识别队友击杀归属，需弹道链路带 killer 信息）
- [ ] 队友占位视觉换正式模型；枪口改模型 Muzzle socket
- [ ] LLM 流式 SSE（MVP 非流式，扩展位已留）
- [ ] M5 已知瑕疵：安全态 LLM 爱刷 loot 指令（逐个拾取有效但会离开玩家去捡）；聊天输入抑制为代码层 review，未经真实键鼠 e2e

## 阻塞

- 无（DeepSeek key 已加密配置，链路 Online 实测通过）
