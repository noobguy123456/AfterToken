# Item System 进度

## 已完成
- [x] `cfg.Item` 扩展（`EItemType`、`EQuality`、`Icon`、`StackLimit`、`Price`、`Desc`、`ExchangeList`）
- [x] 4 档稀有度（红/黄/紫/蓝）与 `RarityColors` 运行时颜色映射
- [x] 稀有度边框 prefab（`RarityFrame` 资源 + `ItemSlotWidget` 动态绑定）
- [x] `ItemConfigMgr` 业务隔离层（`Get`/`GetName`/`GetQuality`/`GetStackLimit`）
- [x] `ItemStack` 数据结构与 `RunInventory`/`Warehouse` 堆叠逻辑

## 进行中
- [ ] 无

## 待办
- [ ] 道具使用效果（消耗品、材料、装备等）
- [ ] 道具图标美术资源替换占位图
- [ ] 配置表字段扩展（如使用冷却、使用范围、增益效果）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
