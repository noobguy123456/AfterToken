# NPC System（NPC 系统）

## 概述

模拟经营场景（基地）的 NPC 底座：场景摆放 `NpcEntity`，玩家靠近出现 "Press E to Talk" 提示，按 E 触发交谈事件。当前交谈为占位实现（Log + 事件），为后续对话系统（`docs/Proposal/narrative/dialogue-system.md`，pending）预留接线点。

## 组成

| 模块 | 文件 | 职责 |
|------|------|------|
| 配置表 | `Configs/GameConfig/Datas/npc.xlsx` → `cfg_tbnpc.json` | `TbNpc`（map，按 id 索引）：`name` / `role` / `dialogueId`（预留，0=无对话） |
| 配置管理 | `GameLogic/Config/NpcConfigMgr.cs` | `TbNpc` 包装，`Get(npcId)` 不存在返回 null |
| 实体 | `GameLogic/Entity/Npc/NpcEntity.cs` | SphereCollider trigger（默认放宽 1.5m）+ 占位视觉（0.6x0.9m 钢蓝色胶囊）+ 头顶名字牌（TMP + `BillboardFaceCamera`，name + 灰色 role 两行） |
| 系统 | `GameLogic/System/NpcSystem.cs` | 单例（`ProcedureSimulation` 挂载），交互提示、E 键交谈（占位），走开出触发区收起提示 |
| 事件 | `GameLogic/IEvent/INpcEvent.cs` | `OnNpcTalked(int npcId)`，GroupLogic，对话系统落地后由它接管 |

## 如何新增一个 NPC

1. `npc.xlsx` 加一行：`id` / `name` / `role` / `dialogueId`（英文），跑 `Configs/GameConfig/gen_code_bin_to_project.bat`（在项目根执行）
2. 经营场景新建空 GameObject 挂 `NpcEntity`，Inspector 设 `Npc Id` 为该 id（私有字段 `_npcId`，Inspector 可见）
3. 摆放位置不要与 Portal / 建筑触发区重叠

## 交互规则

- 玩家进入触发区：显示 "Press E to Talk"（复用 `InteractionPromptUI`）
- E：触发交谈（当前占位：Log + 发 `INpcEvent.OnNpcTalked`）
- 走出触发区：提示自动收起

## 注意

- 与 PortalSystem 等共用 `OnInteractPressed`，触发区不要重叠（统一 IInteractable 仲裁器待做，属对话系统 P0）
- 名字牌为 Billboard 全朝向相机，俯视相机下会随视角倾斜，属正常表现；字号/位置在 `NpcEntity.EnsureVisual` 调整
- 当前 NPC 只在经营场景（SimulationScene）摆放了两个测试实例：`NPC_Quartermaster`（id=1，(-3,0,3)）、`NPC_Doc`（id=2，(3,0,-4)）
