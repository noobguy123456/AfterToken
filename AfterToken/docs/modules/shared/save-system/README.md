# Save System

## 职责

负责玩家跨会话的持久化数据存储，包括：
- 玩家档案（等级、经验、解锁）
- 货币（金币、钻石、能量等）
- 仓库道具
- 设置项（音量、画质、操作、灵敏度等）
- 战斗外全局状态（如当前解锁的关卡、任务进度）

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `SaveSystem` | `Assets/GameScripts/HotFix/GameLogic/Shared/SaveSystem.cs` | 存档总入口（含 `SaveData` 各数据段定义） |
| `CurrencySystem` | `GameLogic/Shared/CurrencySystem.cs` | 货币，已接入持久化 |
| `PlayerProfileSystem` | `GameLogic/Shared/PlayerProfileSystem.cs` | 玩家档案，已接入持久化 |
| `Warehouse` | `GameLogic/Item/Warehouse.cs` | 仓库，已接入持久化（含 `ItemStack` 获取序号水位） |
| `SensitivitySetting` / `SniperAimModeSetting` | `GameLogic/Module/SettingModule/` | 设置项，已从 PlayerPrefs 迁移到 SaveSystem |

## 实现方案（2026-08-06 定稿）

- **后端**：单个 JSON 文件 `Application.persistentDataPath/save.json`，序列化用 `JsonUtility`（被序列化的 struct/class 必须带 `[Serializable]`，否则字段被静默跳过——`ItemStack` 踩过这个坑）。
- **写盘时机**：变动即存。模块修改数据后调用 `SaveSystem.Flush()` 立即写整份文件（文件很小，无性能问题）。
- **加载时机**：懒加载。各模块首次访问时从 `SaveSystem.Data` 自己的数据段恢复；`initialized` 标记区分"无存档"（用默认值 / 导入旧 PlayerPrefs）。
- **版本迁移**：`SaveSystem.CurrentVersion` 递增 + `Migrate()` 钩子逐级升级。
- **结构**：根对象 `SaveData` 每模块一段：`currency` / `profile` / `warehouse` / `settings`。

## 接入新模块的模式

```csharp
private static bool _loaded;

private static void EnsureLoaded()
{
    if (_loaded) return;
    _loaded = true;
    var d = SaveSystem.Data.xxx;
    if (!d.initialized) return;
    // 从 d 恢复字段
}

private static void Persist()
{
    var d = SaveSystem.Data.xxx;
    d.initialized = true;
    // 把字段写回 d
    SaveSystem.Flush();
}
```

事件调用注意加 `?.`（`GameEvent.Get<IXxxEvent>()?.On...`），否则 GM/编辑模式下无事件系统会 NRE。

## GM 调试命令

- `save` / `save path`：显示存档文件路径
- `save export`：输出存档内容到 GM 控制台
- `save clear`：删除存档并重置货币/档案/仓库为默认值

## 依赖关系

- 依赖：Unity `JsonUtility` + `Application.persistentDataPath`
- 被依赖：PlayerProfileSystem、CurrencySystem、Warehouse、SettingModule

---

> 状态：✅ 基础版已完成（四件套）。模拟经营存档、加密/校验和、云存档待后续。详细进度见 [progress.md](./progress.md)。
