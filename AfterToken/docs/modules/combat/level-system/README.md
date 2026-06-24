# 关卡系统

## 职责

管理关卡加载、波次生成、胜负判定以及关卡配置。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `LevelConfig` / `LevelConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 临时关卡配置 |
| `ILevelEvent` | 规划中 | 关卡事件接口 |

## 待完成

- 波次生成逻辑
- 胜负判定
- 接入 Luban `TbWave` / `TbLevel`

## 设计要点

- 关卡配置目前硬编码，后续接入 Luban。
- `LevelSystem` 根据波次触发 `EnemySpawnSystem` 生成敌人。
