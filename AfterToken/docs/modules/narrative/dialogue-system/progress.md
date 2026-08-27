# Dialogue System 进度

## 已完成
- [x] Luban 新表 `TbDialogue`（对话头：startNode/onceOnly/priority）+ `TbDialogueNode`（扁平节点：type/speaker/text/choices/condition/action/next）；`__beans__`/`__tables__`/白名单（CustomTemplate 与生成拷贝）同步；测试数据：对话 900（Quartermaster，2 台词 + setflag + 2 选项分支）、901（Doc，2 台词），npc.xlsx 两个测试 NPC 已填 dialogueId
- [x] `DialogueConfigMgr`（对话头/节点查询 + 条件开场节点选择）
- [x] `NarrativeCondition` / `NarrativeAction`（最小领域语言：flag/level/give:gold 已生效，quest:* 告警占位待任务系统）
- [x] `DialogueFlagSystem` + `SaveData.dialogue` 存档段（flags 列表，变动即存；旧档缺字段自动取默认值，无需版本迁移）
- [x] `DialogueSystem` 解释器（Idle→Playing→WaitingChoice 状态机；条件节点跳过；setflag/end 不占 UI 拍子；E 推进两拍交互、数字键 1~4 选项；同帧防连跳；出区打断）
- [x] `DialogueUI`（底部 280px 对话框：名字/打字机正文/E 提示 + 中部 4 选项按钮；UILayer.Top 不暂停不动光标；订阅 IDialogueEvent 呈现）
- [x] 接线：`NpcSystem`（E 交谈驱动对话、提示收起/恢复、出区打断）、`SimulationInputSystem`（对话中 E 不广播交互、Esc 链最先关对话）、`ProcedureSimulation` 挂载
- [x] Play 实测（2026-08-26）：E 开对话（名字+正文+提示显示正确）→ E 推进过 setflag 节点到选项 → 选项按钮 1/2 显示正确 → 选 1 到分支台词 → 推进结束关窗、提示恢复、`IsPlaying=False`；flag `met_quartermaster` 已写入存档；Console 0 错误
- [x] 对话编辑器（2026-08-26）：两张表改为 CSV 数据源（编辑器直读直写，引号字段/逗号文本无损；Excel 仍可打开）；`Assets/Editor/Dialogue/DialogueEditorWindow.cs`（Tools/Dialogue/Dialogue Editor）支持对话/节点增删改、引用校验（next/选项/起始节点存在性、ID 重复）、一键"保存并导表"（自动跑 gen bat + Refresh）；无头实测加载/保存/导表链路通过

## 进行中
（无）

## 待办
- [ ] 任务系统落地后启用 `quest:*` 条件/动作词汇（见 `docs/Proposal/narrative/quest-system.md`）
- [ ] 对话中镜头/演出节点类型（camera/anim/fx/wait，方案在提案 §8，后续接 Timeline+Cinemachine）
- [ ] onceOnly/priority 的完整内容验证（当前测试对话均为 onceOnly=false）
- [ ] 对话日志回顾（再订阅 IDialogueEvent 即可）
- [ ] 统一 IInteractable 仲裁器（Portal/Note/NPC/容器触发区重叠时）

---

> 状态说明：
> - 当前总状态：🟡（MVP 可用，任务系统联动与演出未做）
> - 每次更新后同步 `docs/TODO.md`
