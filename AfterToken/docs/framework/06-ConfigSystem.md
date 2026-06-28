# 06 配置系统

## 当前方案

项目使用 **Luban** 作为配置表工具链，输出格式为：

- **代码目标**：`cs-newtonsoft-json`（C# 代码 + Newtonsoft.Json 属性）
- **数据目标**：`json`（JSON 文件）

选择 JSON 的原因：

1. **调试方便**：`.json` 文件可直接打开查看。
2. **版本控制友好**：Git diff 可清晰追踪配置变更。

## 配置工程

配置工程位于 `Configs/GameConfig/`。

### 配置源文件（Excel）

| 路径 | 说明 |
|------|------|
| `Configs/GameConfig/Datas/__tables__.xlsx` | 注册所有数据表 |
| `Configs/GameConfig/Datas/__beans__.xlsx` | 定义 Bean 结构 |
| `Configs/GameConfig/Datas/__enums__.xlsx` | 定义枚举 |
| `Configs/GameConfig/Datas/weapon.xlsx` | 武器表 |
| `Configs/GameConfig/Datas/level.xlsx` | 关卡表 |
| `Configs/GameConfig/Datas/item.xlsx` | 道具表 |
| `Configs/GameConfig/Datas/player.xlsx` | 玩家属性表 |
| `Configs/GameConfig/Datas/buff.xlsx` | Buff 表 |
| `Configs/GameConfig/Datas/battle.xlsx` | 战斗相关表（多 Sheet） |
| `Configs/GameConfig/Datas/battle.xlsx@Enemy` | 敌人属性表 |
| `Configs/GameConfig/Datas/battle.xlsx@Wave` | 波次表 |
| `Configs/GameConfig/Datas/battle.xlsx@Drop` | 掉落表 |

> `@` 前为 Sheet 名，后为文件名。例如 `Enemy@battle.xlsx` 表示读取 `battle.xlsx` 的 `Enemy` 页签。

### 生成脚本

```
Configs/GameConfig/gen_code_bin_to_project.bat
```

运行后会输出：

- **代码**：`Assets/GameScripts/HotFix/GameProto/GameConfig/`
- **数据**：`Assets/AssetRaw/Configs/json/`

## 生成的代码与数据

### 代码位置

```
Assets/GameScripts/HotFix/GameProto/GameConfig/
```

- `Tables.cs` —— 总入口
- `cfg/` 目录 —— 每张表和每个 Bean/枚举的类

例如：

- `cfg/TbWeapon.cs`
- `cfg/Weapon.cs`
- `cfg/WeaponType.cs`

> 这些代码是自动生成的，**不要手动修改**，改 Excel 后重新跑脚本即可。

### 数据位置

```
Assets/AssetRaw/Configs/json/
```

例如：

- `cfg_tbweapon.json`
- `cfg_tblevel.json`
- `cfg_tbitem.json`
- `cfg_tbplayer.json`
- `cfg_tbenemy.json`
- `cfg_tbwave.json`
- `cfg_tbdrop.json`
- `cfg_tbbuff.json`

运行时由 `ConfigSystem` 通过 YooAsset 加载这些 JSON。YooAsset 收集器 `Configs` 组已包含 `Assets/AssetRaw/Configs`，因此 `.json` 文件会被自动打包。

## 运行时访问

### 直接访问 Luban 生成的表

```csharp
var tables = ConfigSystem.Instance.Tables;

// 武器表（map）
var weaponCfg = tables.TbWeapon.Get(1001);

// 关卡表（list）
var levelList = tables.TbLevel.DataList;

// 道具表
var item = tables.TbItem.Get(10001);
foreach (var exchange in item.ExchangeList)
{
    Debug.Log($"兑换: itemId={exchange.Id}, num={exchange.Num}");
}

// 敌人 / 波次 / 掉落 / Buff
var enemy = tables.TbEnemy.Get(9001);
var wave  = tables.TbWave.DataList[0];
var drop  = tables.TbDrop.DataList[0];
var buff  = tables.TbBuff.Get(1);
var player = tables.TbPlayer.Get(1);
```

`ConfigSystem` 使用懒加载，首次访问 `Tables` 时通过 YooAsset 加载 `AssetRaw/Configs/json/` 下的 `.json` 文件，并用 Newtonsoft.Json 反序列化。

