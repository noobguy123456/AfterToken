# Luban Config System 进度

## 已完成

- [x] Luban 工具链目录结构搭建（`Tools/Luban/`）
- [x] Luban 配置工程搭建（`Configs/GameConfig/`）
- [x] `luban.conf` 主配置
- [x] `Defines/builtin.xml` Unity 类型映射
- [x] `CustomTemplate/ConfigSystem.cs` 和 `ExternalTypeUtil.cs`
- [x] `gen_code_bin_to_project.bat` 生成脚本
- [x] `__tables__.xlsx` / `__enums__.xlsx` 注册索引与枚举
- [x] `weapon.xlsx` 武器表（5 条数据）
- [x] `level.xlsx` 关卡表（2 条数据）
- [x] `item.xlsx` 示例物品表（10 条数据，含复杂类型）
- [x] 生成脚本切换为 JSON 输出（`cs-newtonsoft-json` + `json`）
- [x] `ConfigSystem.cs` 改为 JSON 加载（Newtonsoft.Json）
- [x] 创建 `AssetRaw/Configs/json/` 输出目录

## 进行中

- [ ] 本地运行生成脚本验证 JSON 输出

## 待办

- [ ] 运行 `gen_code_bin_to_project.bat` 验证生成流程
- [ ] 修复 `item.xlsx` 中未定义的 Bean/Enum（`ItemExchange`、`EQuality`）
- [ ] 在 `__tables__.xlsx` 中注册 `cfg.TbItem`
- [ ] 补充缺失的战斗表
  - [ ] `TbPlayer`
  - [ ] `TbEnemy`
  - [ ] `TbWave`
  - [ ] `TbDrop`
  - [ ] `TbBuff`
- [ ] 配置 YooAsset 收集器包含 `AssetRaw/Configs/json/`
- [ ] 替换硬编码 `WeaponConfigMgr` / `LevelConfigMgr`
- [ ] Play Mode 验证配置加载

## 阻塞

- 当前环境 `Tools/Luban/` 仍缺少运行时文件；需在本地确保完整 Luban 工具链后运行生成脚本。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
