# Save System

## 职责

负责玩家跨会话的持久化数据存储，包括：
- 玩家档案（等级、经验、解锁）
- 货币（金币、钻石、能量等）
- 仓库道具
- 设置项（音量、画质、操作、灵敏度等）
- 战斗外全局状态（如当前解锁的关卡、任务进度）

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `SaveSystem` | 存档系统总入口，提供 Save/Load/Delete 接口 |
| `PlayerProfileSystem` | 玩家档案（等级/经验/解锁），依赖 SaveSystem |
| `CurrencySystem` | 货币系统，依赖 SaveSystem |
| `SettingsSystem` | 设置持久化，依赖 SaveSystem |
| `Warehouse` | 仓库数据，持久化由 SaveSystem 接管 |

## 对外接口

```csharp
public static class SaveSystem
{
    public static void Save<T>(string key, T data);
    public static T Load<T>(string key);
    public static void Delete(string key);
}
```

## 依赖关系

- 依赖：Unity `PlayerPrefs` / JSON 文件 / 可选 AES 加密
- 被依赖：PlayerProfileSystem、CurrencySystem、SettingsSystem、Warehouse

## 设计要点

- 采用键值对存储，按模块分组（`profile_*`、`currency_*`、`inventory_*`、`settings_*`）。
- 存档版本号管理，支持版本迁移。
- 大厅流程初始化时统一加载，战斗流程只读不写。
- 敏感数据（如货币）可考虑加密或校验和。

---

> 状态：待实现。详细进度见 [progress.md](./progress.md)。
