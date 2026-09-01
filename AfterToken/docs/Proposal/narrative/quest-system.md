# 提案：任务系统设计方案

> 提案状态：已落地（2026-08-30，MVP 完成并 Play 实测通过，详见 `docs/modules/narrative/quest-system/progress.md`）  
> 提出时间：2026-08-23  
> 提案路径：`docs/Proposal/narrative/quest-system.md`  
> 关联模块：  
> - `docs/modules/narrative/quest-system/`  
> 关联文档：  
> - `docs/Proposal/narrative/dialogue-system.md`（对话系统，任务接取/交付的对话载体）  
> - `docs/modules/shared/unlock-system/`（条件判定与解锁链的先例）  
> - `docs/modules/shared/cross-play-link/`（撤离奖励联动的先例）

---

## 1. 背景

搜打撤循环目前缺"目的层"：玩家进关卡只有"杀光/撤离"，没有长期目标牵引。业界对这一层的成熟解法是任务（Quest）系统：

- **WoW/范式**：任务 = 状态机（未接 → 进行 → 可交付 → 已完成），目标（Objective）计数驱动；
- **Skyrim/范式**：任务分阶段（Stage），阶段内挂多个目标；
- **逃离塔科夫/鸭科夫（本作直接参照）**：商人（NPC）发任务，目标跨局累计（"累计击杀""带出指定物资"），交付给奖励并解锁后续任务链与商店货品。

本项目选择**塔科夫式跨局任务**为主形态——它与已有的撤离结算（`CrossPlayLink.OnBattleExtracted`）、解锁链（`UnlockSystem`）、仓库（`InventorySystem.Warehouse`）天然衔接。

### 1.1 设计共识（业界成熟实践）

1. **任务定义是纯数据**：策划填表，代码零改动加任务。
2. **任务进度由领域事件驱动**：任务系统订阅击杀/拾取/撤离等已有事件，而不是让各系统反过来调任务接口（单向依赖，避免交叉引用地狱）。
3. **状态机极小化**：只有 4 态，不搞子状态嵌套。
4. **条件/动作与对话系统共用一套表达式词汇表**（见对话系统提案 §4.3）。

---

## 2. 目标

### 2.1 本期目标（MVP）

1. 基地 NPC 通过对话接取/交付任务（依赖对话系统 MVP）。
2. 任务目标类型 4 种：`kill`（累计击杀指定敌人）、`collect`（仓库持有指定物品数）、`extract`（成功撤离指定关卡）、`flag`（对话/剧情标志位）。
3. 任务奖励：金币/经验/物品，走现有 `CurrencySystem`/`PlayerProfileSystem`/`Warehouse`。
4. 任务链：前置任务完成后解锁后续任务。
5. 任务日志 UI：列表 + 详情（目标进度 x/y）+ 基地 HUD 追踪提示。

### 2.2 本期不做

- 限时任务、日常/周常刷新。
- 分支任务（完成方式二选一）。
- 任务失败态（搜打撤里"没完成"就是没完成，不引入 Failed）。
- 战斗内 HUD 追踪（先在基地看日志；战斗内追踪待战斗 HUD 信息密度评估后再加）。

---

## 3. 总体架构

```
TbQuest / TbQuestObjective（Luban 表）
        │
        ▼
QuestConfigMgr ──► QuestSystem（纯 C# 单例，跨场景常驻）
                       │ 订阅：IEnemyEvent / IItemEvent / IPortalEvent / IDialogueEvent(flag)
                       │ 状态机 + 进度计数 + 奖励发放
                       ▼
        IQuestEvent（接取/进度/可交付/完成）
                       ▼
        QuestLogUI（基地日志）/ 对话条件（quest:1001:done）
                       ▲
        QuestSaveData（变动即存，跨局累计的根基）
```

**为什么 QuestSystem 是纯 C# 单例而非场景挂载**：任务进度跨场景累计（基地接 → 战斗做 → 撤离回基地交），挂 BattleRoot 会在切场景时销毁重建、丢订阅。它与 `UnlockSystem`/`CurrencySystem` 同级（`Shared/` 或 `Narrative/` 下的静态类），订阅的事件源（GameEvent 全局事件总线）本身跨场景存活。

---

## 4. 配置表设计

### 4.1 `TbQuest`（任务头）

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | int | 任务 ID |
| `name` | string | 显示名（英文） |
| `desc` | string | 描述 |
| `giverNpc` | int | 发布 NPC（对话系统 NPC ID，0=任务板） |
| `prereq` | string | 接取条件表达式（`quest:1001:done&level:>=2`），空=无 |
| `rewards` | string | `gold:200\|exp:50\|item:3001:2` |
| `nextQuest` | int | 链式后继（完成即解锁其接取条件），0=无 |

### 4.2 `TbQuestObjective`（目标）

| 列 | 类型 | 说明 |
|----|------|------|
| `id` | int | 目标 ID |
| `questId` | int | 所属任务 |
| `type` | string | `kill` / `collect` / `extract` / `flag` |
| `targetId` | int/string | 敌人 configId / 物品 itemId / 关卡 id / flag 键 |
| `count` | int | 需求数（extract 类恒为 1） |
| `desc` | string | 目标一行描述（"Eliminate 10 Raiders in Level 101"） |

### 4.3 状态机

```
Inactive ──accept(满足 prereq)──► Active ──全部目标达成──► ReadyToTurnIn ──交付──► Completed
```

