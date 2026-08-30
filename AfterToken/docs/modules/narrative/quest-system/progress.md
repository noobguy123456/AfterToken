# 任务系统（Quest System）进度

> 设计提案：`docs/Proposal/narrative/quest-system.md`（已按提案落地 MVP）

## 当前状态

✅ MVP 已完成并 Play 实测通过（2026-08-30）。

## 已完成

- [x] **配置表**：`TbQuest`（quest.csv：id/name/desc/giverNpc/prereq/rewards/nextQuest）+ `TbQuestObjective`（questobjective.csv：id/questId/type/targetId/count/desc），CSV 数据源（同 dialogue.csv 惯例），`_tableFiles` 白名单两处已同步
- [x] **QuestConfigMgr**：表包装 + questId→目标列表索引
- [x] **QuestSystem**（`Narrative/QuestSystem.cs`，纯 C# 静态类，跨场景常驻）：四态状态机 Inactive→Active→ReadyToTurnIn→Completed；事件订阅走全局 GameEvent（`IEnemyEvent.OnEnemyDied`/`IItemEvent.OnWarehouseChanged`）+ `DialogueFlagSystem.OnFlagSet` + `CrossPlayLink.OnBattleExtracted` 末尾钩子；collect 按仓库当前持有数重算（塔科夫式"带回来才算"），kill/extract 跨局累计；奖励复用 `CurrencySystem.AddGold`/`PlayerProfileSystem.AddExp`/`InventorySystem.AddItem`
- [x] **存档**：`SaveData.quest` 段（QuestEntry 状态字符串 + 按 objectiveId 存进度，策划改表不串位），变动即存；`QuestSystem.Reset()` GM 口
- [x] **叙事词汇落地**：条件 `quest:id:active/ready/done/accept`，动作 `quest:accept:id`/`quest:turnin:id`（NarrativeCondition/NarrativeAction 原 pending 桩已接线）
- [x] **IQuestEvent**：OnQuestAccepted / OnObjectiveProgress / OnQuestReadyToTurnIn / OnQuestCompleted
- [x] **IEnemyEvent.OnEnemyDied 补 configId 参数**（提案 P0）：发布处 EnemyDeadState + 订阅处 DropSystem/PortalSystem/ProjectileSystem 同步改签名
- [x] **QuestLogUI**（prefab `Assets/AssetRaw/UI/QuestLogUI/`，TEngine UIWindow）：左列表（[Ready]/[Active]/[Available]/[Done] 标签，不可接取的任务不显示防剧透）+ 右详情（描述/目标 x/y/奖励预览）；基地 J 键开关，ESC 关窗链已注册进 SimulationInputSystem；不暂停游戏
- [x] **示例任务链**：1001 Pest Control（杀 5 个 9001，Quartermaster）→ 1002 Bring Back Timber（仓库 3 木料，prereq=quest:1001:done）→ 1003 Into the Factory（撤离 101，Doc）；对话接线：dialogue 900/901 起始节点前插条件节点链（ready→turnin 优先，accept→接取，落回默认开场白）
- [x] **NPC 头顶任务标记**（MMO 惯例三态，2026-08-30）：`QuestConfigMgr.GetQuestsByGiver(npcId)` 反向索引 + `NpcEntity.RefreshQuestMarker()`；TextMeshPro 文本符号挂头顶 y=2.7（fontSize 3.5，BillboardFaceCamera 面向相机）。优先级：任意 ReadyToTurnIn → 黄"?" > 任意 CanAccept → 黄"!" > 任意 Active → 灰"!" > 隐藏；订阅 IQuestEvent 四事件实时刷新，填 giverNpc 即自动生效
- [x] **NPC 对话任务枢纽**（2026-08-30）：NPC 有可交付/可接取任务时，对话不再从任务节点链自动开始，改为先播默认问候语（无条件起始节点）并出选项菜单——"Turn in: X"（可交付在前）/"Accept: Y" + "Just chatting."（走默认问候的后续节点）；选项由 `DialogueSystem.CollectQuestChoices` 扫描节点表 `quest:id:ready/accept` 条件动态生成（上限 3 任务项+1 闲聊），无需额外配表；任务项选中后跳到原任务节点（offer 台词+接取确认窗 / 交付台词+发奖励），交付后沿 next 链可继续自动 offer 下一任务
- [x] **任务接取确认窗**（QuestAcceptConfirmUI，2026-08-30）：`quest:accept:id` 不再直接接取，到达节点时弹确认窗（任务名+描述，Confirm/Cancel 按钮 + Enter/Esc 快捷键，玩家确认才走 `QuestSystem.Accept`）；弹窗期间 DialogueSystem 不响应推进键（Update 守卫），对话被结束（Esc 关对话/走出触发区）时弹窗自动按取消关闭；不可接取时跳过并告警
- [x] **任务追踪 HUD**（QuestTrackerUI，2026-08-30）：屏幕左侧中部常驻列出进行中任务（Active/ReadyToTurnIn）的名称+目标 x/y 进度，Ready 任务名转绿并加 "(Ready)" 后缀；纯展示不挡射线（根 CanvasGroup blocksRaycasts=false）；基地与战斗场景均常驻（ProcedureSimulation/ProcedureBattle 打开），订阅 IQuestEvent 全量重建刷新

