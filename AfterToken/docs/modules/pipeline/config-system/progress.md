# 配置表系统进度

> 本项目配置表系统基于 Luban，输出格式已切换为 JSON。
> 详细进度见：`docs/modules/pipeline/luban-config-system/progress.md`

## 当前总状态

🟡 配置工程已搭建，生成脚本已改为 JSON 输出，待本地运行验证。

## 主要待验证项

- `cs-newtonsoft-json` + `json` 生成是否成功
- 生成的 `Tables` 构造函数签名是否与 `ConfigSystem.LoadJson` 匹配
- `AssetRaw/Configs/json/` 下是否正确生成 `.json` 文件

## 下一步

1. 在本地运行 `Configs/GameConfig/gen_code_bin_to_project.bat`。
2. 根据生成结果微调 `ConfigSystem.cs`（如有必要）。
3. 配置 YooAsset 收集器包含 `AssetRaw/Configs/json/`。
4. 替换硬编码 `WeaponConfigMgr` / `LevelConfigMgr`。
