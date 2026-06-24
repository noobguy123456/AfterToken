# 敌人系统

## 职责

管理敌人的生成、AI 行为、状态机、死亡与掉落。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `EnemyEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Enemy/EnemyEntity.cs` | 敌人表现实体 |
| `EnemySpawnSystem` | `Assets/GameScripts/HotFix/GameLogic/System/EnemySpawnSystem.cs` | 敌人生成 |
| `IEnemyEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IEnemyEvent.cs` | 敌人事件接口 |

## 待完成

- 敌人 FSM：Idle / Chase / Attack / Dead
- 敌人 AI 类型与配置
- 掉落物 `PickupEntity`

## 设计要点

- 敌人逻辑由系统层驱动，Entity 只负责表现与碰撞回调。
- 生成规则由 `LevelSystem` 根据波次配置触发。
