# 06 配置系统

## 当前状态：硬编码配置

Luban 当前仅预留运行时库（`Assets/GameScripts/HotFix/GameProto/LubanLib/`），没有真实的配置表生成流程。

当前配置以硬编码 C# 类实现：

| 配置 | 路径 | 说明 |
|------|------|------|
| `LevelConfig` / `LevelConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 关卡数据，临时实现 |
| `WeaponConfig` / `WeaponConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 武器数据，临时实现 |
| `Enums` | `Assets/GameScripts/HotFix/GameLogic/Config/Enums.cs` | 武器/弹道/开火模式枚举 |

## 配置访问示例

```csharp
public class LevelConfigMgr
{
    public static LevelConfigMgr Instance => _instance ??= new LevelConfigMgr();

    private LevelConfigMgr()
    {
        _configs[1] = new LevelConfig
        {
            id = 1,
            displayName = "Training Ground",
            sceneName = "BattleScene",
            description = "Basic training level.",
            // ...
        };
    }

    public LevelConfig Get(int id) { ... }
    public IEnumerable<LevelConfig> GetAll() => _configs.Values.OrderBy(c => c.id);
}
```

武器配置同理，所有 UI 展示文本已统一为英文。

## Luban 规划

`docs/项目架构方案.md` 中规划的 Luban 表：

- `TbPlayer`
- `TbWeapon`
- `TbBullet`
- `TbEnemy`
- `TbWave`
- `TbLevel`
- `TbDrop`
- `TbBuff`
- `TbPlayerAttr`
- `TbItem`
- `TbBuilding`
- `TbProduction`
- `TbCrop`
- `TbOrder`
- `TbPlayerAnimation`

接入后计划将 `LevelConfigMgr`、`WeaponConfigMgr` 等替换为：

```csharp
ConfigSystem.Instance.Tables.TbXXX
```

## 开发建议

1. 当前新增配置字段时，直接修改 `LevelConfig` / `WeaponConfig` 类及对应 `Mgr` 的构造函数。
2. 所有面向 UI 的文本字段使用英文，避免 TMP 中文字体兼容性问题。
3. 配置类放在 `GameProto` 或 `GameLogic.Config` 中，便于后续迁移到 Luban。
