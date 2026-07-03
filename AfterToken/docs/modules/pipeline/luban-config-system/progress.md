# Luban Config System 进度

## 已完成

- [x] Luban 工具链目录结构搭建（`Tools/Luban/`）
- [x] Luban 配置工程搭建（`Configs/GameConfig/`）
- [x] `luban.conf` 主配置
- [x] `Defines/builtin.xml` Unity 类型映射
- [x] `CustomTemplate/ConfigSystem.cs` 和 `ExternalTypeUtil.cs`
- [x] `gen_code_bin_to_project.bat` 生成脚本
- [x] `__tables__.xlsx` / `__beans__.xlsx` / `__enums__.xlsx` 注册索引、bean 定义与枚举
- [x] `weapon.xlsx` 武器表（5 条数据）
- [x] `level.xlsx` 关卡表（2 条数据）
- [x] `item.xlsx` 示例物品表（10 条数据，含复杂类型）
- [x] `player.xlsx` 玩家属性表（已接入 `PlayerSystem`）
- [x] `battle.xlsx` 战斗表（含 `Enemy` / `Wave` / `Drop` 三个 sheet）
- [x] 生成脚本切换为 JSON 输出（`cs-newtonsoft-json` + `json`）
- [x] `ConfigSystem.cs` 改为 JSON 加载（Newtonsoft.Json）
- [x] 创建 `AssetRaw/Configs/json/` 输出目录
- [x] Luban 生成逻辑已跑通：本地运行 `gen_code_bin_to_project.bat` 可正常生成 `GameProto/GameConfig/` 代码与 `AssetRaw/Configs/json/` 数据
- [x] 新增配置表流程文档：[ADDING-NEW-CONFIG.md](./ADDING-NEW-CONFIG.md)

## 进行中

- [ ] `buff.xlsx` 数据补充与 Buff 系统接入
- [ ] 修复 `item.xlsx` 中未定义的 Bean/Enum（`ItemExchange`、`EQuality`）并注册 `cfg.TbItem`
- [ ] 配置 YooAsset 收集器包含 `AssetRaw/Configs/json/`
- [ ] 替换硬编码 `WeaponConfigMgr` / `LevelConfigMgr`
- [ ] Play Mode 验证配置加载

## 待办

- [ ] 玩家/武器/敌人/关卡系统全面接入对应 Luban 表
- [ ] 配置表热更验证：修改 Excel 后重新导表 + SimulateBuild

## 阻塞

- 无

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
