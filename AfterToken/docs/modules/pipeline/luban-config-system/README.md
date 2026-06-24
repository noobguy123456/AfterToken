# Luban 配置表系统

## 职责

使用 Luban 工具链管理游戏配置表，并在运行时通过生成的代码访问配置。

## 相关文件

| 文件 | 路径 | 说明 |
|---|---|---|
| `luban.conf` | `Configs/GameConfig/` | Luban 配置 |
| `Defines/builtin.xml` | `Configs/GameConfig/` | Schema 定义 |
| `gen_code_bin_to_project.bat` | `Configs/GameConfig/` | 生成客户端代码脚本 |

## 待接入的配置表

- `TbPlayer` / `TbPlayerAttr`
- `TbWeapon` / `TbBullet` / `TbEnemy`
- `TbWave` / `TbLevel` / `TbDrop` / `TbBuff`
- `TbItem` / `TbBuilding` / `TbProduction` / `TbCrop` / `TbOrder`

## 设计要点

- 替换现有的硬编码 `WeaponConfigMgr`、`LevelConfigMgr` 等。
