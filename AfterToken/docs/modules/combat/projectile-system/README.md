# 飞行物系统

## 职责

管理子弹、火箭等飞行物的生成、飞行、命中检测与回收。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `ProjectileSystem` | `Assets/GameScripts/HotFix/GameLogic/System/ProjectileSystem.cs` | 飞行物逻辑更新 |
| `ProjectileEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Projectile/ProjectileEntity.cs` | 飞行物表现实体 |
| `IProjectileEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IProjectileEvent.cs` | 飞行物事件接口 |

## 设计要点

- 所有飞行物由 `ProjectileSystem` 统一 `Update`，避免每个子弹一个 MonoBehaviour `Update`。
- 命中后触发 `IProjectileEvent.OnProjectileHit`，交由 `BattleSystem` 计算伤害。
- 使用对象池回收飞行物实例。
