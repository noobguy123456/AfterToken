# 生产系统

## 职责

管理生产队列、制造配方与产出结算。

## 规划中的内容

| 类/文件 | 说明 |
|---|---|
| `ProductionSystem` | 生产管理入口 |

## 设计要点

- 生产配方使用 Luban `TbProduction`。
- 产出完成后更新 `InventorySystem` 并触发事件。
