# 经营 UI

## 职责

模拟经营玩法所需的所有 UI 窗口与控件，包括经营主界面、建筑、资源、订单面板。

## 规划中的 UI

| UI | 说明 |
|---|---|
| `SimulationMainUI` | 经营主界面 |
| `BuildingWidget` | 建筑信息/操作控件 |
| `ResourceWidget` | 资源显示控件 |
| `OrderWidget` | 订单列表/交付控件 |

## 设计要点

- 沿用 TEngine `UIWindow` + Prefab 工作流。
- 数据变更通过 `ISimulationEvent` / `ICurrencyEvent` / `IInventoryEvent` 刷新 UI。
