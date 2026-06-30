# 敌人系统

## 职责

管理敌人的生成、AI 行为、状态机、死亡与掉落。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `EnemyEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Enemy/EnemyEntity.cs` | 敌人表现实体（含头顶血条） |
| `EnemySpawnSystem` | `Assets/GameScripts/HotFix/GameLogic/System/EnemySpawnSystem.cs` | 敌人生成 |
| `IEnemyEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IEnemyEvent.cs` | 敌人事件接口 |

## 已完成

- `EnemyEntity` 血量、受伤、死亡。
- 敌人头顶血条：默认位于敌人头顶上方 `0.6` 单位处，使用 `SpriteRenderer` 实现。
  - 背景（黑色半透明）+ 填充（绿/黄/红根据血量比例变色）。
  - 支持 Prefab 预配置血条节点，也支持运行时动态创建占位血条。
- `EnemySpawnSystem` 占位生成。

## 待完成

- 敌人 FSM：Idle / Chase / Attack / Dead
- 敌人 AI 类型与配置
- 掉落物 `PickupEntity`

## 设计要点

- 敌人逻辑由系统层驱动，Entity 只负责表现与碰撞回调。
- 生成规则由 `LevelSystem` 根据波次配置触发。
- 血条跟随敌人移动，使用 World Space `SpriteRenderer`，`sortingOrder` 高于敌人本体以保证可见。
