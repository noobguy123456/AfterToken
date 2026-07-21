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
- [x] `item.xlsx` 道具表（10 条数据，含 `ItemExchange` / `EQuality` / `EItemType`）
- [x] `player.xlsx` 玩家属性表（已接入 `PlayerSystem`）
- [x] `enemy.xlsx` 敌人属性表（已接入 `EnemySpawnSystem`）
- [x] `battle.xlsx` 战斗表（含 `Enemy` / `Wave` / `Drop` 三个 sheet）
- [x] `buff.xlsx` Buff 表（数据已存在，业务未接入）
- [x] `inventory.xlsx` 背包容量表（已接入 `InventoryConfigMgr`）
- [x] `portal.xlsx` 传送门表（已接入 `PortalConfigMgr`）
- [x] 生成脚本切换为 JSON 输出（`cs-newtonsoft-json` + `json`）
- [x] `ConfigSystem.cs` 改为 JSON 加载（Newtonsoft.Json）
- [x] 创建 `AssetRaw/Configs/json/` 输出目录
- [x] Luban 生成逻辑已跑通：本地运行 `gen_code_bin_to_project.bat` 可正常生成 `GameProto/GameConfig/` 代码与 `AssetRaw/Configs/json/` 数据
- [x] 新增配置表流程文档：[ADDING-NEW-CONFIG.md](./ADDING-NEW-CONFIG.md)
- [x] 业务接入：`PlayerConfigMgr`/`WeaponConfigMgr`/`LevelConfigMgr`/`ItemConfigMgr`/`DropConfigMgr`/`InventoryConfigMgr`/`PortalConfigMgr`

## 进行中

- [ ] `TbWave` 接入 `EnemySpawnSystem` 波次生成
- [ ] `TbBuff` 接入 Buff 系统

## 待办

- [ ] 配置表热更验证：修改 Excel 后重新导表 + YooAsset 收集器配置 + SimulateBuild 运行时加载
- [ ] Play Mode 验证配置加载

## 阻塞

- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
