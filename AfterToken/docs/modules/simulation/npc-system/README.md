# NPC System（NPC 系统）

## 概述

模拟经营场景（基地）的 NPC 底座：场景摆放 `NpcEntity`，玩家靠近出现 "Press E to Talk" 提示，按 E 触发交谈（已接对话系统，`NpcSystem` 驱动 `DialogueSystem`，见 `docs/modules/narrative/dialogue-system/`）。支持最小档移动：路径点巡逻 + 对话时站住转身面向玩家。

## 组成

| 模块 | 文件 | 职责 |
|------|------|------|
| 配置表 | `Configs/GameConfig/Datas/npc.xlsx` → `cfg_tbnpc.json` | `TbNpc`（map，按 id 索引）：`name` / `role` / `dialogueId`（0=无对话）/ `moveSpeed`（巡逻速度，0=站桩）/ `patrolPath`（巡逻路径，格式 `x,z|x,z`，不足 2 点=站桩） |
| 配置管理 | `GameLogic/Config/NpcConfigMgr.cs` | `TbNpc` 包装，`Get(npcId)` 不存在返回 null |
| 实体 | `GameLogic/Entity/Npc/NpcEntity.cs` | SphereCollider trigger（默认放宽 1.5m）+ 占位视觉（0.6x0.9m 钢蓝色胶囊）+ 头顶名字牌（TMP + `BillboardFaceCamera`，name + 灰色 role 两行）+ 最小档移动（巡逻/对话站住转身，见下） |
| 系统 | `GameLogic/System/NpcSystem.cs` | 单例（`ProcedureSimulation` 挂载），交互提示、E 键交谈（占位），走开出触发区收起提示 |
| 事件 | `GameLogic/IEvent/INpcEvent.cs` | `OnNpcTalked(int npcId)`，GroupLogic；对话系统已接管交谈呈现（`NpcSystem` 直接驱动 `DialogueSystem`） |

## 如何新增一个 NPC

1. `npc.xlsx` 加一行：`id` / `name` / `role` / `dialogueId` / `moveSpeed` / `patrolPath`（文本英文；`moveSpeed=0` 或 `patrolPath` 不足 2 点 = 站桩；巡逻格式 `x,z|x,z`，如 `-3,3|4,3`），跑 `Configs/GameConfig/gen_code_bin_to_project.bat`（在项目根执行）
2. 经营场景新建空 GameObject 挂 `NpcEntity`，Inspector 设 `Npc Id` 为该 id（私有字段 `_npcId`，Inspector 可见）
3. 摆放位置与巡逻路径不要与 Portal / 建筑触发区重叠

## 移动规则（最小档，2026-08-27）

- 巡逻：路径点间 ping-pong 往返，速度取 `moveSpeed`，到点停留 1 秒再走向下一点；移动中平滑转身面向目标点（仅 Y 轴，`Slerp 8/s`）
- 对话时（`DialogueSystem.IsPlaying && CurrentNpcId == 本 NPC`）：立即站住，转身面向玩家；对话结束自动恢复巡逻
- 不做寻路/避障、不做 FSM、不存档记忆位置——路径要避开障碍物摆放
- 玩家引用为懒获取（玩家生成晚于场景加载，`Start` 里拿不到）

## 交互规则

- 玩家进入触发区：显示 "Press E to Talk"（复用 `InteractionPromptUI`）
- E：触发交谈（当前占位：Log + 发 `INpcEvent.OnNpcTalked`）
- 走出触发区：提示自动收起；进行中的对话被打断

## 注意

- 与 PortalSystem 等共用 `OnInteractPressed`，触发区不要重叠（统一 IInteractable 仲裁器待做，属对话系统 P0）
- 名字牌为 Billboard 全朝向相机，俯视相机下会随视角倾斜，属正常表现；字号/位置在 `NpcEntity.EnsureVisual` 调整
- 当前 NPC 只在经营场景（SimulationScene）摆放了两个测试实例：`NPC_Quartermaster`（id=1，(-3,0,3)，巡逻 `-3,3|4,3`）、`NPC_Doc`（id=2，(3,0,-4)，站桩）
