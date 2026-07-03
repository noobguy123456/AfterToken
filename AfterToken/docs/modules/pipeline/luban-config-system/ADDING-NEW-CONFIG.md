# Luban 配置表新增流程

> 本文档说明 AfterToken 项目中如何新增/修改 Luban 配置表、字段和枚举。
> 配置工程位于 `Configs/GameConfig/`。

---

## 一、三个核心定义文件

Luban 配置表由三层结构组成：

| 文件 | 作用 | 通俗理解 |
|---|---|---|
| `__enums__.xlsx` | 定义枚举 | 定义“类型”：武器类型、弹道类型、品质等 |
| `__beans__.xlsx` | 定义数据结构（bean） | 定义“类”：玩家属性、武器属性、敌人属性等 |
| `__tables__.xlsx` | 注册数据表 | 定义“表”：哪个 bean、数据存在哪个文件、主键是什么 |

真实数据则分散在各自的 `.xlsx` 中（如 `player.xlsx`、`weapon.xlsx`、`battle.xlsx`）。

### 1.1 `__enums__.xlsx`：枚举定义

枚举用于限制某个字段的取值范围，生成代码时会变成 C# 枚举。

示例：`cfg.WeaponType`

```text
full_name       | flags | unique | group | comment
---------------------------------------------------
cfg.WeaponType  | False | True   | c     | 武器类型

name    | value | comment
-------------------------
Pistol  | 0     | 手枪
SMG     | 1     | 冲锋枪
Rifle   | 2     | 步枪
Sniper  | 3     | 狙击枪
Rocket  | 4     | 火箭筒
```

字段说明：

| 属性 | 说明 |
|---|---|
| `full_name` | 枚举全名，如 `cfg.WeaponType` |
| `flags` | 是否为位标志枚举，普通枚举填 `False` |
| `unique` | 枚举值是否唯一，通常填 `True` |
| `group` | 分组，客户端填 `c` |
| `name` / `value` / `comment` | 枚举项名、值、注释 |

### 1.2 `__beans__.xlsx`：数据结构定义

bean 是配置表的基本数据单元，生成代码时会变成 C# 类。

示例：`cfg.Player`

```text
full_name  | comment  | group
------------------------------
cfg.Player | 玩家属性 | c

name                  | type    | comment
-------------------------------------------
id                    | int     | 玩家ID
maxHp                 | int     | 最大血量
maxStamina            | int     | 最大体力
staminaRecoveryRate   | float   | 体力恢复速率
dodgeStaminaCost      | int     | 闪避消耗体力
moveSpeed             | float   | 移动速度
dodgeSpeed            | float   | 闪避速度
dodgeDuration         | float   | 闪避持续时间
rotationSpeed         | float   | 旋转速度
colliderRadius        | float   | 碰撞体半径
prefab                | string  | 预制体
```

字段说明：

| 属性 | 是否必填 | 说明 |
|---|---|---|
| `full_name` | 必填 | bean 全名，如 `cfg.Player`。生成后对应 `GameConfig.cfg.Player` 类 |
| `parent` | 可选 | 父 bean，支持继承 |
| `comment` | 建议 | 中文注释 |
| `group` | 建议 | 分组，客户端填 `c` |
| `name` | 必填 | 字段名，必须**小写驼峰**，如 `maxHp`。生成 C# 代码时会自动转为 `MaxHp` |
| `type` | 必填 | 字段类型 |
| `comment` | 建议 | 字段中文注释 |

常用字段类型：

| 类型 | 示例 | 说明 |
|---|---|---|
| `bool` | `true` / `false` | 布尔 |
| `int` | `100` | 整数 |
| `long` | `1000000` | 长整数 |
| `float` | `1.5` | 浮点数 |
| `string` | `"Player_Hero"` | 字符串 |
| `cfg.WeaponType` | `Pistol` | 引用枚举 |
| `cfg.Color` | `0,1,0,1` | 引用 bean |
| `list<int>` | `1001,1002,1003` | 整数列表，用逗号分隔 |
| `list<string>` | `a,b,c` | 字符串列表 |
| `map<int,int>` | `1:100,2:200` | 字典 |

