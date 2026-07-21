# Currency System

## 职责

负责玩家持有的各类货币管理：
- 金币（Gold）
- 钻石（Diamond）
- 能量/体力（Energy）
- 其他代币（如活动币、竞技币等）

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `CurrencySystem` | 货币总入口，提供 Add/Consume/Query 接口 |
| `CurrencyChangedEvent` | 货币变化事件（待定义） |
| `TbCurrency` / `TbCurrencyExchange` | 可选：货币配置表 |

## 对外接口

```csharp
public static class CurrencySystem
{
    public static int Get(CurrencyType type);
    public static bool Add(CurrencyType type, int amount, string reason);
    public static bool Consume(CurrencyType type, int amount, string reason);
}
```

## 依赖关系

- 依赖：`SaveSystem`（持久化）、`IPlayerProfileEvent`（等级/解锁联动）
- 被依赖：RewardSystem、OrderSystem、BuildingSystem、ShopSystem

## 设计要点

- 所有货币变化走统一接口，便于审计和防负数。
- 变化时派发 `ICurrencyEvent`。
- 与 SaveSystem 绑定，避免内存数据丢失。

---

> 状态：待实现。详细进度见 [progress.md](./progress.md)。