### 通过适配管理器访问（兼容旧代码）

武器和关卡仍保留原来的管理器接口，数据来自 Luban 配置表：

```csharp
var weapon = WeaponConfigMgr.Instance.Get(1001);
var level  = LevelConfigMgr.Instance.Get(1);
```

内部实现已从硬编码改为读取 `ConfigSystem.Instance.Tables`。

## 一个 Excel 多个 Sheet

Luban 支持一个 `.xlsx` 文件内包含多个 Sheet，每个 Sheet 作为一个独立表。

在 `__tables__.xlsx` 中通过 `Sheet名@文件名` 注册：

| full_name | value_type | read_schema_from_file | input | index | mode | group |
|-----------|------------|----------------------|-------|-------|------|-------|
| `cfg.TbEnemy` | `Enemy` | false | `Enemy@battle.xlsx` | `id` | `map` | `c` |
| `cfg.TbWave` | `Wave` | false | `Wave@battle.xlsx` | `id` | `list` | `c` |
| `cfg.TbDrop` | `Drop` | false | `Drop@battle.xlsx` | `id` | `list` | `c` |

每个 Sheet 的表头格式与普通单文件表相同，A 列留空、字段从 B 列开始。

## 已定义的表

| 表名 | 源文件 | 状态 |
|------|--------|------|
| `TbWeapon` | `Datas/weapon.xlsx` | 已接入，5 条数据 |
| `TbLevel` | `Datas/level.xlsx` | 已接入，2 条数据 |
| `TbItem` | `Datas/item.xlsx` | 已接入，含 `EQuality`、`ItemExchange` |
| `TbPlayer` | `Datas/player.xlsx` | 已接入 |
| `TbEnemy` | `Datas/battle.xlsx@Enemy` | 已接入 |
| `TbWave` | `Datas/battle.xlsx@Wave` | 已接入 |
| `TbDrop` | `Datas/battle.xlsx@Drop` | 已接入 |
| `TbBuff` | `Datas/buff.xlsx` | 已接入 |

## 待接入配置表

以下模块仍存在硬编码数据或 TODO，建议后续接入配置表：

### P0 — 已有明确 TODO

| 配置 | 当前位置 | 说明 |
|------|----------|------|
| `TbPlayerAnimation` | `Entity/Player/PlayerEntity.cs` | 玩家状态名到动画 Clip 名的映射（`Idle→Player_Idle` 等） |

### P1 — 战斗体验核心参数

| 配置 | 当前位置 | 说明 |
|------|----------|------|
| 玩家移动/闪避参数 | `PlayerEntity.cs` | `BaseMoveSpeed`、`DodgeSpeed`、`DodgeDuration` |
| 辅助瞄准参数 | `AimAssistSystem.cs` | `_aimAssistRadius`、`_aimAssistMaxAngle`、`_lockOnRange` 等 |
| 相机参数 | `CameraSystem.cs` | `_smoothTime`、`_defaultFov`、`_fovSmoothTime`、`_shakeDamping` |
| 弹道全局参数 | `BallisticSystem.cs` | `_tracerRadius`、默认 `_hitLayers` |

### P2 — UI / 表现 / 占位资源

| 配置 | 当前位置 | 说明 |
|------|----------|------|
| 伤害数字颜色 | `DamageNumberUI.cs` | 普通/暴击颜色 |
| 受击反馈颜色 | `HitFeedbackUI.cs` | 指示器/标记颜色 |
| Loading 文本颜色 | `LoadingUI.cs` | 背景、进度条、文字颜色 |
| 占位敌人生成 | `EnemySpawnSystem.cs` | 占位 Sprite 大小、颜色、Collider 半径 |

## 开发建议

1. 新增配置字段时，优先修改 Excel 并重新生成代码，不要再改硬编码 `Mgr`。
2. 所有面向 UI 的文本字段使用英文，避免 TMP 中文字体兼容性问题。
3. 数据行 A 列必须留空，字段从 B 列开始。
4. Bean/Color 等复杂类型如需用逗号分隔单单元格填写，类型应写为 `cfg.Color#sep=,`。
5. 列表 + Bean 组合类型应写为 `(list#sep=;),cfg.ItemExchange#sep=,`。
6. 相关小表可合并到一个 Excel 的多个 Sheet 中，减少文件数量。