- `collect` 目标在每次 `OnWarehouseChanged` 时重算（持有数，非累计拾取——塔科夫式"带回来才算"）。
- `kill`/`extract` 目标跨局累计，计数存存档。
- `ReadyToTurnIn` 是一个显式状态而非"Active 且进度满"，原因：交付动作要触发奖励与后续对话分支，状态边界清晰才好挂日志与引导。

### 4.4 事件到目标的映射

| 目标类型 | 订阅的现有事件 | 备注 |
|---------|---------------|------|
| `kill` | `IEnemyEvent.OnEnemyDied(int enemyId)` | ⚠️ 现有事件只给实例 ID 不给 configId，需要给事件加 `configId` 参数或击杀时查表——列入工作量 |
| `collect` | `IItemEvent.OnWarehouseChanged()` | 重算持有数 |
| `extract` | `CrossPlayLink.OnBattleExtracted`（现有钩子，含 levelId） | 与奖励发放同一时机 |
| `flag` | 对话系统的 flag 写入点 | `DialogueFlagSystem.Set` 内发通知 |

---

## 5. 运行时与存档

### 5.1 `QuestSystem` API

```csharp
bool CanAccept(int questId);          // prereq 求值
bool Accept(int questId);             // Inactive→Active
QuestState GetState(int questId);     // 四态
int GetProgress(int objectiveId);     // 当前计数
bool TryTurnIn(int questId);          // ReadyToTurnIn→Completed + 发奖 + Flush
List<int> GetActiveQuests();
```

事件 `IQuestEvent`：`OnQuestAccepted(int questId)` / `OnObjectiveProgress(int objectiveId, int cur, int need)` / `OnQuestReadyToTurnIn(int questId)` / `OnQuestCompleted(int questId)`。

### 5.2 存档

```csharp
[Serializable]
public class QuestSaveData
{
    public bool initialized;
    public List<QuestEntry> quests = new List<QuestEntry>();
}

[Serializable]
public class QuestEntry
{
    public int questId;
    public string state;                    // 字符串存枚举，与 KeyBindingEntry 同惯例
    public List<int> objectiveProgress;     // 与表内目标顺序对齐
}
```

`SaveData` 加 `quest` 段（与对话系统的 `dialogue` 段同一版本迁移，1→2）。

### 5.3 奖励发放

复用现成窄口：`CurrencySystem.Add` / `PlayerProfileSystem.AddExp` / `Warehouse` 入栈。任务系统不碰钱和背包的内部结构。

---

## 6. UI 设计

### 6.1 `QuestLogUI`（基地）

- 打开时机：基地按 J 键 + 任务板/NPC 对话内"View Jobs"选项。
- 左列表右详情：列表分组（Active / Ready / Done），详情显示目标 x/y 进度条与奖励预览。
- TEngine UIWindow + prefab（`Assets/AssetRaw/UI/QuestLogUI/`），光标按 CursorManager 配对规则，关窗 ESC 链路注册进 `InputSystem.HandleEscapeInput` 同类位置（经营场景走 `SimulationInputSystem`）。

### 6.2 交付动线（塔科夫式）

回基地 → 找发布 NPC → 对话开场白条件分支（`quest:1001:ready` → "完成了？")→ 选项 "Turn in" → 动作 `quest:turnin:1001` → 奖励到账 → 后继任务对话解锁。整条动线**不需要任务系统写一句 UI 代码**——这是对话/任务分层带来的。

---

## 7. 分期与工作量估算

| 阶段 | 内容 | 预估 |
|------|------|------|
| P0 | `IEnemyEvent.OnEnemyDied` 补 `configId` 参数（连带改 BattleSystem 发事件处与现有订阅者） | 0.5 天 |
| P1 | 两张表 + `QuestConfigMgr` + `QuestSystem` 状态机与计数 + 存档段 | 1 天 |
| P2 | 四类目标的事件订阅与进度推进 + 奖励发放 | 0.5 天 |
| P3 | `QuestLogUI` prefab + 列表/详情/进度显示 | 0.5 天 |
| P4 | 示例任务链（1001 击杀 101 敌人 → 1002 带出 3 个木料 → 1003 撤离 102）+ 基地 NPC 对话接线 | 0.5 天 |

前置依赖：对话系统 P0~P2（P4 接线需要对话动作 `quest:accept/turnin`）。

---

## 8. 风险与对策

- **跨场景订阅泄漏**：QuestSystem 常驻而事件源按场景生灭——订阅统一走全局 `GameEvent`（不走系统实例字段），场景销毁不影响总线；QuestSystem 自身无 MonoBehaviour 生命周期问题。
- **事件参数变更波及面**（OnEnemyDied 加 configId）：现有订阅者只有 BattleSystem 内部与音效/反馈，逐个改签名即可，编译器会揪出全部遗漏。
- **目标计数与表变更不一致**：存档按 `objectiveId` 存进度而非下标（示例用 List 按下标只是 MVP 简化，若策划改表频繁，P1 直接改 `List<ObjectiveProgress{id, count}`，成本几乎相同，建议直接做）。
- **与解锁系统职责重叠**：`UnlockSystem` 管"内容是否可用"（关卡/武器），任务系统管"目标与奖励"；任务完成可以作为解锁条件（prereq 表达式 `quest:1001:done` 已覆盖），不反过来让 UnlockSystem 感知任务。
