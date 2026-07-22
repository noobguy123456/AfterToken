# 飞行物系统

## 职责

管理子弹、火箭等飞行物的生成、飞行、命中检测与回收。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `ProjectileSystem` | `Assets/GameScripts/HotFix/GameLogic/System/ProjectileSystem.cs` | 飞行物逻辑更新 |
| `ProjectileEntity` | `Assets/GameScripts/HotFix/GameLogic/Entity/Projectile/ProjectileEntity.cs` | 飞行物表现实体 |
| `IProjectileEvent` | `Assets/GameScripts/HotFix/GameLogic/IEvent/IProjectileEvent.cs` | 飞行物事件接口 |
| `ProjectileVisualSystem`（提案） | 待实现 | 逻辑与视觉分离后的视觉层 |

## 设计要点

- 所有飞行物由 `ProjectileSystem` 统一 `Update`，避免每个子弹一个 MonoBehaviour `Update`。
- 命中后触发 `IProjectileEvent.OnProjectileHit`，交由 `BattleSystem` 计算伤害。
- 使用对象池回收飞行物实例。
- 爆炸范围伤害使用 `Physics2D.OverlapCircle` + 静态 `Collider2D[]` 缓冲，避免每次爆炸分配结果数组。

## 架构演进（弹幕扩展）

当前实现中，每发子弹都对应一个 `GameObject` + `SpriteRenderer`，在 M1 阶段足够直观。后续若引入密集弹幕，该方案会在 Draw Call、Transform 更新、对象池生命周期上遇到瓶颈。

已提出演进方案：**逻辑子弹与视觉表现分离**。

- **逻辑层**（`ProjectileSystem`）：只维护 `ProjectileData`，负责飞行、碰撞、命中、生命周期。
- **视觉层**（`ProjectileVisualSystem`）：根据逻辑数据批量渲染：
  - 普通小弹丸使用 **Particle System** 批量渲染。
  - 超大规模固定轨迹弹幕可升级为 **GPU Instancing / Compute Shader**。
  - 关键子弹（火箭、榴弹、追踪弹）保留 **实体 GameObject + `ProjectileEntity`**。

详细方案见：`docs/Proposal/combat/bullet-logic-visual-separation.md`
