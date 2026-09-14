# AI 队友好感度与记忆系统设计方案

> 状态：**已落地（2026-09-14）**，M1-M4 全部完成并 MCP 实测通过（含真实 LLM 召回验证）。
> 涉及模块：`docs/modules/ai/companion-system/`

## 对齐决议（2026-09-14）

1. **信息面板两种打开方式都要**：队友交互选项 + 聊天窗口按钮。
2. **好感度来源数值全部走配置表**；可赠送物品在 `item.xlsx` 加 `affinityValue` 字段，**NPC 能否接受该物品**也在 `item.xlsx` 加字段（`companionAccept`，0=拒收）。
3. **聊天记忆筛选用方案 B**：LLM 打标（聊天契约加 `memorable` 字段）。
4. **记忆调用按候选 2（LLM 驱动）开发**：模型输出 `recall` 请求 → 客户端查记忆 → 第二次请求带记忆内容。详细调用规则用户后续提供，本次先把两阶段链路做出来。
5. **记忆写入提示要做**，文案方向为"XXX 对你的话/礼物感到好奇"——**含蓄表达，不暴露随机判定结果**（写没写成玩家不知道）。
6. **档位暂不附带实际收益**（仅人格差异）；此问题在本文 §10 挂起，等用户后续方案。

## 0. 需求复述

1. 好感度系统：增加好感度时有提示；NPC 有信息面板展示好感度。
2. 好感度分 4 档，档位越高人格越拟人，但保持统一人设不割裂。
3. 记忆功能：记忆**内容**与记忆**功能**解耦——什么好感度区间能记什么类型，走配置表，策划可改。
   - T1（初始）：只记礼物，且**随机记忆**（不会全记）
   - T2：追加记忆玩家的聊天片段和对 AI 的评价
   - T3：追加记忆**全部**礼物（不再随机遗忘）
   - T4：方案预留（建议：记忆容量扩大 + 战斗事件记忆）
4. 记忆只存本地；**NPC 主动确认调用记忆时**，才把这段记忆注入 LLM prompt。调用规则待定（见 §6 三个候选）。

## 1. 好感度系统（AffinitySystem）

### 数值模型

- 每个队友一份好感度经验值（当前只有 Exusiai id=1，结构上按 companionId 分桶，多队友预留）。
- 4 档阈值走配置（见 §7 `companionaffinity.xlsx`），默认建议：

| 档位 | 名称 | 累计好感 | 人格倾向（在统一人设上叠加的关系姿态） |
|---|---|---|---|
| T1 | 陌生 Stranger | 0 | 职业化、保持距离，"老板"式称呼，简短汇报体 |
| T2 | 熟悉 Familiar | 100 | 开始闲聊、会开玩笑，主动提起玩家说过的事 |
| T3 | 信任 Trusted | 300 | 关心玩家状态，语气软，会记住所有礼物并回赠情感反馈 |
| T4 | 默契 Bonded | 600 | 近似老友/家人，口癖与内部梗，战斗中心意相通的播报 |

### 好感度来源（可配置，建议值）

| 来源 | 数值 | 限制 |
|---|---|---|
| 赠送礼物 | 按物品 `affinityValue`（item.xlsx 加列，0=不可赠送） | 无限制，主要来源 |
| 主动聊天一轮 | +2 | 每 10 分钟冷却（防刷） |
| 共同成功撤离 | +15 | 每局一次 |
| 战斗中保护玩家（击杀追击玩家的敌人） | +5 | 每局最多 3 次 |

> 数值都是占位建议，对齐时可改；全部进配置表。

### 提示反馈

- 好感度增加时：队友头顶飘字 `+N ♥`（复用伤害飘字/小纸条光圈的现有范式），升格档位时播放专属 bark（如 "Hey... thanks, boss."）+ 更明显的提示。
- 升档事件 `OnAffinityTierUp` 同时解锁对应记忆类型（见 §3）。

### 信息面板（CompanionInfoUI）

- 打开方式（已定）：**两个都要**——①经营场景与队友交互（E）时出选项；②聊天窗口（T）上加信息按钮。
- 内容：名字 + 立绘/模型快照（复用 BuildingInfoUI 的 RenderTexture 快照方案）+ 当前档位名 + 好感进度条（到下一档）+ 当前档位人格描述 + 记忆列表（按类型分页，见 §3）+ 赠礼入口按钮。

