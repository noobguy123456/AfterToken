# 配置表系统进度

> 本项目配置表系统基于 Luban，输出格式已切换为 JSON。
> 详细进度见：`docs/modules/pipeline/luban-config-system/progress.md`

## 当前总状态

✅ 配置工程已搭建，生成脚本已改为 JSON 输出，**Luban 生成逻辑已跑通**，所有战斗/共享相关表均已生成代码与示例数据。

## 已验证项

- [x] `cs-newtonsoft-json` + `json` 生成成功
- [x] 生成的 `Tables` 构造函数签名与 `ConfigSystem.LoadJson` 匹配
- [x] `AssetRaw/Configs/json/` 下可正确生成 `.json` 文件
- [x] `TbPlayer` / `TbWeapon` / `TbLevel` / `TbEnemy` / `TbDrop` / `TbItem` / `TbInventoryConfig` / `TbPortal` 已接入业务代码

## 待补充/接入

- [ ] `TbWave` 数据已存在，但 `EnemySpawnSystem` 仍使用硬编码参数，待接入波次生成
- [ ] `TbBuff` 数据已存在，但 Buff 系统尚未实现

## 下一步

1. 在 `EnemySpawnSystem` 中接入 `TbWave`，实现多波次敌人刷新。
2. 实现 Buff/Debuff 系统并接入 `TbBuff`。
3. 确认 YooAsset 收集器已包含 `AssetRaw/Configs/json/`。
4. Play Mode 验证配置加载与战斗闭环。

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
