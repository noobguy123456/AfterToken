# Player Profile System

## 职责

负责玩家全局档案与成长：
- 玩家等级与经验
- 经验值曲线
- 解锁内容记录（关卡、武器、功能）
- 与解锁系统联动

## 核心类与文件

| 类/文件 | 说明 |
|---|---|
| `PlayerProfileSystem` | 玩家档案总入口 |
| `IPlayerProfileEvent` | 玩家档案事件接口（待定义） |
| `UnlockSystem` | 解锁判定（依赖档案数据） |

## 对外接口

```csharp
public static class PlayerProfileSystem
{
    public static int Level { get; }
    public static long Exp { get; }
    public static void AddExp(int amount);
    public static bool IsUnlocked(string contentId);
    public static void Unlock(string contentId);
}
```

## 依赖关系

- 依赖：`SaveSystem`（持久化）
- 被依赖：UnlockSystem、RewardSystem、CrossPlayLink

## 设计要点

- 经验值升级时触发 `IPlayerProfileEvent.OnLevelUp`。
- 解锁状态集中管理，支持配置表驱动解锁条件。

---

> 状态：待实现。详细进度见 [progress.md](./progress.md)。
