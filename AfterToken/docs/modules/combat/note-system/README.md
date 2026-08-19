# Note System（小纸条叙事系统）

## 概述

搜打撤叙事收集品：玩家靠近场景中带 `NoteEntity` 的场景物，出现 "Press E to Read" 提示，按 E 打开约 1/3 屏（640x360）的纸条阅读面板（`NoteUI`），显示标题 + 正文。可重复阅读，不暂停游戏。

## 组成

| 模块 | 文件 | 职责 |
|------|------|------|
| 配置表 | `Configs/GameConfig/Datas/note.xlsx` → `cfg_tbnote.json` | `TbNote`（map，按 id 索引）：`title` / `content`（支持 `\n` 换行） |
| 配置管理 | `GameLogic/Config/NoteConfigMgr.cs` | `TbNote` 包装，`Get(noteId)` 不存在返回 null |
| 实体 | `GameLogic/Entity/Note/NoteEntity.cs` | BoxCollider trigger（默认放宽 2m）+ 占位视觉（0.5x0.35m 纸白色平躺 sprite，抬高 0.05m 防 z-fighting） |
| 系统 | `GameLogic/System/NoteSystem.cs` | 单例（`ProcedureBattle` 挂载），交互提示、E 键开关面板、死亡闸、走开出触发区自动关闭 |
| UI | `GameLogic/UI/NoteUI/NoteUI.cs` + `Assets/AssetRaw/UI/NoteUI/NoteUI.prefab` | 阅读面板（UILayer.Top，不暂停），Esc 关闭链已接入（`InputSystem.HandleEscapeInput`） |

## 如何新增一张纸条

1. `note.xlsx` 加一行：`id` / `title` / `content`（英文优先，换行写 `\n`），跑 `Configs/GameConfig/gen_code_bin_to_project.bat`
2. 战斗场景新建空 GameObject 挂 `NoteEntity`，Inspector 设 `Note Id` 为该 id

## 交互规则

- 玩家进入触发区：显示 "Press E to Read"（复用 `InteractionPromptUI`）
- E：打开面板；面板开着再按 E = 关闭
- Esc：走全局 Esc 关闭链（SettingsUI > BattleBagUI > LootContainerUI > NoteUI）
- 走出触发区：提示与面板都自动关闭
- 玩家死亡：交互闸关闭

## 注意

- 与 PortalSystem/LootContainerSystem 共用 `OnInteractPressed`，三者触发区不要重叠（统一 IInteractable 仲裁器待做）
- 面板开着时不显示准星、显示光标；关闭后恢复
