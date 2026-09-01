# 提案：对话系统设计方案

> 提案状态：MVP 已落地（2026-08-26，P1~P4 完成并实测通过；P0 交互仲裁器未做，见 docs/TODO.md）  
> 提出时间：2026-08-23  
> 提案路径：`docs/Proposal/narrative/dialogue-system.md`  
> 关联模块：  
> - `docs/modules/narrative/dialogue-system/`  
> 关联文档：  
> - `docs/Proposal/narrative/quest-system.md`（任务系统，依赖本系统的对话标志位）  
> - `docs/modules/combat/note-system/`（叙事收集品，同属 Narrative 命名空间）  
> - `docs/modules/ui/ui-render-architecture.md`（UI 渲染约定）

---

## 1. 背景

项目已有叙事相关的最小设施：`NoteSystem`（小纸条，E 键阅读静态文本）与 `InteractionPromptUI`（交互提示）。但纸条是单向静态文本，无法支撑：

- NPC 多轮对话（任务接取/交付的载体）；
- 分支选择（玩家选项影响走向）；
- 条件对话（根据任务进度/玩家状态说不同的话）。

搜打撤循环中，基地（Simulation 场景）是天然的 NPC 驻地：发布任务的军需官、剧情 NPC 都在这里。本提案按业界成熟方案设计数据驱动的对话系统。

### 1.1 业界方案参考

| 方案 | 代表 | 核心思想 | 对我们的适用性 |
|------|------|---------|---------------|
| 脚本语言外置 | Ink（《80 Days》）、Yarn Spinner | 对话写成领域脚本，运行时解释执行 | 功能强但引入新工具链，与项目"Luban 配表驱动一切"的惯例冲突 |
| 节点图编辑器 | Dialogic（Godot）、Fungus、xNode | 可视化连线编辑对话树 | 需要自研编辑器扩展，成本高 |
| **扁平节点表** | RPGMaker 事件指令、大量商业 MMO/RPG 内部做法 | 每行一个节点，`next` 字段串成链/树，条件与动作字段驱动分支 | 与 Luban xlsx 工作流天然契合，策划零学习成本，**采用** |

无论哪种方案，成熟实践的共识是三层分离：**内容数据（表）→ 运行时解释器（System）→ 呈现（UI）**。本设计严格遵守该分层。

---

## 2. 目标

### 2.1 本期目标（MVP）

1. 与场景 NPC 交互（E 键）触发对话。
2. 对话窗口逐条播放：说话人名字 + 正文 + 打字机效果 + 点击/E 键推进。
3. 支持选项分支（玩家 2~4 选 1）与条件分支（按对话标志位/任务状态走不同节点）。
4. 对话动作：设置对话标志位（供任务系统与其他对话消费）。
5. 对话数据全部走 Luban 配表，热更代码内解释执行。

### 2.2 本期不做

- 可视化节点编辑器（先用 xlsx，量大后再评估）。
- 配音、口型、立绘差分演出（立绘字段预留）。
- 对话中的镜头演出/角色动作指令（action 字段预留扩展位）。
- 多语言（当前用户可见文本统一英文，表结构预留本地化键）。

---

## 3. 总体架构

```
对话内容（Luban 表：TbDialogue / TbDialogueNode）
        │ ConfigSystem 加载
        ▼
DialogueConfigMgr ──► DialogueSystem（解释器：当前节点、推进、条件求值、动作执行）
                            │ IDialogueEvent（开始/节点推进/选项/结束）
                            ▼
                      DialogueUI（呈现：名字/正文/打字机/选项按钮）
                            ▲
DialogueEntity（场景 NPC 挂载，trigger + E 交互，复用 InteractionPromptUI）
```

分层原则：

- **解释器不知道 UI 存在**：`DialogueSystem` 只发事件，`DialogueUI` 订阅呈现。后续加"对话日志回顾"只是多一个订阅者。
- **UI 不知道表结构**：UI 只消费事件载荷里的"说话人/正文/选项列表"。
- **条件与动作走统一管线**：条件求值（Condition）与动作执行（Action）是对话系统与任务系统**共用**的设施，集中放在 `GameLogic.Narrative` 下，避免两套字符串解析。

