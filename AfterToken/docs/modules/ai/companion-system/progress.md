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

## 进行中

- [ ] M2 收尾：Attack 标点集火未单独实测（与 Loot 共用分类代码 + Engage 目标覆盖，风险低）
- [ ] 在线实测：等用户提供真实 LLM API key（链路代码已就绪，未验证真实端点往返）

## 待办

- [ ] M4 断联降级补全 + 设置：设置面板 API 配置项（endpoint/apiKey/model 输入 + LlmConfig.Reload 热生效）；Degraded 态字幕干扰样式
- [ ] kill 触发台词未接线（当前无法识别队友击杀归属，需弹道链路带 killer 信息）
- [ ] 队友占位视觉换正式模型；枪口改模型 Muzzle socket
- [ ] LLM 流式 SSE（MVP 非流式，扩展位已留）

## 阻塞

- 在线实测与调优需要真实 LLM API key（复制 `llm_config.example.json` → `UserSettings/llm_config.json` 填 key）
