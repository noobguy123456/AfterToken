# 提案：基于 LLM 的 AI 队友系统

> 提案状态：M1/M2/M3 已落地（2026-09-07/08 Play 实测通过，见 `docs/modules/ai/companion-system/progress.md`）；M4 待启动；在线实测待真实 API key
> 提出时间：2026-09-07
> 提案路径：`docs/Proposal/ai/llm-companion.md`
> 关联模块：
> - `docs/modules/combat/enemy-system/`（FSM 模板参考）
> - `docs/modules/narrative/dialogue-system/`（对话呈现参考）
> 关联文档：
> - `docs/Proposal/narrative/dialogue-system.md`
> - `docs/Proposal/combat/pure-csharp-data-oriented-roadmap.md`

---

## 1. 背景

搜打撤玩法目前是纯单人体验。用户希望加入一名由 LLM 驱动的 AI 队友，具备四项能力：

1. **实时文本对话**：基于队友设定库（人设卡），在安全状态（未被攻击/追踪）时输出个性语言或世界观对话；队友自身有状态机与行为偏好。
2. **标点识别**：识别玩家的标点（APEX 式标记系统），执行相关操作或改变自身状态机。
3. **敌我识别**：进入战斗状态时自动保护玩家，以玩家为最优先。
4. **断联降级**：LLM 断联时在队友自身文本中体现（如"信号中断"式的世界观化表达），但保留完整的本地状态机行为能力。

### 1.1 核心设计原则（业界共识）

**LLM 是"嘴和人格"，永远不是"身体"。** 参考业界 AI 队友实践（《Biomutant》式行为树 + 近年 LLM NPC 如 Inworld/conv.ai 方案的共同教训）：

- LLM 延迟 1~5 秒、可能超时、可能输出非法内容——**绝不能**让它直接控制移动/开火/寻路。
- 行为控制由**本地确定性 FSM** 承担（复用敌人 FSM 的成熟模式），离线也能完整玩。
- LLM 层只做两件事：**生成对话文本**、在**白名单内**提交"意图建议"（如"建议驻守"），由本地 FSM 裁决是否执行。
- 断联时对话降级为本地配置的自由台词（bark 表），玩法行为零损失。

### 1.2 现状盘点

| 已有设施 | 复用点 |
|---|---|
| 敌人 FSM（`FSM/Enemy/`：Idle/Wander/Chase/Attack/Dead + 中断仲裁 Driver） | 队友 FSM 直接套用同一模式（FsmState + Context + 优先级请求） |
| `AStarNavigationSystem.FindPath(Vector2, Vector2)` | 队友跟随/移动寻路 |
| `IEnemyEvent.OnEnemyStateChanged` | 感知层判断"是否被追踪/攻击"，即安全状态判定 |
| 武器/弹道链路（`WeaponInstance.Fire` + ownerId 机制） | 队友开火复用，ownerId 区分敌我 |
| `IDamageable` + 血条 Billboard | 队友受击与血条 |
| Luban 配表 + 本地化 | 人设卡、台词表配置 |
| 事件总线（19 个 IEvent 接口） | 新增 `IPingEvent` / `ICompanionEvent` |
| `DialogueUI` / `InteractionPromptUI` | 对话呈现参考（队友用轻量气泡，不走全屏对话窗） |

| 缺口 | 说明 |
|---|---|
| 无 HTTP 网络设施 | 全项目零 UnityWebRequest/HttpClient 使用，需新建 LLM 客户端层（见 §6 风险） |
| 无标点系统 | APEX 式 ping 本身是新的玩家向系统，需从零做 |
| 无队友实体 | 新建 CompanionEntity + FSM |

---

## 2. 目标

### 2.1 本期目标

1. 队友实体常驻战斗关卡，默认**跟随**玩家（寻路、保持距离）。
2. 本地 FSM 全量行为能力：跟随 / 驻守 / 前往标点 / 交战（护主优先）/ 撤退回避。
3. 玩家标点系统：上下文敏感的单击标点（敌人→集火标记、地面→移动标记、掉落物→拾取标记），队友按标记行动。
4. LLM 在线时：安全状态个性闲聊、事件触发台词（开战/击杀/标点响应/低血量）、对白名单指令的意图裁决。
5. LLM 断联时：UI 与台词体现"通讯中断"的世界观化表达，行为全部由本地 FSM + 本地台词表兜底。

### 2.2 本期不做

- 语音合成（TTS）、口型。
- 多队友（架构按单队友做，集合语义预留）。
- 队友背包/装备养成、队友倒地救援交互。
- 标点半径轮盘（APEX 长按轮盘），只做单击上下文标点。
- 本地小模型部署方案（只走云端 HTTP API）。

