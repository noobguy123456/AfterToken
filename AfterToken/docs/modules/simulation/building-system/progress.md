# 建筑系统进度

## 已完成
- [x] 建筑建造、升级、拆除
- [x] `BuildingEntity` 场景实体（加载 3D 模型、按状态/进度变色显示）
- [x] `BuildingInstance` 运行时数据管理
- [x] `BuildingPlacementSystem` 3D 摆放：半透明预览、落点校验（`CanPlaceAt`）、点击落位、随时取消
- [x] 全局统一网格 `MapGrid`：基础格 1m、寻路子格 0.5m，所有场景严格统一（2026-08-02 决策）
- [x] `TbBuilding` 新增 `footprintX/Z` 占地配置：工坊/农场 4x4，贸易站/装饰 2x2（1m 格）
- [x] `BuildingSystem` 网格占用表：建造吸附 + 占地查重、拆除释放、`IsAreaFree` 查询
- [x] `BuildingModelFactory` 按类型拼装占位方块模型（正式模型资源到位前使用），实体与摆放预览共用
- [x] 摆放模式网格线：仅 `StartPlacement` 期间显示 1m 网格，取消即销毁；**线画在格子边界 x.5**（格中心在 1m 整数倍坐标），与建筑实际占地严格对齐（2026-08-02 修复：此前误画在格中心整数坐标，视觉与占地差半格导致"放不下去"）
- [x] R 键旋转（2026-08-02）：摆放中按 R 旋转 90°（`_rotationY` 0/90/180/270），旋转 90/270 时占地 X/Z 对调（预览吸附、占用校验、建造全链路一致）；`BuildingSystem.TryBuild` 新增 `rotationY` 参数，实体按朝向创建
- [x] 修复 `ConfigSystem._tableFiles` 漏配：模拟经营等 10 张表（含 `cfg_tbbuilding`）此前未被预加载、运行时为空表
- [x] 修复 `BuildingEntity`：模型加载返回 null 时无回退（实体不可见）、代码拼装模型被误走 `UnloadAsset` 销毁报错
- [x] `BuildingSystem.CanBuild(configId, position, rotationY, out reason)` 统一建造校验（2026-08-02）：占地/同类型已存在/金币/材料不足，依次输出中文失败原因；`TryBuild` 改为先调 CanBuild。摆放预览红色染色与落点校验改走 CanBuild——此前预览只查占地，材料/金币不足时"绿色却放不下去"无任何提示
- [x] 摆放失败飘字提示（2026-08-02）：`BuildingPlacementSystem.ShowFloatText` 世界空间 Canvas + **legacy `UnityEngine.UI.Text`**（`Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`）——项目 TMP 字库为 Latin-only，TMP 中文显示方框，中文 UI 文字目前一律走 legacy 动态字体；内嵌 `FloatTextAnim` 上飘淡出 1.2s 销毁
- [x] 连续摆放（2026-08-02）：放置成功/失败均保持选中不退出摆放模式，仅右键/Esc 取消；失败时飘字提示具体原因（如"材料不足"）
- [x] 测试期默认物资（2026-08-02）：`ProcedureSimulation.GrantTestMaterials` 进入经营流程时把所有建筑建造/升级材料补足到堆叠上限（Wood/Stone x99），避免测试被"材料不足"卡住；正式经济循环（生产/订单产出）接入后移除
- [x] 同类型建筑数量上限三方式并存解锁（2026-08-02）：`TbBuilding` 新增 5 字段（配置方法已备注在 `__beans__.xlsx` comment 列与 `building.xlsx` 注释行）——`maxCount` 基础值、`maxCountPerPlayerLevel` 玩家等级解锁、`maxCountUpgradeLevel` 升级解锁（**默认方式**：同类每有 1 座达到该等级上限 +1）、`maxCountSlotBaseCost`/`maxCountSlotCostGrow` 金币购买栏位（线性涨价）；`BuildingSystem.GetMaxCount/CountByConfig/GetSlotPrice/TryPurchaseSlot`，`CanBuild"该建筑已存在"`改为`"数量已达上限"`；管理面板建筑项显示 `[当前/上限]` + 右侧 Unlock 按钮。当前配置：工坊/农场基础上限 1、Lv3 升级解锁、500G 起购；装饰基础 3、100G 起购；农场另支持玩家每级 +1
- [x] 事故修复（2026-08-02）：Luban bat 的"复制桥接文件"步骤用旧模板覆盖了 `GameProto/ConfigSystem.cs`，此前"`_tableFiles` 补 10 张表"的修复（未提交 git）丢失，运行时建筑表又变空表；已把 19 张表清单同时写入 `GameProto/ConfigSystem.cs` 和 `Configs/GameConfig/CustomTemplate/ConfigSystem.cs`（模板源头），今后重跑 Luban 不会再丢。**教训：改 `GameProto/ConfigSystem.cs` 必须同步改 CustomTemplate 模板**

## 实现说明
1. `TbBuilding` 配置建筑消耗、耗时、队列槽位、解锁等级、占地格数（`footprintX/Z`）。
2. `BuildingSystem.TryBuild` / `TryUpgrade` 消耗 `CurrencySystem` 金币与 `InventorySystem` 材料。
3. 监听 `ISimulationEvent.OnSimulationTimeAdvanced` 推进建造/升级进度，完成后广播事件。
4. 拆除功能已实现（`TryDemolish`），本期未在 UI 中暴露。
5. 建筑摆放走 `BuildingSelectionUI` 选建筑 → `BuildingPlacementSystem.StartPlacement` → 预览跟随鼠标 → 点击落位调用 `TryBuild`；`BuildingEntity` 在 `BuildingRoot` 下创建。
6. 网格约定（`MapGrid`）：奇数格建筑中心对齐 1m 整数倍坐标，偶数格对齐 x.5 格缝；`NavigationSystem` 寻路子格固定引用 `MapGrid.NavCellSize`，勿在 Inspector 改。
7. 占位模型按类型区分外形与颜色：工坊（棕主体+烟囱）、农场（田块+绿作物）、贸易站（蓝主体+雨棚）、装饰（白底座+金立柱）；正式模型到位后在配置表填 `icon` 资源地址即可自动替换。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