## 2. 人格分档（配置结构）

原则：**统一人设基底 + 档位关系姿态叠加**，不是四份独立人设（避免割裂）。

- `companion.xlsx` 的 `personaPrompt` 保持为不变基底（身份、口癖、世界观立场）。
- 新表 `companionpersona.xlsx`：`companionId, tier, promptAdd`（该档位追加的关系姿态描述，如 T1="You keep things professional..."）。
- `PromptBuilder.BuildSystem/BuildChatSystem` 组装时：`基底 + 当前档位 promptAdd`。切档即时生效（下次请求用新 prompt）。
- `companionbark.xlsx` 加 `minTier` 列：高档位轮播语录池更大更亲昵；低档位语录在高档位仍可命中（池子累计），保持人格连续。
- 可选：档位影响 LLM 请求参数（如 T4 提高 temperature / 放宽 say 字数上限）。建议先不做，对齐时再定。

## 3. 记忆系统（CompanionMemorySystem）—— 内容与功能解耦

### 数据结构

```csharp
MemoryEntry { int type; string content; long timestamp; }
```

- `content` 存**事实文本**（如礼物=物品名×数量；聊天=玩家原句截取），不存 LLM 生成文本。
- 本地存档：`SaveData` 新增 `companionMemory` 段（companionId → List\<MemoryEntry\>），变动即存，随槽位切换失效。

### 解耦核心：记忆规则配置表（companionmemoryrule.xlsx）

| 列 | 说明 |
|---|---|
| `memoryType` | 记忆类型枚举：Gift / ChatPlayer / ChatAboutAI / BattleEvent（可扩展） |
| `minTier` | 该类型从哪个档位开始记录 |
| `maxCount` | 该类型容量上限 |
| `sampleRule` | 容量满时的淘汰规则：`fifo`（先进先出）/ `random_forget`（写入时按概率直接不记）/ `random_evict`（随机踢一条旧的） |
| `writeChance` | 写入概率（1=必记；T1 礼物随机记忆=0.5 之类） |

你的三档需求映射：

| 档位 | Gift | ChatPlayer / ChatAboutAI | 实现 |
|---|---|---|---|
| T1 | `minTier=1, maxCount=3, writeChance=0.5, random_evict` | 不记（minTier=2） | "随机记忆、记不全" |
| T2 | 同上 | `minTier=2, maxCount=5, writeChance=1, fifo` | 追加聊天/评价记忆 |
| T3 | `writeChance=1, maxCount=20`（覆盖规则按档位取生效行） | 同上 | 礼物全记 |

> 实现方式：查询某类型在**当前档位**生效的规则行（minTier ≤ currentTier 的最高 minTier 行）。改表即可调整"什么档位记什么"，代码零改动——这就是解耦。

### 聊天记忆的捕获与筛选（已定：方案 B）

聊天契约的 JSON 加 `"memorable": true/false` 字段，模型在回复时顺带判断玩家这句话值不值得记住（能识别"对 AI 的评价"这类语义）。客户端收到 `memorable=true` 且当前档位规则允许时，按 `ChatPlayer`/`ChatAboutAI` 类型写入（类型分类也由契约字段给出，如 `"memoryType": "chat_player|chat_about_ai|none"`）。

## 4. 赠礼功能（礼物记忆的来源，当前不存在，需新建）

- 入口：CompanionInfoUI 的"赠礼"按钮 → 弹出仓库物品选择列表（只显示 `affinityValue>0` 且 `companionAccept==1` 的物品）。
- 赠送：扣仓库物品 → 好感度 +affinityValue → 按 §3 规则尝试写入 Gift 记忆 → 飘字提示 → 触发一条收礼 bark/LLM 反应（收礼瞬间是天然的记忆调用时机，见 §6）。
- `item.xlsx` 加两列：`affinityValue`（好感值，0=不可赠送）、`companionAccept`（1=NPC 接受，0=拒收——拒收时赠礼列表直接不显示）。
- 礼物品味差异（喜欢/讨厌系数）本次不做，留待后续。

## 5. 信息展示与提示 UI 清单

