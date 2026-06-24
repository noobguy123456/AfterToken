# 弹道系统

## 职责

根据武器配置分发 Raycast 或 Projectile 弹道，并处理命中判定与 Debug 可视化。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `BallisticSystem` | `Assets/GameScripts/HotFix/GameLogic/System/BallisticSystem.cs` | 弹道分发与命中判定 |
| `WeaponConfig` | `Assets/GameScripts/HotFix/GameLogic/Config/WeaponConfig.cs` | 弹道参数配置 |

## 关键配置

- `raycastRadius`：射线半径
- `hitLayers`：命中层
- `showDebugRay` / `debugRayDuration` / `debugHitColor` / `debugMissColor`：Debug 可视化

## 设计要点

- Raycast 适合即时命中（步枪、霰弹）。
- Projectile 适合可见飞行物（火箭、榴弹）。
- 命中结果统一由 `BattleSystem` 处理，避免重复调用。
