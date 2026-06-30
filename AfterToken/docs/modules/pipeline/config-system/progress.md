# 配置表系统进度

> 本项目配置表系统基于 Luban，输出格式已切换为 JSON。
> 详细进度见：`docs/modules/pipeline/luban-config-system/progress.md`

## 当前总状态

🟡 配置工程已搭建，生成脚本已改为 JSON 输出，**Luban 生成逻辑已跑通**，当前仅缺表数据补充。

## 已验证项

- [x] `cs-newtonsoft-json` + `json` 生成成功
- [x] 生成的 `Tables` 构造函数签名与 `ConfigSystem.LoadJson` 匹配
- [x] `AssetRaw/Configs/json/` 下可正确生成 `.json` 文件

## 待补充数据

- `TbPlayer` / `TbPlayerAttr`
- `TbEnemy`
- `TbWave`
- `TbDrop`
- `TbBuff`
- `TbItem`（需先修复 `item.xlsx` 中 `ItemExchange` / `EQuality` 定义）

## 下一步

1. 用户补充上述缺失表数据后，重新运行 `Configs/GameConfig/gen_code_bin_to_project.bat`。
2. 配置 YooAsset 收集器包含 `AssetRaw/Configs/json/`。
3. 替换硬编码 `WeaponConfigMgr` / `LevelConfigMgr` 为 `TbWeapon` / `TbLevel`。
4. Play Mode 验证配置加载与战斗闭环。