| UI | 内容 |
|---|---|
| 好感飘字 | 队友头顶 `+N ♥`，升档时特殊样式 |
| CompanionInfoUI（新 prefab） | 档位/进度条/人格描述/记忆列表/赠礼按钮 |
| 记忆写入提示（已定，含蓄版） | **尝试写入时**（无论随机判定成败）字幕一行，文案方向："XXX 对你的礼物感到好奇…" / "XXX 似乎在琢磨你说的话…"——不暴露随机记忆是否真正写入 |

## 6. 记忆调用规则（已定：候选 2 LLM 驱动；详细规则用户后续提供）

记忆默认不进 prompt。链路为两阶段：

1. 聊天契约加 `"recall": ["Gift", ...]` 字段（可空数组）。模型判断当前对话需要回忆时，在回复里声明要查的记忆类型。
2. 客户端解析到非空 `recall` → 从 CompanionMemorySystem 取对应类型的记忆内容（受档位规则过滤）→ **发起第二次请求**，system prompt 中注入召回的记忆段落（`PromptBuilder` 加 `AppendRecalledMemories`），模型基于记忆给出最终回复。
3. 节流：每次聊天对话最多触发一次召回（防止循环召回）；召回失败（无记忆）时第二次请求标注"没有相关记忆"，让模型自然带过。

> 成本注意：触发召回的聊天轮 = 2 次 LLM 请求。详细调用规则（什么场景允许召回、召回预算）用户后续提供，本次先把链路打通。

## 7. 配置表变更清单

| 表 | 变更 |
|---|---|
| `companionaffinity.xlsx`（新） | tier / 档位名 key / 累计阈值 / 人格描述 key + 各来源好感数值（礼物系数/聊天/撤离/护驾/冷却） |
| `companionpersona.xlsx`（新） | companionId / tier / promptAdd（英文，LLM 用） |
| `companionmemoryrule.xlsx`（新） | memoryType / minTier / maxCount / sampleRule / writeChance |
| `item.xlsx` | 加 `affinityValue` + `companionAccept` 两列 |
| `companionbark.xlsx` | 加 `minTier` 列 + 高档位语录 |
| `localization.csv` | 档位名、人格描述、面板 UI、飘字、记忆写入提示等词条 |

## 8. 代码结构

```
GameLogic/
  Shared/CompanionAffinitySystem.cs     # 好感度数值/档位/事件（仿 SkillSystem 存档模式）
  Shared/CompanionMemorySystem.cs       # 记忆读写/规则查询/容量淘汰
  Config/CompanionAffinityConfigMgr.cs  # 阈值表包装
  Config/CompanionMemoryRuleConfigMgr.cs
  IEvent/ICompanionAffinityEvent.cs     # OnAffinityChanged / OnAffinityTierUp / OnMemoryRecorded
  AI/Llm/PromptBuilder.cs               # 档位 promptAdd 注入 + 记忆注入口（Recall 时调用）
  UI/CompanionInfoUI/                   # 信息面板 + 赠礼列表（prefab）
  UI/ 飘字                               # 复用现有飘字范式
```

存档：`SaveData` 加 `companionAffinity`（companionId → exp）与 `companionMemory` 两段，`SwitchSlot` 失效链挂两个新系统。

## 9. 里程碑拆分（对齐后按此开发）

- **M1 数值与存档**：AffinitySystem + MemorySystem + 配置表 + 存档段 + 飘字提示（无 UI 面板，无 LLM 注入）
- **M2 面板与赠礼**：CompanionInfoUI + 赠礼链路 + bark minTier
- **M3 人格分档**：companionpersona 接入 PromptBuilder + 4 档 prompt 文本撰写
- **M4 记忆召回**：按 §6 选定方案实现召回注入（规则细节届时确认）

## 10. 挂起问题（用户后续给方案）

1. **档位实际收益**（已定方向：暂只做人格差异）——T4 高档位是否附带实际收益（战斗播报更详细、决策预算提高、回赠礼物等），用户后续出方案，届时在此补充。
2. **记忆召回详细规则**——什么场景允许召回、召回预算/频率上限、战斗态是否禁召回等，用户后续提供；本次 M4 只实现两阶段链路与"每次对话最多一次召回"的基础节流。
3. **礼物品味差异**（喜欢/讨厌系数）——本次不做，预留扩展位。
