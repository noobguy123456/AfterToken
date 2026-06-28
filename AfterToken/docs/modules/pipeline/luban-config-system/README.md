# Luban 配置表系统

> 项目已接入 Luban 工具链，配置工程位于 `Configs/GameConfig/`。
> 当前状态：**配置工程已搭建，数据表已定义，但生成脚本尚未成功运行**（`Tools/Luban/` 缺少运行时依赖文件）。

---

## 一、Luban 工具链

| 文件 | 路径 | 说明 |
|------|------|------|
| `Luban.dll` | `Tools/Luban/Luban.dll` | Luban 主程序 |
| `README.md` | `Tools/Luban/README.md` | 安装说明 |

### 当前问题

运行生成脚本时会报错：

```
A fatal error was encountered. The library 'hostpolicy.dll' required to execute the application was not found...
Luban.runtimeconfig.json was not found.
```

**原因**：`Tools/Luban/` 目录下只有 `Luban.dll`，缺少 Luban 正常运行所需的 `.runtimeconfig.json`、`.deps.json` 及依赖 DLL。

**修复方法**：

从 Luban 官方 Release 下载完整工具链压缩包，解压后**整个文件夹**放到 `Tools/Luban/`，而不是只拷贝 `Luban.dll`。

官方地址：
- GitHub：https://github.com/focus-creative-games/luban/releases
- Gitee 镜像：https://gitee.com/focus-creative-games/luban/releases

完整目录示例：

```
Tools/Luban/
├── Luban.dll
├── Luban.runtimeconfig.json
├── Luban.deps.json
├── hostpolicy.dll
├── hostfxr.dll
├── ... 其他依赖 DLL
```

验证安装：

```bash
dotnet Tools/Luban/Luban.dll --help
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
│   ├── __beans__.xlsx                      # 复合类型定义（当前为空）
│   ├── __enums__.xlsx                      # 枚举定义
│   ├── weapon.xlsx                         # 武器表
│   ├── level.xlsx                          # 关卡表
│   └── item.xlsx                           # 物品表（示例数据，含复杂类型）
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

> 注意：`item.xlsx` 当前未在 `__tables__.xlsx` 中注册。

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

`gen_code_bin_to_project.bat` 流程：

1. 检查 `Tools/Luban/Luban.dll` 是否存在。
2. 复制 `CustomTemplate/ConfigSystem.cs` → `Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs`
3. 复制 `CustomTemplate/ExternalTypeUtil.cs` → `Assets/GameScripts/HotFix/GameProto/ExternalTypeUtil.cs`
4. 调用 Luban 生成：
   - 目标：`client`
   - 代码格式：`cs-newtonsoft-json`
   - 数据格式：`json`
   - 代码输出：`Assets/GameScripts/HotFix/GameProto/GameConfig/`
   - 数据输出：`Assets/AssetRaw/Configs/json/`

### 运行方式

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
| `Tools/Luban/` 缺少运行时文件 | P0 | 下载完整 Luban 工具链并替换当前目录 |
| `item.xlsx` 引用了未定义的 Bean/Enum | P1 | 在 `__beans__.xlsx`/`__enums__.xlsx` 中定义 `ItemExchange`/`EQuality`，或简化 item 表 |
| `item.xlsx` 未在 `__tables__.xlsx` 注册 | P1 | 注册 `cfg.TbItem` |
| 尚未运行生成脚本 | P0 | 修复工具链后运行 `gen_code_bin_to_project.bat` |
| 未替换 `WeaponConfigMgr` / `LevelConfigMgr` | P1 | 生成代码后逐步替换 |
| 缺少 `TbPlayer`、`TbEnemy`、`TbWave`、`TbDrop`、`TbBuff` 等战斗表 | P1 | 按项目需求补充 |

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
