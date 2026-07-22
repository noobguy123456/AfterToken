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
| `Configs/GameConfig/Datas/player.xlsx` | 玩家属性表（含动画名映射） |
| `Configs/GameConfig/Datas/enemy.xlsx` | 敌人属性表 |
| `Configs/GameConfig/Datas/inventory.xlsx` | 背包容量表 |
| `Configs/GameConfig/Datas/portal.xlsx` | 传送门表 |
| `Configs/GameConfig/Datas/buff.xlsx` | Buff 表 |
| `Configs/GameConfig/Datas/battle.xlsx` | 战斗相关表（多 Sheet：Wave / Drop） |
| `Configs/GameConfig/Datas/camera.xlsx` | 相机配置表 |
| `Configs/GameConfig/Datas/ballistic.xlsx` | 弹道全局配置表 |
| `Configs/GameConfig/Datas/ui_config.xlsx` | UI 全局配置表 |
| `Configs/GameConfig/Datas/pickup.xlsx` | 拾取物配置表 |

> `@` 前为 Sheet 名，后为文件名。例如 `Enemy@battle.xlsx` 表示读取 `battle.xlsx` 的 `Enemy` 页签。

### 生成脚本

```
Configs/GameConfig/gen_code_bin_to_project.bat
```

> 项目已移除历史遗留的临时 Python 生成脚本，统一使用 Luban 官方批处理脚本。

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
- `cfg_tbportal.json`
- `cfg_tbinventoryconfig.json`
- `cfg_tbcamera.json`
- `cfg_tbballistic.json`
- `cfg_tbuiconfig.json`
- `cfg_tbpickup.json`

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
| `TbPlayer` | `Datas/player.xlsx` | 已接入，含动画名映射 |
| `TbEnemy` | `Datas/enemy.xlsx` | 已接入，含 `pathRefreshInterval`（敌人追击路径刷新间隔） |
| `TbWave` | `Datas/battle.xlsx@Wave` | 已接入 |
| `TbDrop` | `Datas/battle.xlsx@Drop` | 已接入 |
| `TbBuff` | `Datas/buff.xlsx` | 已接入 |
| `TbPortal` | `Datas/portal.xlsx` | 已接入 |
| `TbInventoryConfig` | `Datas/inventory.xlsx` | 已接入 |
| `TbCamera` | `Datas/camera.xlsx` | 已接入 |
| `TbBallistic` | `Datas/ballistic.xlsx` | 已接入 |
| `TbUiConfig` | `Datas/ui_config.xlsx` | 已接入 |
| `TbPickup` | `Datas/pickup.xlsx` | 已接入 |

## 待接入配置表

以下模块此前存在硬编码数据或 TODO，已在本轮统一接入配置表：

| 配置 | 源文件 | 原硬编码位置 | 说明 |
|------|--------|--------------|------|
| `TbPlayer` 动画名扩展 | `Datas/player.xlsx` | `Entity/Player/PlayerEntity.cs` | 玩家状态名到动画 Clip 名的映射（`Idle→Player_Idle` 等） |
| `TbCamera` | `Datas/camera.xlsx` | `CameraSystem.cs` | 平滑时间、默认 FOV、FOV 平滑时间、震动衰减、狙击镜参数 |
| `TbBallistic` | `Datas/ballistic.xlsx` | `BallisticSystem.cs` | Tracer 半径/宽度/尾迹/颜色、默认命中层级、最大同时存在数 |
| `TbUiConfig` | `Datas/ui_config.xlsx` | `DamageNumberUI.cs` / `HitFeedbackUI.cs` / `LoadingUI.cs` | 飘字/命中标记/受击指示器颜色、字体大小、Loading 文本格式 |
| `TbPickup` | `Datas/pickup.xlsx` | `Entity/Pickup/PickupEntity.cs` | 拾取物碰撞体半径、视觉缩放、渲染排序 |

当前暂无明确剩余硬编码配置项。

## 配置变更标准流程

无论是新增配置表、新增字段，还是修改现有字段，都遵循以下流程：

1. **在 Excel 中修改定义**
   - 结构定义：`Configs/GameConfig/Datas/__beans__.xlsx`
   - 表注册：`Configs/GameConfig/Datas/__tables__.xlsx`
   - 枚举定义：`Configs/GameConfig/Datas/__enums__.xlsx`
   - 数据：`Configs/GameConfig/Datas/<表名>.xlsx`

2. **运行生成脚本**

   ```bash
   D:\U3D_project\AfterToken\AfterToken\Configs\GameConfig\gen_code_bin_to_project.bat
   ```

   生成结果：
   - C# 代码：`Assets/GameScripts/HotFix/GameProto/GameConfig/`
   - JSON 数据：`Assets/AssetRaw/Configs/json/`
   - 桥接文件：`ConfigSystem.cs` / `ExternalTypeUtil.cs`

3. **验证**
   - `dotnet build GameProto.csproj --no-dependencies`
   - `dotnet build GameLogic.csproj --no-dependencies`
   - 进 Play Mode 检查配置加载与业务逻辑。

4. **同步文档**
   - 更新 `docs/framework/06-ConfigSystem.md`。
   - 更新 `docs/modules/pipeline/luban-config-system/README.md` 与 `ADDING-NEW-CONFIG.md`。
   - 更新相关模块的 `README.md` / `progress.md`。

> 注意：历史遗留的临时 Python 脚本（`generate_cs.py`、`update_config.py`、`update_item_text.py`）已清理，统一使用 `gen_code_bin_to_project.bat`。

---

## 开发建议

1. 新增配置字段时，优先修改 Excel 并重新生成代码，不要再改硬编码 `Mgr`。
2. 所有面向 UI 的文本字段使用英文，避免 TMP 中文字体兼容性问题。
3. 数据行 A 列必须留空，字段从 B 列开始。
4. Bean/Color 等复杂类型如需用逗号分隔单单元格填写，类型应写为 `cfg.Color#sep=,`。
5. 列表 + Bean 组合类型应写为 `(list#sep=;),cfg.ItemExchange#sep=,`。
6. 相关小表可合并到一个 Excel 的多个 Sheet 中，减少文件数量。