---

## 3. 总体架构

```
                    ┌──────────────── LLM 链路（可断） ────────────────┐
                    │                                                  │
  TbCompanion 人设卡 ─► PromptBuilder ─► LlmClient(HTTP) ─► 云端 LLM API │
  TbCompanionBark 台词表        ▲              │                       │
                    │         上下文            │ ① 台词文本            │
                    │         快照             │ ② 结构化意图(JSON白名单)│
                    ▼                          ▼                       │
┌─────────────────────────────────────────────────────────────────┐
│ CompanionBrain（队友大脑，热更内）                                  │
│  - LinkState（Online/Degraded/Offline）+ 心跳超时判定               │
│  - 台词调度：触发事件 → 在线问 LLM / 离线查 Bark 表 → 气泡+字幕      │
│  - 意图裁决：LLM 建议 → 白名单校验 + TTL 过期 → 转 FSM 状态请求      │
└──────────────┬───────────────────────────────────┬───────────────┘
               │ 状态请求（IntentRequest）            │ 台词事件
               ▼                                     ▼
┌──────────────────────────┐            CompanionSubtitleUI（底部字幕条）
│ CompanionEntity + FSM     │
│ Follow/Hold/PingMove/     │
│ Engage/Retreat/Dead       │
└──────┬───────────┬───────┘
       │ 寻路        │ 开火
       ▼           ▼
AStarNavigationSystem   WeaponInstance（ownerId=companion）

玩家输入 ─► PingSystem（上下文射线判定）─► IPingEvent ─► CompanionBrain/FSM
                                                      └► PingMarkerView（世界标记+小地图图标）
```

分层原则（与对话系统一致）：

- **FSM 不知道 LLM 存在**：FSM 只消费"状态请求"，无论来源是标点、感知规则还是 LLM 意图裁决。
- **LLM 客户端不知道玩法存在**：`LlmClient` 是纯 HTTP 封装（请求/超时/重试/取消），可被未来任何系统复用。
- **呈现不知道内容来源**：气泡 UI 只消费"说了一句话"事件，不区分 LLM 生成还是本地兜底。

---

## 4. 队友本地 FSM（离线兜底核心）

### 4.1 状态定义

| 状态 | 进入条件 | 行为 | 退出 |
|---|---|---|---|
| `Follow` | 默认 | 跟随玩家，保持 2~4m 距离，超出 5m 寻路追赶 | 收到标点/交战/驻守指令 |
| `Hold` | 驻守标点或 LLM/玩家指令 | 原地警戒，小范围索敌 | 新指令 |
| `PingMove` | 地面移动标点 | 寻路到标点，到位后转 Hold | 交战打断 |
| `Engage` | 感知层判定战斗状态（玩家被追踪/攻击，或队友被攻击） | **护主优先**：优先攻击"正在追击玩家的敌人"，其次攻击自身威胁；边打边保持与玩家距离 ≤8m | 威胁清空后回 Follow |
| `Retreat` | 自身 HP < 30%（可配） | 向玩家方向收缩 + 脱离仇恨范围 | HP 安全/威胁清空 |
| `Dead` | HP ≤ 0 | 倒地，本局不再行动（MVP 不做救援） | 撤离/重开 |

### 4.2 状态仲裁

沿用敌人 FSM 的 `Context.PendingRequest` + 优先级模式。请求来源优先级（高→低）：

1. 生存规则（Retreat/Dead，本地硬性，LLM 不可覆盖）
2. 玩家标点（显式指令，最高意图优先级）
3. 感知规则（交战触发/解除，本地硬性）
4. LLM 意图建议（白名单内、TTL 内才生效，可被以上任意来源顶掉）

### 4.3 战斗参与

- 队友持有简化武器（配置表指定 weaponConfigId），开火复用 `WeaponInstance.Fire(origin, direction, ownerId)`，ownerId 为队友 InstanceID——弹道/伤害链路零改动。
- 目标选择：每 0.2s 感知扫描，威胁列表按"追击玩家 > 攻击队友 > 距离最近"排序。
- 队友伤害走 `IDamageable`，血条复用现有 Billboard 方案（固定朝向版）。

---

## 5. 标点系统（Ping）

APEX 式单击上下文标点，也是玩家向的新输入系统（队友只是其最大消费者）。

### 5.1 交互与判定

