# 关卡系统

## 职责

管理关卡加载、波次生成、胜负判定以及关卡配置。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `LevelConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | Luban `TbLevel` 查询包装 |
| `ILevelEvent` | 规划中 | 关卡事件接口 |

## 待完成

- 波次生成逻辑（接入 Luban `TbWave`）
- 胜负判定

## 设计要点

- 关卡配置已由 Luban `TbLevel` 驱动（`LevelConfigMgr` 包装）。
- `ProcedureBattle` 按 `TbLevel` 配置（默认武器、玩家血量、敌人数量/半径/配置 ID）初始化战斗并驱动 `EnemySpawnSystem` 生成敌人；波次生成（`TbWave`）尚未接入。
- 选关流程：大厅（ProcedureLobby）已废弃，现为基地 SELECT_LEVEL 传送门 / Deploy 按钮（`PortalSystem`）选关。