---

## 4. 配置表设计

### 4.1 `TbDialogue`（对话头）

每行一段可触发的对话。

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | int | 对话 ID |
| `name` | string | 策划注释用名（不显示） |
| `startNode` | int | 起始节点 ID |
| `onceOnly` | bool | 是否只触发一次（读后写标志位 `dlg_seen_{id}`） |
| `priority` | int | 同一 NPC 挂多段对话时的选择优先级（大优先，配合条件做"有任务时先说任务对话"） |

### 4.2 `TbDialogueNode`（对话节点，扁平表）

每行一个节点，`next` 串联。这是 RPGMaker 事件指令的表格式写法。

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | int | 节点 ID（全局唯一） |
| `dialogueId` | int | 所属对话 |
| `type` | string | `line`（台词）/ `choice`（选项组）/ `setflag`（写标志位）/ `end`（结束） |
| `speaker` | string | 说话人名（显示用，英文） |
| `text` | string | 正文（`line`/`choice` 的提示语） |
| `choices` | string | 选项，格式 `文本1->节点A\|文本2->节点B`（仅 `choice`） |
| `condition` | string | 进入本节点的条件表达式，空=无条件（见 §4.3） |
| `action` | string | 到达本节点时执行的动作（见 §4.3） |
| `next` | int | 下一节点 ID，0/空=对话结束 |

`setflag` 与 `end` 类节点不占 UI 拍子，解释器连续执行直到下一个 `line`/`choice`。

### 4.3 条件与动作表达式（与任务系统共用）

刻意设计为**最小领域语言**，只做键值比较与标志位读写，不做算术和脚本：

- 条件：`flag:has_key` / `flag:!has_key` / `quest:1001:active` / `quest:1001:done` / `level:>=3`，多个用 `&` 连接（与关系）。
- 动作：`flag:+has_key` / `flag:-has_key` / `quest:accept:1001` / `quest:turnin:1001` / `give:gold:200`。

解析器实现为 `NarrativeCondition` / `NarrativeAction` 静态工具类。**不引入 Lua/Reflection  eval**，理由：①HybridCLR 热更下反射性能与兼容性坑多；②策划填错可在导表期校验（Luban 的 validator 或 ConfigMgr 加载时扫一遍引用完整性）。

### 4.4 配表示例

```
TbDialogueNode:
id    dialogueId  type    speaker   text                              choices                     condition            action          next
9001  900         line    Quartermaster  "Back in one piece. Good."                                 quest:1001:inactive                       9002
9002  900         choice  Quartermaster  "Need work?"                  "Accept job->9003|Later->9005"                                          0
9003  900         setflag                                                          (空)                                        quest:accept:1001   9004
9004  900         line    Quartermaster  "Clear out level 101. Come back alive."                                                     0
9005  900         line    Quartermaster  "Don't slack off."                                                                             0
9010  900         line    Quartermaster  "101's clean? Nice work."                                quest:1001:done                        0
```

对话 900 的入口选择：`DialogueSystem` 拿到 dialogueId 后从 `startNode` 起按 condition 跳到第一个满足条件的节点（9010 与 9001 这种"条件开场白"模式）。

---

## 5. 运行时设计

### 5.1 `DialogueSystem`

- 挂载：基地走 `ProcedureSimulation` 的 SimulationRoot 挂载；战斗场景暂不挂（战斗内不对话）。
- 状态机：`Idle → Playing(line) → WaitingChoice → Idle`。
- 职责：开始对话（按 NPC 的可用对话列表 + 条件选出一段）、推进（E/点击）、选项选择、执行 `setflag`/action 节点、发事件。
- 事件 `IDialogueEvent`：`OnDialogueStarted(int dialogueId)` / `OnDialogueLine(int nodeId, string speaker, string text)` / `OnDialogueChoices(int nodeId, string[] options)` / `OnDialogueEnded(int dialogueId)`。