### 1.3 `__tables__.xlsx`：表注册

定义一个可读取的数据表，把 bean 和真实数据文件关联起来。

示例：

```text
full_name      | value_type | read_schema_from_file | input            | index | mode | group | comment
---------------------------------------------------------------------------------------------------------------
cfg.TbWeapon   | Weapon     | False                 | weapon.xlsx      | id    | map  | c     | 武器表
cfg.TbLevel    | Level      | False                 | level.xlsx       | id    | list | c     | 关卡表
cfg.TbPlayer   | Player     | False                 | player.xlsx      | id    | map  | c     | 玩家属性表
cfg.TbEnemy    | Enemy      | False                 | Enemy@battle.xlsx| id    | map  | c     | 敌人属性表
cfg.TbWave     | Wave       | False                 | Wave@battle.xlsx | id    | list | c     | 波次表
cfg.TbDrop     | Drop       | False                 | Drop@battle.xlsx | id    | list | c     | 掉落表
```

字段说明：

| 属性 | 说明 |
|---|---|
| `full_name` | 表全名，生成后对应 `GameConfig.cfg.TbWeapon` 等 |
| `value_type` | 关联的 bean 名（不带 `cfg.` 前缀），必须与 `__beans__.xlsx` 中的 bean 名一致 |
| `read_schema_from_file` | `False`：结构从 `__beans__.xlsx` 读取；`True`：从数据文件的 `##var/##type` 行读取 |
| `input` | 数据文件路径。单个文件用 `file.xlsx`；多表文件用 `SheetName@file.xlsx` |
| `index` | 主键字段名，如 `id` |
| `mode` | `map`：按主键索引；`list`：列表 |
| `group` | 分组，客户端填 `c` |

当前项目统一使用 `read_schema_from_file = False`，所以结构定义全部在 `__beans__.xlsx` 中维护。

---

## 二、新增配置的完整流程

### 情况 A：在现有 bean 中新增字段

例如：给 `cfg.Player` 新增 `reviveCount`（复活次数）。

1. **修改 `__beans__.xlsx`**
   - 找到 `cfg.Player` bean。
   - 在字段列表末尾新增一行：`reviveCount | int | 复活次数`。

2. **修改数据文件（如 `player.xlsx`）**
   - 在 `##var` 行末尾新增列 `reviveCount`。
   - 在 `##type` 行对应位置新增类型 `int`。
   - 在数据行填入具体数值。

3. **重新生成代码与数据**
   ```bash
   Configs/GameConfig/gen_code_bin_to_project.bat
   ```

4. **在代码中使用**
   ```csharp
   var playerCfg = ConfigSystem.Instance.Tables.TbPlayer.Get(1);
   int reviveCount = playerCfg.ReviveCount;
   ```

### 情况 B：新增一个独立配置表

例如：新增 `cfg.Skill` 和对应数据表 `skill.xlsx`。

1. **在 `__beans__.xlsx` 中定义 `cfg.Skill`**
   ```text
   full_name  | comment | group
   cfg.Skill  | 技能配置 | c

   name        | type    | comment
   id          | int     | 技能ID
   name        | string  | 技能名
   cooldown    | float   | 冷却时间
   damage      | int     | 伤害
   ```

2. **创建 `Configs/GameConfig/Datas/skill.xlsx`**
   - 第一行 `##var`：`id, name, cooldown, damage`
   - 第二行 `##type`：`int, string, float, int`
   - 第三行 `##`：中文注释
   - 从第四行开始填数据。

3. **在 `__tables__.xlsx` 中注册 `cfg.TbSkill`**
   ```text
   full_name     | value_type | read_schema_from_file | input       | index | mode | group | comment
   cfg.TbSkill   | Skill      | False                 | skill.xlsx  | id    | map  | c     | 技能表
   ```

4. **重新生成代码与数据**
   ```bash
   Configs/GameConfig/gen_code_bin_to_project.bat
   ```

