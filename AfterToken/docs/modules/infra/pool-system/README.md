# 对象池系统

## 职责

统一管理游戏中需要频繁创建和销毁的对象（子弹、敌人、特效、伤害数字等），避免 Instantiate/Destroy 带来的 GC 压力。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `GameObjectPool` | `Assets/GameScripts/HotFix/GameLogic/Pool/GameObjectPool.cs` | 通用对象池 |
| `PoolSystem` | `Assets/GameScripts/HotFix/GameLogic/Pool/PoolSystem.cs` | 对象池管理入口，支持 `Get` / `Recycle` / `Clear` / `ClearAll` / `Preload` |

## 已接入场景

- `ProjectileSystem`：预加载并复用子弹 GameObject。
- `EnemySpawnSystem`：按 Prefab 地址分池，预加载并复用敌人；`EnemyEntity` 死亡后回池。

## 待完成

- 按类型拆分 `ProjectilePool`、`EnemyPool`、`EffectPool`、`DamageTextPool`（当前仍使用通用 `PoolSystem`）
- 完善 `ClearAll` 接口与容量自动扩缩容策略

## 设计要点

- 所有对象统一通过 Key 分池管理。
- 场景切换时调用 `ClearAll` 释放对象池。
