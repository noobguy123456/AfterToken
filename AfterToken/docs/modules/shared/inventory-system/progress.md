# 背包系统 进度

## 已完成
- [x] `RunInventory`（临时背包，槽位制，整批判定，OnInventoryFull 广播）
- [x] `Warehouse`（仓库，内存态，AddAll 批量入库）
- [x] `IItemEvent` 事件接口
- [x] 生命周期接线：死亡清空 / 回大厅清空 / 胜利转入仓库
- [x] `BattleBagUI` 脚本（B 键开关 + 容量显示 + 关闭按钮 + 再按 B 键关闭 + 默认显示全部容量格子含空槽位 + 打开时隐藏准星并暂停时间）
- [x] `WarehouseUI` 脚本 + LobbyUI `m_btn_Warehouse` 按钮绑定 + 大厅 ESC 关闭
- [x] `ItemTooltipUI` 悬浮提示窗（名称/稀有度/类型/价格/描述）
- [x] `ItemSlotHoverHandler` 转发鼠标悬停事件
- [x] `InventoryConfigMgr` 已接入 `TbInventoryConfig`
- [x] 格子选中高亮（仅左键点击切换：黄色框，Yellow 档稀有度显示为橙色避免撞色；`ItemSlotClickHandler` + `ItemSlotWidget.SetSelected`，空槽位不可选）
- [x] 整理排序（搜打撤规则）：`InventorySorter` 按 稀有度降序 → 价值降序 → 获取时间升序；`ItemStack.AcquireSeq` 记录获取序号（堆叠合并保留首次获取时间）；`RunInventory.Organize()` / `Warehouse.Organize()`；两个 UI 的 `m_btn_Sort` 按钮接线。整理后新道具仍追加到后面槽位，直到下次整理

## 进行中
- [ ] 无

## 待办
- [ ] Play Mode 全链路人工验证
- [ ] 仓库持久化（save-system，需包含 `AcquireSeq` 的存取）
- [ ] 背包满时 UI 提示表现（当前仅事件广播）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
