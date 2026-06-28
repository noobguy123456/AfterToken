# 06 配置系统

## 当前方案

项目使用 **Luban** 作为配置表工具链，输出格式为：

- **代码目标**：`cs-newtonsoft-json`（C# 代码 + Newtonsoft.Json 属性）
- **数据目标**：`json`（JSON 文件）

选择 JSON 的原因：

1. **调试方便**：`.json` 文件可直接打开查看。
2. **版本控制友好**：Git diff 可清晰追踪配置变更。

## 配置工程

配置工程位于 `Configs/GameConfig/`，详细梳理见：

- `docs/modules/pipeline/luban-config-system/README.md`
- `docs/modules/pipeline/luban-config-system/progress.md`

当前已定义的表：

| 表名 | 源文件 | 状态 |
|------|--------|------|
| `TbWeapon` | `Datas/weapon.xlsx` | 已定义，5 条数据 |
| `TbLevel` | `Datas/level.xlsx` | 已定义，2 条数据 |
| `TbItem` | `Datas/item.xlsx` | 示例数据，Bean/Enum 待补齐 |

计划接入的其他表：

- `TbPlayer`
- `TbBullet`
- `TbEnemy`
- `TbWave`
- `TbDrop`
- `TbBuff`
- `TbPlayerAttr`
- `TbBuilding`
- `TbProduction`
- `TbCrop`
- `TbOrder`
- `TbPlayerAnimation`

## 运行时访问

```csharp
var weaponCfg = ConfigSystem.Instance.Tables.TbWeapon.Get(1001);
var levelList = ConfigSystem.Instance.Tables.TbLevel.DataList;
```

`ConfigSystem` 使用懒加载，首次访问 `Tables` 时通过 YooAsset 加载 `AssetRaw/Configs/json/` 下的 `.json` 文件，并用 Newtonsoft.Json 反序列化。

## 当前硬编码配置（待替换）

在 Luban 生成正式跑通前，以下配置仍以硬编码 C# 类实现：

| 配置 | 路径 | 说明 |
|------|------|------|
| `LevelConfig` / `LevelConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 关卡数据，临时实现 |
| `WeaponConfig` / `WeaponConfigMgr` | `Assets/GameScripts/HotFix/GameLogic/Config/` | 武器数据，临时实现 |
| `Enums` | `Assets/GameScripts/HotFix/GameLogic/Config/Enums.cs` | 武器/弹道/开火模式枚举 |

## 开发建议

1. 当前新增配置字段时，直接修改 `LevelConfig` / `WeaponConfig` 类及对应 `Mgr` 的构造函数。
2. 所有面向 UI 的文本字段使用英文，避免 TMP 中文字体兼容性问题。
3. 修复 Luban 工具链后，优先生成 `TbWeapon` / `TbLevel` 替换硬编码配置。
4. `item.xlsx` 含复杂类型（`ItemExchange`、`EQuality`、`datetime`、`ref`、`list`），生成前需先在 `__beans__.xlsx` / `__enums__.xlsx` 中定义对应类型，或简化表结构。
