# Save System 进度

## 已完成（2026-08-06 基础版）

- [x] `SaveSystem` 总入口：单 JSON 文件（`persistentDataPath/save.json`）+ 版本号 + `Migrate()` 迁移钩子 + `Flush()` 变动即存 + 懒加载
- [x] `CurrencySystem` 持久化（金币/钻石/体力）
- [x] `PlayerProfileSystem` 持久化（等级/经验）
- [x] `Warehouse` 持久化（道具列表 + `ItemStack` 获取序号水位 `nextSeq`，重启后序号继续分配不回退）
- [x] 设置项迁移：`SensitivitySetting` / `SniperAimModeSetting` 从 PlayerPrefs 迁入 SaveSystem，旧值首次读取时一次性导入
- [x] GM 调试：`save path` / `save export` / `save clear`
- [x] 坑记录：① `JsonUtility` 要求被序列化类型带 `[Serializable]`，否则列表字段静默丢失（`ItemStack` 已补）；② 编辑器"Script Changes While Playing"设为播放结束后编译时，Play 中改代码 `isCompiling` 会一直挂起，需停 Play 才生效；③ 模块事件调用必须 `?.`，编辑模式无事件系统会 NRE
- [x] Play 实测闭环：跨 Play 重启 gold=1277/lv=2/仓库 [10000x3#5][10002x1#6]/灵敏度 2.5/开镜 Toggle 全部保留；`save clear` 复位正确
- [x] Review 优化（2026-08-08）：`Warehouse.AddAll` 批量期间抑制逐条事件与写盘（`_batchDepth` 门控），结算入库 N 种道具只写盘 1 次；删除死代码 `SaveSystem.IsInitialized` 与 3 处冗余 `EnsureLoaded`（HasGold/HasDiamond/HasItem 内部已保证）

## 待办
- [ ] 模拟经营存档（建筑摆放/生产/订单状态）
- [ ] 货币等敏感数据校验和/加密
- [ ] 云存档接入
- [ ] 战斗内临时背包的持久化（目前只在战斗流程内存）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅（基础版）
> - 每次更新后同步 `docs/TODO.md`