- 按键：默认**鼠标中键**（设置界面可改绑，走现有按键绑定链路）。
- 判定：从准星做射线（复用弹道层 mask），按命中物分类：
  | 命中 | 标点类型 | 队友响应 |
  |---|---|---|
  | 敌人 | `Attack` | Engage 并优先集火该目标，回话"集火标记目标" |
  | 掉落物/容器 | `Loot` | PingMove 前往并停留（拾取逻辑本期不做），回话确认 |
  | 地面/障碍 | `Move` | PingMove 前往，到位转 Hold，回话确认 |
  | 空（超出射程） | `Move`（射程尽头） | 同上 |
- 同一时刻只保留一个有效标点（新标点覆盖旧的），15s 未消费自动过期。

### 5.2 呈现

- 世界内：标记图标（占位 quad + 颜色区分类型），复用特效系统的 Follow 挂载。
- 小地图：标点图标接入现有 MinimapSystem 图标层。
- 事件：`IPingEvent.OnPingCreated(type, pos, targetId)` / `OnPingConsumed`，队友 FSM、小地图、音效（未来）都是订阅者。

---

## 6. LLM 链路

### 6.1 LlmClient（纯 HTTP 封装）

- OpenAI 兼容的 `POST /chat/completions`（非流式 MVP；流式 SSE 留扩展位）。
- 覆盖 Kimi(Moonshot) / OpenAI / DeepSeek 等所有兼容端点；endpoint、apiKey、model 走**本地配置文件**（`UserSettings/llm_config.json`，gitignore，禁止入库；设置界面留输入位，见 §9）。
- 超时 8s 可配；取消令牌随场景切换统一取消；失败不抛异常，返回结构化 `LlmResult{ok, text, error}`。

### 6.2 Prompt 组装（PromptBuilder）

```
system: 人设卡（TbCompanion.personaPrompt）+ 世界观摘要 + 输出契约（§6.3）
user:   上下文快照——
        游戏状态（safe/combat）、触发事件类型、玩家HP/队友HP、
        当前威胁数、标点类型、最近 3 条已说台词（防复读）
```

### 6.3 输出契约（结构化）

要求 LLM 返回 JSON：

```json
{
  "say": "台词文本（必填，≤60字，符合人设）",
  "intent": "none | follow | hold | retreat（可选，白名单枚举）",
  "mood": "calm | tense | hurt（可选，驱动气泡样式）"
}
```

- 解析失败 → 丢弃 intent，say 字段提取失败则整包丢弃（转本地兜底台词）。
- intent 进裁决器：白名单校验 + **TTL 3s**（LLM 延迟产生的过期意图直接作废）+ 不得覆盖硬性状态（§4.2）。
- **请求节奏**：事件驱动 + 冷却。安全闲聊定时器 25~40s 随机（可配）；战斗事件台词走本地 bark 表（LLM 延迟跟不上战斗节奏，战斗台词不走 LLM——这是硬决策）。

### 6.4 断联降级（LinkState）

| 状态 | 判定 | 表现 |
|---|---|---|
| `Online` | 最近请求成功 | 正常 LLM 台词 |
| `Degraded` | 连续失败 1~2 次 | 台词混入"信号干扰"人设化表达（LLM 最后一次成功时被提示，或本地干扰台词表），气泡加干扰样式 |
| `Offline` | 连续失败 ≥3 次或未配置 API | 全部走本地 bark 表；首次进入 Offline 播一条"通讯中断"专属台词；后台每 30s 探测恢复 |

离线时 §4 的 FSM 与 §5 的标点响应**完全不受影响**——它们本来就不经过 LLM。

---

## 7. 配置表设计

### 7.1 `TbCompanion`（队友人设卡）

| 字段 | 说明 |
|---|---|
| id / name / nameKey | 标识与本地化键 |
| personaPrompt | 人设 system prompt（英文，进 LLM） |
| weaponConfigId | 持有武器 |
| maxHp / moveSpeed / followDist | 基础数值 |
| bark 触发参数 | 闲聊间隔、TTL、失败阈值等（或拆 TbCompanionTuning） |

### 7.2 `TbCompanionBark`（本地台词表，离线兜底 + 战斗台词）

| 字段 | 说明 |
|---|---|
| id / trigger | 触发键：safe_idle / combat_start / kill / ping_attack / ping_move / ping_loot / low_hp / link_lost / link_recovered… |
| textKey | 本地化键（沿用"用户可见文本先英文"约定） |
| weight / cooldown | 随机权重与单键冷却 |

---

## 8. 文件结构