5. **在代码中使用**
   ```csharp
   var skillCfg = ConfigSystem.Instance.Tables.TbSkill.Get(1001);
   float cd = skillCfg.Cooldown;
   ```

### 情况 C：新增枚举

例如：新增 `cfg.DamageType`（伤害类型：Physical / Fire / Ice）。

1. **在 `__enums__.xlsx` 中定义**
   ```text
   full_name       | flags | unique | group | comment
   cfg.DamageType  | False | True   | c     | 伤害类型

   name     | value | comment
   Physical | 0     | 物理
   Fire     | 1     | 火
   Ice      | 2     | 冰
   ```

2. **在 `__beans__.xlsx` 的某个 bean 中使用**
   - 例如给 `cfg.Weapon` 加字段 `damageType | cfg.DamageType | 伤害类型`。

3. **在数据文件（如 `weapon.xlsx`）中填入枚举名**
   - 新列填 `Physical` / `Fire` / `Ice`。

4. **重新生成代码与数据**
   ```bash
   Configs/GameConfig/gen_code_bin_to_project.bat
   ```

---

## 三、一个 Excel 放多个 Sheet 的情况

`battle.xlsx` 就是一个文件里放多个表：

| Sheet | 对应 bean | 注册表 |
|---|---|---|
| `Enemy` | `cfg.Enemy` | `cfg.TbEnemy` |
| `Wave` | `cfg.Wave` | `cfg.TbWave` |
| `Drop` | `cfg.Drop` | `cfg.TbDrop` |

注册方式：

```text
full_name      | value_type | input
-----------------------------------------
cfg.TbEnemy    | Enemy      | Enemy@battle.xlsx
cfg.TbWave     | Wave       | Wave@battle.xlsx
cfg.TbDrop     | Drop       | Drop@battle.xlsx
```

要点：
- 每个 sheet 都要在 `__beans__.xlsx` 中定义一个独立的 bean。
- `input` 使用 `SheetName@file.xlsx` 格式。
- sheet 内的 `##var` / `##type` 行在 `read_schema_from_file = False` 时仅供参考。

---

## 四、生成与验证

修改完配置后，必须执行：

```bash
E:\U3D_project\AfterToken\AfterToken\Configs\GameConfig\gen_code_bin_to_project.bat
```

该脚本会：
1. 根据 `__beans__.xlsx` / `__enums__.xlsx` / `__tables__.xlsx` 生成 C# 代码到 `Assets/GameScripts/HotFix/GameProto/GameConfig/`。
2. 生成 JSON 数据到 `Assets/AssetRaw/Configs/json/`。
3. 复制 `ConfigSystem.cs` 等桥接文件。

生成后，Unity 会自动触发编译。若 Console 出现 `CS1061` 等字段找不到的错误，通常是：
- `__beans__.xlsx` 里漏了字段。
- `__tables__.xlsx` 的 `value_type` 与 bean 名不一致。
- 数据文件里的列名与 bean 字段名不一致。

---

## 五、注意事项

1. **字段命名必须小写驼峰**
   - bean 中写 `maxHp`，生成代码为 `MaxHp`。
   - 数据文件中的列名也要写 `maxHp`。

2. **`read_schema_from_file` 当前统一为 `False`**
   - 所以修改了数据文件的 `##var/##type` 行不会生效，必须在 `__beans__.xlsx` 里改。

3. **新增表不要忘记在 `__tables__.xlsx` 注册**
   - 只改 `__beans__.xlsx` 不会生成 `TbXXX` 访问类。

4. **新增枚举不要忘记在 `__enums__.xlsx` 注册**
   - 否则 bean 中使用 `cfg.Xxx` 类型会报错。

5. **group 统一填 `c`**
   - 表示客户端保留。不填可能导致字段被过滤掉。

6. **数据文件从第四行开始**
   - 第 1 行 `##var`，第 2 行 `##type`，第 3 行 `##` 注释，第 4 行起真实数据。
