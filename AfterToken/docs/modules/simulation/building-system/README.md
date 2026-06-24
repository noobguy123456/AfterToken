# 建筑系统

## 职责

管理模拟经营中的建筑建造、升级、拆除与功能解锁。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `BuildingSystem` | 建筑管理入口 |
| `BuildingEntity` | 建筑表现实体 |

## 设计要点

- 建筑配置使用 Luban `TbBuilding`。
- 建造/升级消耗货币与材料，完成后触发 `ISimulationEvent.OnBuildingCompleted`。