```
Assets/GameScripts/HotFix/GameLogic/
├── Entity/Companion/CompanionEntity.cs        # 实体：HP/受击/血条/动画
├── FSM/Companion/                             # 6 个状态 + Context + Driver（套用敌人 FSM 模式）
├── System/CompanionSystem.cs                  # 生成/销毁、感知聚合（威胁列表、安全状态判定）
├── System/PingSystem.cs                       # 标点输入、射线分类、生命周期
├── AI/Llm/LlmClient.cs                        # HTTP 封装（UnityWebRequest）
├── AI/Llm/CompanionBrain.cs                   # 台词调度 + 意图裁决 + LinkState
├── AI/Llm/PromptBuilder.cs                    # 上下文快照 → prompt
├── IEvent/IPingEvent.cs / ICompanionEvent.cs
├── UI/CompanionSubtitleUI/                     # 底部字幕条（Prefab，AssetRaw/UI/）
└── Config/CompanionConfigMgr.cs               # TbCompanion / TbCompanionBark
```

---

## 9. 风险与决策

| 风险/决策点 | 分析 | 处置 |
|---|---|---|
| LLM 延迟破坏战斗节奏 | 1~5s 延迟做不了实时反应 | **硬决策**：战斗台词与战斗行为永远走本地；LLM 只负责安全期闲聊与低频率意图 |
| 项目无 HTTP 先例 | UnityWebRequest 在热更程序集可用（UnityEngine 模块）；AOT/裁剪需在 link.xml 留意 Networking 模块 | M3 先做真机验证项进 M5 清单惯例 |
| API Key 安全 | 严禁入库 | 本地 json 配置 + gitignore + 设置界面输入；仓库只放 `llm_config.example.json`；**M4 起本地加密存储，需兼容 Windows + Android**（用户 2026-09-08 明确要求）：AES-256 + 设备派生密钥（PBKDF2(deviceUniqueIdentifier+应用盐+bundleId)），单套代码跨平台，弃用 Windows 专属 DPAPI；读取兼容旧明文自动迁移 |
| LLM 输出失控（越权指令/出戏文本） | 契约 JSON + 白名单 + TTL + 长度截断 + 敏感 fallback | §6.3 裁决器 |
| 费用/频率失控 | 闲聊定时器 + 全局请求冷却（最小间隔 5s）+ 单局限额（可配，默认 60 次/局） | PromptBuilder 内计数 |
| 队友挡子弹/卡位 | 队友 collider 加入玩家弹道排除层；与玩家保持最小间距的避让 | 弹道 hitLayers 配置 |
| 敌人仇恨 | MVP：敌人索敌逻辑不变（只追玩家）；队友被攻击判定用"受击事件"而非敌人仇恨 | 后续再给敌人加目标选择 |

### 用户已确认的决策（2026-09-07）

1. **LLM 服务商**：按 OpenAI 兼容端点设计，endpoint/apiKey/model 全部可配置，**设置面板中加入 API 配置项**（M4）。
2. **对话呈现**：**底部字幕条**（不做头顶气泡）。
3. **队友战力**：**真实开火**造成伤害，复用武器/弹道链路。
4. **常驻范围**：战斗关卡与基地/经营场景**都出现**；玩家可切换队友是否跟随（跟随开关）。AI 队友定位为本作独特且核心的剧情推手与玩法支柱，需用心设计。
5. **断联表达**："信号干扰"式世界观化表达。
6. **本期范围**：先做到 M2（队友本体 + 标点系统）。

---

## 10. 实施步骤

| 里程碑 | 内容 | 验收 |
|---|---|---|
| **M1 队友本体** | CompanionEntity + 6 态 FSM + 跟随/护主 + 寻路 + 简化武器 + 血条 | 离线进 101：跟随玩家、遇敌护主、低血撤退、死亡倒地 |
| **M2 标点系统** | PingSystem + 三类标点 + 世界标记 + 小地图图标 + 队友响应 | 标点敌人队友集火、标点地面队友前往驻守 |
| **M3 LLM 链路** | LlmClient + PromptBuilder + 输出契约 + 人设卡表 + 气泡/字幕 UI + 安全闲聊 | 在线时安全状态个性对话、LLM 意图建议生效 |
| **M4 断联降级 + 设置** | LinkState 三态 + 本地 bark 兜底 + 干扰表达 + 设置界面 API 配置 | 拔网线/错 key 时行为无损、台词体现断联、恢复自动重连 |

M1/M2 不依赖任何网络，可先完整落地；M3/M4 是增量。

---

## 11. 结论

以"LLM 是嘴不是身体"为原则，把队友拆成**本地确定性 FSM（永远在线）**与**LLM 人格层（可断可降级）**两层。标点系统作为独立玩家向系统先行，既是队友的指令入口，本身也是搜打撤玩法的通用设施。全部行为链路复用现有 FSM/寻路/武器/事件设施，新增代码集中在 `Entity/Companion`、`FSM/Companion`、`AI/Llm`、`System/PingSystem` 四处。
