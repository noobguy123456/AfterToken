# 道具系统 进度

## 已完成

- [x] `cfg.EQuality` 改为 4 档稀有度（蓝/紫/黄/红）并重新生成
- [x] `cfg.EItemType` 枚举（Material/Consumable）
- [x] `cfg.Item` 扩展字段（itemType/icon/stackLimit）+ 示例数据（4 档各 1 个示例道具）
- [x] `cfg.InventoryConfig` 新表（临时背包 12 槽 / 仓库 200 槽）
- [x] `cfg.Drop` 示例掉落行（enemyId 9001/9002 → 示例道具）
- [x] `ItemStack` / `RarityColors` / `ItemConfigMgr` / `DropConfigMgr` / `InventoryConfigMgr`
- [x] `ItemSlotWidget` + `ItemSlot.prefab` 稀有度框（脚本部分）
- [x] `ItemTooltipUI` 悬浮提示（读配置表展示名称/稀有度/类型/价格/描述，跟随鼠标）
- [x] 修复背包不刷新：BattleBagUI/WarehouseUI 绑定路径补 `m_img_Background/` 前缀

## 进行中

- [ ] 无

## 待办

- [ ] Play Mode 全链路人工验证
- [ ] 道具使用效果（Consumable 使用逻辑）
- [ ] 道具图标美术资源接入
- [ ] 仓库持久化（依赖 save-system）

## 阻塞

- 无
