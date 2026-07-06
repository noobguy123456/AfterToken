# Luban 配置表系统

> 项目已接入 Luban 工具链，配置工程位于 `Configs/GameConfig/`。
> 当前状态：**配置工程已搭建，数据表已定义，生成脚本可正常运行**。
> 新增/修改配置表流程见 [ADDING-NEW-CONFIG.md](./ADDING-NEW-CONFIG.md)。

---

## 一、Luban 工具链

| 文件 | 路径 | 说明 |
|------|------|------|
| `Luban.exe` | `Tools/Luban/Luban.exe` | Luban 主程序（脚本中解析为 `Configs/GameConfig/../../../Tools/Luban/Luban.exe`，即 `E:\U3D_project\AfterToken\Tools\Luban\Luban.exe`） |
| `README.md` | `Tools/Luban/README.md` | 安装说明 |

### 工具链状态

当前 `Tools/Luban/` 已包含完整运行依赖，`gen_code_bin_to_project.bat` 可正常生成代码与 JSON 数据。

`Tools/Luban/` 指**仓库根目录**下的 `Tools/Luban/`，对应本机路径为 `E:\U3D_project\AfterToken\Tools\Luban`。若在新环境遇到 `hostpolicy.dll` / `Luban.runtimeconfig.json` 缺失错误，需从 Luban 官方 Release 下载完整工具链压缩包，解压后**整个文件夹**放到仓库根目录的 `Tools/Luban/` 下。

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
├── gen_code_bin_to_project.bat             # 客户端代码生成脚本
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
│   ├── item.xlsx                           # 物品表（示例数据，含复杂类型）
│   ├── battle.xlsx                         # 战斗表（含 Wave/Drop 多个 sheet）
│   ├── buff.xlsx                           # Buff 表
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
| `cfg.TbItem` | `Item` | `map` | 物品表 |
| `cfg.TbPlayer` | `Player` | `map` | 玩家属性表 |
| `cfg.TbEnemy` | `Enemy` | `map` | 敌人属性表（数据源：`enemy.xlsx`） |
| `cfg.TbWave` | `Wave` | `list` | 波次表（数据源：`battle.xlsx#Wave`） |
| `cfg.TbDrop` | `Drop` | `list` | 掉落表（数据源：`battle.xlsx#Drop`） |
| `cfg.TbBuff` | `Buff` | `map` | Buff 表 |

> 注意：`item.xlsx` 已注册，但引用的 `item.ItemExchange` Bean 与 `item.EQuality` 枚举仍需在 `__beans__.xlsx` / `__enums__.xlsx` 中定义。

### 2.4 已定义的枚举

`Datas/__enums__.xlsx`：

| 枚举名 | 项 |
|--------|-----|
| `WeaponType` | Pistol, SMG, Rifle, Sniper, Rocket |
| `BallisticType` | Raycast, Projectile |
| `FireMode` | Single, Auto |

### 2.5 数据表内容

#### `weapon.xlsx`

- 5 把武器：1001 Pistol、1002 SMG、1003 Rifle、1004 Sniper、1005 Rocket Launcher
- 字段与当前硬编码 `WeaponConfig` 基本一致
- 字段数：28 个

#### `level.xlsx`

- 2 个关卡：1 Training Ground、2 Abandoned Factory
- 字段与当前硬编码 `LevelConfig` 一致

#### `enemy.xlsx`

- 独立敌人属性表，已从 `battle.xlsx#Enemy` 拆分出来，方便单独调整不同敌人类型。
- 当前数据：
  - `9001` Training Dummy：`prefab = Enemy`，`maxHp = 50`，`moveSpeed = 2`，`attackDamage = 5`，`attackRange = 1.5`，`attackInterval = 1`
  - `9002` Assault Bot：`prefab = Enemy_Assault`，`maxHp = 80`，`moveSpeed = 3`，`attackDamage = 15`，`attackRange = 2`，`attackInterval = 1.2`

#### `item.xlsx`

- 10 条示例物品数据
- 包含复杂类型：`item.EQuality`（枚举）、`item.ItemExchange`（Bean）、`datetime?`、`ref`、`list` 等
- **当前问题**：
  - `__tables__.xlsx` 中未注册 `cfg.TbItem`
  - `__beans__.xlsx` 中未定义 `item.ItemExchange`
  - `__enums__.xlsx` 中未定义 `item.EQuality`
  - 该表直接运行会报错，需先补齐 Bean/Enum 定义或简化表结构

---

## 三、生成脚本

`gen_code_bin_to_project.bat` 流程（脚本内所有输出与注释已改为英文，避免中文编码导致的乱码问题）：

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
| `LUBAN_EXE` | `..\..\..\Tools\Luban\Luban.exe` | `E:\U3D_project\AfterToken\Tools\Luban\Luban.exe` |
| `CODE_OUT` | `..\..\Assets\GameScripts\HotFix\GameProto` | `E:\U3D_project\AfterToken\AfterToken\Assets\GameScripts\HotFix\GameProto` |
| `DATA_OUT` | `..\..\Assets\AssetRaw\Configs\json` | `E:\U3D_project\AfterToken\AfterToken\Assets\AssetRaw\Configs\json` |

> 注意：`Luban.exe` 位于**仓库根目录** `E:\U3D_project\AfterToken\Tools\Luban\`，而代码与数据输出位于**项目代码根目录** `E:\U3D_project\AfterToken\AfterToken\Assets\`。

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

- **阻塞模块**：`WeaponSystem`、`LevelSystem`、`EnemySystem`、`PlayerSystem` 等待 Luban 表替换硬编码配置。
- **YooAsset 集成**：生成的 `.json` 文件需被 `AssetRaw/Configs/` 收集器包含。
- **热更**：`GameProto/GameConfig/` 和 `AssetRaw/Configs/json/` 均位于热更域，支持热更。

---

## 六、已知问题与 TODO

| 问题 | 优先级 | 解决方案 |
|------|--------|----------|
| `Tools/Luban/` 在新环境可能缺少运行时文件 | P0 | 下载完整 Luban 工具链并解压到仓库根目录 `Tools/Luban/` |
| `item.xlsx` 引用了未定义的 Bean/Enum | P1 | 在 `__beans__.xlsx`/`__enums__.xlsx` 中定义 `ItemExchange`/`EQuality`，或简化 item 表 |
| 未替换 `WeaponConfigMgr` / `LevelConfigMgr` | P1 | 生成代码后逐步替换 |
| `TbEnemy` 已从 `battle.xlsx#Enemy` 拆分为独立 `enemy.xlsx`，已接入业务 | P1 | 后续新增敌人类型直接在 `enemy.xlsx` 中添加，并同步创建对应 Prefab 与 YooAsset 地址 |
| `TbEnemy` 接入后 `9001` 的 `prefab` 字段原值为 `Enemy_Dummy`，与实际资源地址 `Enemy` 不符 | P0 | 已修正为 `Enemy`；`attackDamage` 已从 `10` 下调为 `5` |
| `TbWave`/`TbDrop`/`TbBuff` 尚未接入业务 | P1 | 按项目需求接入波次/掉落/Buff 系统 |

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

## 八、相关文档

- 配置系统总览：`docs/framework/06-ConfigSystem.md`
- 项目开发计划：`docs/开发计划方案.md`
- 整体 TodoList：`docs/TODO.md`
- 武器系统：`docs/modules/combat/weapon-system/README.md`
- 关卡系统：`docs/modules/combat/level-system/README.md`