## Play 实测记录（2026-08-30）

- accept(1001) → 5 次 OnEnemyDied(9001) → ReadyToTurnIn → TryTurnIn → Completed，+200G ✓
- collect：仓库已有木料时 accept(1002) 立即 ReadyToTurnIn（持有数重算生效）✓
- extract：Accept(1003) → CrossPlayLink.OnBattleExtracted(101) → ReadyToTurnIn → 交付 ✓
- 对话起始节点选择（枢纽改造前）：1001 完成后 900 对话从 9103（木料任务 offer）开始；全部完成后回落 9001 默认开场 ✓
- 对话任务枢纽（2026-08-30 改造后）：有可接任务时 900 对话出选项 "1. Accept: Pest Control / 2. Just chatting."；Just chatting 走默认闲聊链（9006）；Accept 项播 offer 台词并弹接取确认窗，Confirm 后才接取 ✓；Ready 状态下选项变 "1. Turn in: Pest Control"，选中即交付发奖励（+200G）✓
- QuestLogUI：列表三任务标签正确（[Done]/[Available]x2），详情进度 5/5、奖励格式化 "200G + 50EXP" ✓
- 坑位：新建 UI prefab 根节点必须挂 Canvas + GraphicRaycaster（否则 UIModule 报 "Not found Canvas in panel"）；改 AssetRaw 后需 SimulateBuild 才能在 Editor Simulate 模式加载

## 待完成

- [ ] 任务板（giverNpc=0）实体
- [ ] GM 命令挂接 quest reset/accept/turnin（现有 GM 只有 gold/exp/unlock/profile）
- [ ] 对话编辑器对 quest 条件/动作的可视化提示

## 如何新增任务（策划流程）

1. `quest.csv` 加任务行（prereq 用 `quest:1001:done&level:>=2` 表达式，rewards 用 `gold:200|exp:50|item:10004:2`）
2. `questobjective.csv` 加目标行（type=kill/collect/extract/flag，kill/extract/collect 的 targetId 填数字 ID，flag 填标志键）
3. 运行 `Configs/GameConfig/gen_code_bin_to_project.bat` 导表
4. 在 `dialoguenode.csv` 给发布 NPC 加条件节点：`condition=quest:id:accept action=quest:accept:id`（到达节点即弹接取确认窗，玩家确认才接取）与 `condition=quest:id:ready action=quest:turnin:id`（交付），插入该 NPC 起始节点链前方
5. 填了 `giverNpc` 后头顶任务标记自动生效，无需额外配置
