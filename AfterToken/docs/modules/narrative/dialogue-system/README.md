# Dialogue System（对话系统）

## 概述

数据驱动的 NPC 对话：Luban 扁平节点表（RPGMaker 事件指令式）→ `DialogueSystem` 解释执行 → `IDialogueEvent` → `DialogueUI` 呈现。支持多轮台词、打字机、选项分支、条件分支、对话动作与标志位持久化。设计方案：`docs/Proposal/narrative/dialogue-system.md`。

## 组成

| 模块 | 文件 | 职责 |
|------|------|------|
| 对话头表 | `Configs/GameConfig/Datas/dialogue.csv` → `cfg_tbdialogue.json` | `TbDialogue`（map）：`startNode` / `onceOnly` / `priority` |
| 节点表 | `Configs/GameConfig/Datas/dialoguenode.csv` → `cfg_tbdialoguenode.json` | `TbDialogueNode`（map）：`type`(line/choice/setflag/end) / `speaker` / `text` / `choices`(`文本->节点\|...`) / `condition` / `action` / `next` |
| 编辑器 | `Assets/Editor/Dialogue/DialogueEditorWindow.cs`（Tools/Dialogue/Dialogue Editor） | 可视化编辑两张表（CSV 数据源）：节点图视图（卡片 + 连线自动布局，横向/纵向可切换）+ 详情编辑、引用校验、一键"保存并导表" |
| 配置管理 | `GameLogic/Config/DialogueConfigMgr.cs` | 对话头/节点查询，`GetStartNode` 按条件选开场节点 |
| 解释器 | `GameLogic/System/DialogueSystem.cs` | 单例（`ProcedureSimulation` 挂载）。状态机 Idle→Playing→WaitingChoice；E/回车推进（打字中先补全）、数字键 1~4 选选项、走出触发区打断 |
| 呈现 | `GameLogic/UI/DialogueUI/DialogueUI.cs` + `Assets/AssetRaw/UI/DialogueUI/DialogueUI.prefab` | 底部约 1/4 屏对话框 + 中部选项按钮；打字机 45 字/秒；不暂停、不显示系统光标 |
| 条件/动作 | `GameLogic/Narrative/NarrativeCondition.cs` / `NarrativeAction.cs` | 最小领域语言（与任务系统共用）：`flag:key` / `flag:!key` / `level:>=N`；`flag:+key` / `flag:-key` / `give:gold:N`；`quest:*` 待任务系统落地 |
| 标志位 | `GameLogic/Narrative/DialogueFlagSystem.cs` | 数据黑板（`SaveData.dialogue.flags`，变动即存）；onceOnly 对话读写 `dlg_seen_{id}` |
| 事件 | `GameLogic/IEvent/IDialogueEvent.cs` | Started / Line / Choices / Ended |

## 如何新增一段对话

**推荐：用编辑器**（菜单 Tools/Dialogue/Dialogue Editor）——左栏新建对话、右栏加节点，字段即填即存，点"保存并导表"一次完成；带引用校验（next/选项指向不存在的节点、ID 重复会亮黄警告）。

手动方式（等价）：
1. `dialogue.csv` 加一行对话头（id / startNode / onceOnly / priority）
2. `dialoguenode.csv` 加节点行：`line` 串台词、`choice` 配 `choices`、`setflag` 配 `action`、`next=0` 结束
3. `npc.xlsx` 把 NPC 的 `dialogueId` 指向该对话
4. 项目根跑 `Configs/GameConfig/gen_code_bin_to_project.bat`

> 2026-08-26 起对话两张表从 xlsx 改为 CSV 数据源（编辑器直读直写，无需 openpyxl；Excel 仍可打开编辑）。

## 交互规则

- 靠近 NPC 出 "Press E to Talk" → E 开对话（`NpcSystem` 收起提示，结束后玩家还在区内则恢复）
- E/回车：打字中补全 → 再按推进；选项：数字键 1~4 或点按钮
- Esc：走 `SimulationInputSystem` 关窗链，对话最先被关；走出触发区对话被打断
- 对话进行中：`SimulationInputSystem` 不广播 E 交互（不会重触发交谈/传送门）

## 注意

- 解释器不知道 UI 存在（只发事件），UI 不知道表结构（只消费事件载荷）
- 条件/动作刻意不做算术与脚本，策划填错在运行期告警；`quest:*` 词汇待任务系统落地后生效
- 对话不暂停游戏、不动光标/准星（键盘驱动）
- 统一 IInteractable 仲裁器仍待做（NPC/Portal/Note/容器触发区勿重叠）
