# Loot Container System 进度

## 已完成
- [x] Luban 新表 `TbLootContainer`（权重掉落表，含 2 组测试数据）
- [x] `LootContainerEntity`（trigger + 占位视觉 + 首开掷点 + 已开状态）
- [x] `LootContainerSystem`（E 键开箱、拿取/Take All、死亡闸、提示 UI）
- [x] `LootContainerUI` prefab + 窗口（格子拿取、不暂停游戏）
- [x] 101 关（BattleScene_3D_L01）出生点附近放 2 个测试容器（lootTableId 1/2）
- [x] Play 实测：靠近出提示→E 开箱→首开掷点（同类堆叠合并）→单格拿取/Take All→入临时背包→拿空变灰已开，链路全通
- [x] 修复：`ConfigSystem._tableFiles` 白名单漏加 `cfg_tblootcontainer` 导致表数据为空（CustomTemplate 模板与生成拷贝同步）
- [x] 修复容器占位图标闪烁（2026-08-18）：根因——容器根节点在 y=0，平躺 SpriteRenderer 与地面（y=0）共面 z-fighting；修复：`EnsureVisualRenderer` 将 Visual 子节点抬高 0.05m。注意：任何贴地占位 SpriteRenderer 都需要留离地偏移

## 如何新增一个可开启的箱子
1. **配置掉落表**：编辑 `Configs/GameConfig/Datas/lootcontainer.xlsx`，每个 `containerId` 一组行：`itemId`（须存在于 item.xlsx）、`weight`（相对权重，无需总和 100）、`minCount`/`maxCount`（抽中后的数量区间）；然后跑 `Tools/Luban` 生成（Excel 与 `Assets/AssetRaw/Configs/json/cfg_tblootcontainer.json` 保持同步）
2. **场景摆放**：在战斗场景新建空 GameObject，挂 `LootContainerEntity`（自动补 BoxCollider trigger，2m 触发区），Inspector 里把 `Loot Table Id` 设为第 1 步的 `containerId`
3. 箱内道具 = 首开时对该 containerId 的所有行做 3 次独立权重掷点（`LootContainerEntity.RollTimes`），同类道具合并堆叠；拿空后变灰不可再交互

## 进行中
（无）

## 待办
- [ ] 容器美术资源替换占位方块
- [ ] 容器在关卡中的正式摆放规则（随机点位/固定点位）
- [ ] 统一 IInteractable 仲裁器（解决 Portal/Container 触发区重叠）
- [ ] 格子图标/名称显示依赖 item 表 icon 字段（当前为空，显示白块+数量）

---

> 状态说明：
> - 当前总状态：✅（核心链路完成，美术/摆放待迭代）
> - 每次更新后同步 `docs/TODO.md`
