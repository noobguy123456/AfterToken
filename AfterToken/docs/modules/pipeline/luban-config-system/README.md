# Luban 配置表系统

> 项目已接入 Luban 工具链，配置工程位于 `Configs/GameConfig/`。
> 当前状态：**配置工程已搭建，数据表已定义，生成脚本可正常运行**。
> 新增/修改配置表流程见 [ADDING-NEW-CONFIG.md](./ADDING-NEW-CONFIG.md)。

---

## 一、Luban 工具链

| 文件 | 路径 | 说明 |
|------|------|------|
| `Luban.exe` | `Tools/Luban/Luban.exe` | Luban 主程序（详见 [3.1 生成路径](#31-生成路径)） |
| `README.md` | `Tools/Luban/README.md` | 安装说明 |

### 工具链状态

`gen_code_bin_to_project.bat` 可正常生成代码与 JSON 数据，前提是 `Tools/Luban/` 目录下包含完整运行依赖（`Luban.exe`、`Luban.runtimeconfig.json` 等）。

`Tools/Luban/` 指**仓库根目录**下的 `Tools/Luban/`（本机为 `D:\U3D_project\AfterToken\Tools\Luban`），与脚本所在目录 `Configs/GameConfig/` 相差三级 `..\..\..`。若在新环境遇到 `hostpolicy.dll` / `Luban.runtimeconfig.json` 缺失错误，需从 Luban 官方 Release 下载完整工具链压缩包，解压后**整个文件夹**放到仓库根目录的 `Tools/Luban/` 下。

> 历史遗留的 `generate_cs.py`、`update_config.py`、`update_item_text.py` 等临时 Python 脚本已清理，统一使用 `gen_code_bin_to_project.bat` 作为标准生成流程。

官方地址：
- GitHub：https://github.com/focus-creative-games/luban/releases
- Gitee 镜像：https://gitee.com/focus-creative-games/luban/releases

验证安装：

```bash
dotnet Tools/Luban/Luban.exe --help
```

---

## 二、配置工程结构

```
Configs/GameConfig/
├── luban.conf                              # Luban 主配置
├── gen_code_bin_to_project.bat             # 客户端代码生成脚本（标准模板）
├── gen_code_bin_to_project_nobom.bat       # 客户端代码生成脚本（无 BOM 中文提示）
├── Defines/
│   └── builtin.xml                         # Unity 类型映射（vector2/3/4/2int/3int）
├── Datas/
│   ├── __tables__.xlsx                     # 表注册索引
│   ├── __beans__.xlsx                      # 数据结构（bean）定义
│   ├── __enums__.xlsx                      # 枚举定义
│   ├── player.xlsx                         # 玩家属性表
│   ├── weapon.xlsx                         # 武器表
│   ├── level.xlsx                          # 关卡表
│   ├── enemy.xlsx                          # 敌人属性表
│   ├── item.xlsx                           # 物品表
│   ├── inventory.xlsx                      # 背包容量表
│   ├── battle.xlsx                         # 战斗表（含 Wave/Drop 多个 sheet）
│   ├── buff.xlsx                           # Buff 表
│   ├── portal.xlsx                         # 传送门表
│   ├── camera.xlsx                         # 相机配置表
│   ├── ballistic.xlsx                      # 弹道全局配置表
│   ├── ui_config.xlsx                     # UI 全局配置表
│   └── pickup.xlsx                         # 拾取物配置表
└── CustomTemplate/
    ├── ConfigSystem.cs                     # 配置加载器模板
    ├── ExternalTypeUtil.cs                 # Unity 类型转换工具
    └── CustomTemplate_Client_LazyLoad/     # 懒加载二进制模板目录（当前 JSON 模式下未使用）
        └── cs-bin/
            └── tables.sbn                  # 原 cs-bin 懒加载代码模板
```

### 2.1 `luban.conf`

- `topModule`：`GameConfig`
- 分组：`c`（客户端）、`s`（服务端）、`e`（编辑器）
- 目标：`server` / `client` / `all`
- 数据目录：`Datas`
- Schema 来源：`Defines/`、`Datas/__tables__.xlsx`、`Datas/__beans__.xlsx`、`Datas/__enums__.xlsx`

### 2.2 `Defines/builtin.xml`

已定义以下 Unity 类型映射：

| Luban 类型 | Unity 类型 | 构造方法 |
|-----------|-----------|---------|
| `vector2` | `UnityEngine.Vector2` | `ExternalTypeUtil.NewVector2` |
| `vector3` | `UnityEngine.Vector3` | `ExternalTypeUtil.NewVector3` |
| `vector4` | `UnityEngine.Vector4` | `ExternalTypeUtil.NewVector4` |
| `vector2int` | `UnityEngine.Vector2Int` | `ExternalTypeUtil.NewVector2Int` |
| `vector3int` | `UnityEngine.Vector3Int` | `ExternalTypeUtil.NewVector3Int` |

### 2.3 已注册的数据表

`Datas/__tables__.xlsx`：

| full_name | value_type | read_mode | comment |
|-----------|-----------|-----------|---------|
| `cfg.TbWeapon` | `Weapon` | `map` | 武器表 |
| `cfg.TbLevel` | `Level` | `list` | 关卡表 |
| `cfg.TbItem` | `Item` | `map` | 物品表（含 `EQuality` / `ItemExchange` / `EItemType`） |
| `cfg.TbPlayer` | `Player` | `map` | 玩家属性表 |
| `cfg.TbEnemy` | `Enemy` | `map` | 敌人属性表（数据源：`enemy.xlsx`） |
| `cfg.TbWave` | `Wave` | `list` | 波次表（数据源：`battle.xlsx#Wave`） |
| `cfg.TbDrop` | `Drop` | `list` | 掉落表（数据源：`battle.xlsx#Drop`） |
| `cfg.TbBuff` | `Buff` | `map` | Buff 表 |
| `cfg.TbPortal` | `Portal` | `map` | 传送门表 |
| `cfg.TbInventoryConfig` | `InventoryConfig` | `map` | 背包容量表 |
| `cfg.TbCamera` | `Camera` | `map` | 相机配置表 |
| `cfg.TbBallistic` | `Ballistic` | `map` | 弹道全局配置表 |
| `cfg.TbUiConfig` | `UiConfig` | `map` | UI 全局配置表 |
| `cfg.TbPickup` | `Pickup` | `map` | 拾取物配置表 |

> 注意：`item.xlsx` 已注册，`item.ItemExchange`、`item.EQuality` 与 `item.EItemType` 枚举/Bean 已定义。

### 2.4 已定义的枚举

`Datas/__enums__.xlsx`：

| 枚举名 | 项 |
|--------|-----|
| `WeaponType` | Pistol, SMG, Rifle, Sniper, Rocket |
| `BallisticType` | Raycast, Projectile |
| `FireMode` | Single, Auto |
| `EQuality` | Blue, Purple, Yellow, Red |
| `EItemType` | Material, Consumable, Equipment, Quest, Other |

### 2.5 数据表内容

#### `weapon.xlsx`

- 5 把武器：1001 Pistol、1002 SMG、1003 Rifle、1004 Sniper、1005 Rocket Launcher
- 字段已覆盖：基础属性、扩散、移动系数、瞄准、弹道、辅助瞄准、火箭锁定、后坐力/抖动、资源引用

#### `level.xlsx`

- 2 个关卡：1 Training Ground L1、2 Abandoned Factory L2
- 字段：场景名、默认武器、玩家血量、敌人数/配置/半径等

#### `enemy.xlsx`

- 独立敌人属性表，已从 `battle.xlsx#Enemy` 拆分出来，方便单独调整不同敌人类型。
- 当前数据：
  - `9001` Training Dummy：`prefab = Enemy`，`maxHp = 50`，`moveSpeed = 2`，`attackDamage = 5`，`attackRange = 1.5`，`attackInterval = 1`，`pathRefreshInterval = 0.3`
  - `9002` Assault Bot：`prefab = Enemy_Assault`，`maxHp = 80`，`moveSpeed = 3`，`attackDamage = 15`，`attackRange = 2`，`attackInterval = 1.2`，`pathRefreshInterval = 0.3`

#### `item.xlsx`

- 10 条示例物品数据
- 包含复杂类型：`EQuality`（枚举）、`ItemExchange`（Bean）、`EItemType`（枚举）、列表等

#### `inventory.xlsx`

- 1 条配置：`tempBagCapacity = 12`，`warehouseCapacity = 200`

#### `portal.xlsx`

- 传送门配置，含 `portalType`、`targetLevelId`、`targetSceneName`、`keepPlayerState`、`spawnCondition` 等

#### `camera.xlsx`

- 单条全局配置：`smoothTime`、`defaultFov`、`fovSmoothTime`、`shakeDamping`、`scopeRenderSize`、`scopeCameraFov`、`defaultOrthographicSize`

#### `ballistic.xlsx`

- 单条全局配置：`tracerRadius`、`tracerStartWidth`、`tracerEndWidth`、`tracerTailLength`、`maxActiveTracers`、`hitLayers`、`tracerStartColor`、`tracerEndColor`

#### `ui_config.xlsx`

- 单条全局配置：伤害数字参数（池大小、渐隐时间、偏移、颜色、字体大小）、命中标记参数（池大小、持续时间、大小、颜色）、受击指示器参数（淡出速度、颜色）、Loading 文本格式

#### `pickup.xlsx`

- 单条全局配置：`colliderRadius`、`visualScale`、`sortingOrder`

---

## 三、生成脚本

`gen_code_bin_to_project.bat` 流程：

1. 检查 `Tools/Luban/Luban.exe` 是否存在。
2. 复制 `CustomTemplate/ConfigSystem.cs` → `Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs`
3. 复制 `CustomTemplate/ExternalTypeUtil.cs` → `Assets/GameScripts/HotFix/GameProto/ExternalTypeUtil.cs`
4. 调用 Luban 生成：
   - 目标：`client`
   - 代码格式：`cs-newtonsoft-json`
   - 数据格式：`json`
   - 代码输出：`Assets/GameScripts/HotFix/GameProto/GameConfig/`
   - 数据输出：`Assets/AssetRaw/Configs/json/`

### 3.1 生成路径

`gen_code_bin_to_project.bat` 以自身所在目录 `Configs/GameConfig/` 为基准，涉及以下路径：

| 变量 | 相对路径（基于 `Configs/GameConfig/`） | 本机绝对路径 |
|---|---|---|
| `LUBAN_EXE` | `..\..\..\Tools\Luban\Luban.exe` | `D:\U3D_project\AfterToken\Tools\Luban\Luban.exe` |
| `CODE_OUT` | `..\..\Assets\GameScripts\HotFix\GameProto` | `D:\U3D_project\AfterToken\AfterToken\Assets\GameScripts\HotFix\GameProto` |
| `DATA_OUT` | `..\..\Assets\AssetRaw\Configs\json` | `D:\U3D_project\AfterToken\AfterToken\Assets\AssetRaw\Configs\json` |

> 注意：`Luban.exe` 位于**仓库根目录** `D:\U3D_project\AfterToken\Tools\Luban\`，而代码与数据输出位于**项目代码根目录** `D:\U3D_project\AfterToken\AfterToken\Assets\`。

### 3.2 运行方式

在 `Configs/GameConfig/` 目录下双击运行 `gen_code_bin_to_project.bat`，或在仓库根目录执行：

```bash
cmd //c "Configs\GameConfig\gen_code_bin_to_project.bat"
```

---

## 四、运行时加载

生成成功后，业务代码通过以下方式访问配置：

```csharp
var weaponCfg = ConfigSystem.Instance.Tables.TbWeapon.Get(1001);
var levelList = ConfigSystem.Instance.Tables.TbLevel.DataList;
```

`ConfigSystem` 位于 `Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs`，使用懒加载模式，首次访问 `Tables` 时通过 YooAsset 加载 `.json` 数据，内部使用 Newtonsoft.Json 反序列化。

---

## 五、与项目其他模块的关系

- **已接入模块**：`PlayerSystem`、`WeaponSystem`、`LevelSystem`、`EnemySpawnSystem`、`ItemConfigMgr`、`DropConfigMgr`、`InventoryConfigMgr`、`PortalConfigMgr`、`CameraSystem`、`BallisticSystem`、`DamageNumberUI`、`HitFeedbackUI`、`LoadingUI`、`PickupEntity`
- **YooAsset 集成**：生成的 `.json` 文件需被 `AssetRaw/Configs/` 收集器包含。
- **热更**：`GameProto/GameConfig/` 和 `AssetRaw/Configs/json/` 均位于热更域，支持热更。

---

## 六、已知问题与 TODO

| 问题 | 优先级 | 解决方案 |
|------|--------|----------|
| `Tools/Luban/` 在新环境可能缺少运行时文件 | P0 | 下载完整 Luban 工具链并解压到仓库根目录 `Tools/Luban/` |
| `TbWave` 数据已存在但尚未接入业务 | P1 | 在 `EnemySpawnSystem` 中接入波次生成 |
| `TbBuff` 数据已存在但尚未接入业务 | P1 | 实现 Buff/Debuff 系统 |
| 配置表热更验证 | P1 | 确认 YooAsset 收集器包含 `AssetRaw/Configs/json/`，修改 Excel 后重新导表并验证运行时加载 |

---

## 七、JSON 格式说明

项目已从二进制（`cs-bin` + `bin`）切换到 JSON（`cs-newtonsoft-json` + `json`），原因：

1. **调试方便**：`.json` 文件可直接打开查看配置内容。
2. **版本控制友好**：Git diff 可清晰看到配置变更。

切换涉及改动：

| 文件 | 改动 |
|------|------|
| `gen_code_bin_to_project.bat` | `-c cs-bin -d bin` → `-c cs-newtonsoft-json -d json`；输出目录改为 `AssetRaw/Configs/json/` |
| `CustomTemplate/ConfigSystem.cs` | `LoadByteBuf` → `LoadJson`；`ByteBuf` → `string`；使用 Newtonsoft.Json 反序列化 |
| `AssetRaw/Configs/` | 新增 `json/` 目录，YooAsset 收集器需包含该目录 |

## 八、新增/修改字段速查

### 修改现有 bean 字段

1. 改 `__beans__.xlsx` 中对应 bean 的字段定义。
2. 改数据 `.xlsx` 的 `##var` / `##type` / `##` 行与数据行。
3. 运行 `gen_code_bin_to_project.bat`。
4. 验证 `cfg/Xxx.cs` 与 `cfg_tbxxx.json`。

### 新增表

1. 在 `__beans__.xlsx` 定义 bean。
2. 在 `__tables__.xlsx` 注册 `cfg.TbXxx`。
3. 创建 `Datas/<file>.xlsx` 并填入数据。
4. 运行 `gen_code_bin_to_project.bat`。
5. 确认 `ConfigSystem.cs` 的 `_tableFiles` 已包含 `cfg_tbxxx`。

### 手动同步（Luban 不可用）

参见 `docs/modules/pipeline/luban-config-system/ADDING-NEW-CONFIG.md` 的“Luban 不可用时的人工同步”章节。

---

## 九、相关文档

- 配置系统总览：`docs/framework/06-ConfigSystem.md`
- 新增配置流程：`docs/modules/pipeline/luban-config-system/ADDING-NEW-CONFIG.md`
- 项目开发计划：`docs/开发计划方案.md`
- 整体 TodoList：`docs/TODO.md`
- 武器系统：`docs/modules/combat/weapon-system/README.md`
- 关卡系统：`docs/modules/combat/level-system/README.md`