### 5.2 对话标志位（Flag Blackboard）

- 存档新增 `DialogueSaveData`：`flags: List<string>`（已设置的 flag 键集合）。
- API：`DialogueFlagSystem.Set/Has/Clear(string key)`，变动即存（`SaveSystem.Flush()`）。
- 这是对话与任务共用的轻量"数据黑板"：对话写、任务读、对话再读任务的完成状态。比互相直接引用接口的耦合度低——与之前角色系统收敛 `PlayerStateContext` 的思路一致：状态归一处，读写走窄口。

### 5.3 `DialogueEntity`（NPC 挂载）

- 字段：`dialogueIds: List<int>`（该 NPC 能说的对话，按 priority 选）。
- 触发区：SphereCollider(trigger)，进区 `InteractionPromptUI` 显示 "Press E to Talk"。
- **注意**：`NoteSystem`/`PortalSystem`/`LootContainerSystem` 目前共用 `OnInteractPressed` 且无仲裁（`docs/TODO.md` 已记"统一 IInteractable 仲裁器待做"）。对话系统是第 4 个消费者，**本提案包含落地该仲裁器**：`InteractionSystem` 统一收 E 键，按"距离最近 + 类型优先级"分发给当前可交互对象，各系统改为注册 `IInteractable` 而不再各自监听。

### 5.4 `DialogueUI`

- TEngine `UIWindow`，prefab 放 `Assets/AssetRaw/UI/DialogueUI/`（遵循 UI prefab 化约定）。
- 布局：底部约 1/4 屏对话框（名字板 + 正文 + 继续提示），选项时中部弹出纵向按钮列表。
- 打字机效果：每帧按字符数推进，`EventSystem` 点击/E 键先补全当前行、再推进下一行（业界标准两拍交互）。
- 光标：对话窗口 `TimeScaleWhenVisible` 设为 1（不暂停，对话时世界继续）但需显示系统光标吗？——**不**。对话保持战斗光标语义：键盘 E 推进、数字键 1~4 选选项，不显示系统鼠标，避免与准星/光标管理再打架（参考刚修的准星同步问题）。后续若改鼠标点选，按 `CursorManager` 配对规则接入。

---

## 6. 存档

```csharp
[Serializable]
public class DialogueSaveData
{
    public bool initialized;
    public List<string> flags = new List<string>();  // 含 dlg_seen_{id} 与剧情 flag
}
```

`SaveData` 加 `dialogue` 段，版本号 1→2，`Migrate` 中旧档补空段。

---

## 7. 分期与工作量估算

| 阶段 | 内容 | 预估 |
|------|------|------|
| P0 仲裁器 | `InteractionSystem` 统一交互仲裁，Note/Portal/Loot 改注册式 | 0.5 天 |
| P1 数据层 | 两张表 + `__beans__` + 白名单 + `DialogueConfigMgr` + 条件/动作解析器 | 0.5 天 |
| P2 运行时 | `DialogueSystem` + `DialogueFlagSystem` + `IDialogueEvent` + 存档段 | 0.5 天 |
| P3 表现层 | `DialogueUI` prefab + 打字机 + 选项按钮 | 0.5 天 |
| P4 内容接线 | 基地摆 1 个 NPC（占位方块+名字板），接任务系统 P0 的示例链 | 0.5 天 |

---

## 8. 风险与对策

- **交互仲裁器改动面**：动到 Note/Portal/Loot 三个已验收系统——仲裁器先做兼容层（保留旧监听但加"已被仲裁"短路），逐个迁移，每迁一个 Play 验证一次。
- **表结构返工**：MVP 只用 `line/choice/setflag/end` 四种节点，但 `type` 字段是字符串而非枚举，新类型（如 `playanim`）只加解释器分支不改表。
- **与任务系统的边界**：对话只负责"说"和"写 flag/发动作"，**不存任务进度**；任务系统只读 flag 和领域事件，**不驱动 UI 台词**。两边唯一耦合点是 §4.3 的表达式词汇表。
