# 提案：模拟经营系统 MVP 开发方案

> 提案状态：已接受  
> 提出时间：2026-07-22  
> 提案路径：`docs/Proposal/simulation/simulation-mvp.md`  
> 关联模块：  
> - `docs/modules/simulation/simulation-system/`
> - `docs/modules/simulation/sim-time-system/`
> - `docs/modules/simulation/building-system/`
> - `docs/modules/simulation/production-system/`
> - `docs/modules/simulation/order-system/`
> - `docs/modules/simulation/farm-system/`（本阶段不实现）
> - `docs/modules/simulation/worker-system/`（本阶段不实现）
> 关联文档：
> - `docs/项目架构方案.md`
> - `docs/开发计划方案.md`
> - `docs/俯视角2D射击游戏功能设计.md`

---

## 1. 背景

项目当前战斗主链路已跑通（`MainMenu → Lobby → Battle`），并已完成道具/背包系统的最小闭环（`RunInventory` 临时背包 + `Warehouse` 长期仓库）。按 `docs/开发计划方案.md` 阶段四规划，接下来需要开发**模拟经营玩法**。

模拟经营模块当前 7 个子系统全部处于 ⏳ 未开始状态， README 仅包含一句话职责与类名。为避免一次性开发量过大，本提案建议先落地 **MVP（最小可玩闭环）**：

> **建造 → 生产 → 交付订单 → 获得货币 → 再建造**

农场、工人、季节、解锁链等高级功能在后续迭代中补充。

---

## 2. 目标

### 2.1 本期目标（MVP）

1. 玩家可从主菜单进入经营场景。
2. 经营时间可推进、暂停、加速。
3. 玩家可消耗货币与材料建造/升级建筑。
4. 建筑可加入生产队列，按配方消耗材料并产出物品。
5. 系统定时生成订单，玩家交付订单获得货币与材料奖励。
6. 经营主 UI 显示：时间、资源、建筑列表、生产队列、订单板。

### 2.2 本期不做

- `FarmSystem` / `CropEntity` 种植与季节。
- `WorkerSystem` 工人分配与成长。
- 建筑在场景中的 2D 摆放与拖拽（本期建筑列表以 UI 形式呈现，场景仅作装饰背景）。
- 经营存档持久化（仍沿用战斗仓库的内存态方案，重启清空）。
- 战斗与经营的跨玩法奖励联动（奖励货币/材料即可）。

---

## 3. 需求总览

### 3.1 各子系统职责（从现有文档提炼）

| 系统 | 职责 | 本期是否包含 |
|------|------|-------------|
| `SimulationSystem` | 经营总控，初始化子系统，驱动流程 | ✅ |
| `ProcedureSimulation` | 经营流程（场景加载、UI 打开、清理） | ✅ |
| `SimTimeSystem` | 时间推进、暂停、加速、倍率事件分发 | ✅ |
| `BuildingSystem` | 建筑建造、升级、拆除、状态管理 | ✅ |
| `ProductionSystem` | 生产队列、配方消耗、产出结算 | ✅ |
| `OrderSystem` | 订单生成、交付、刷新、奖励 | ✅ |
| `FarmSystem` | 种植、生长、收获 | ❌ |
| `WorkerSystem` | 工人分配、成长 | ❌ |
| `SimulationMainUI` | 经营主界面 | ✅ |
| `BuildingWidget` / `ResourceWidget` / `OrderWidget` | 子面板 | ✅（可简化） |

### 3.2 最小闭环流程

```
进入经营场景
    ↓
显示资源（金币/材料）与建筑列表
    ↓
选择建筑 → 校验货币/材料 → 建造（耗时）
    ↓
选择建筑 → 选择生产配方 → 开始生产（耗时）
    ↓
时间推进 → 生产完成 → 产物入仓库
    ↓
订单板生成订单 → 校验库存 → 交付 → 获得金币/材料
    ↓
用金币/材料继续建造/生产
```

---

## 4. 配置表设计

> **原则**：所有数值、配方、消耗、奖励必须先走 Luban 配置表，代码中禁止硬编码。  
> 新增表需要在 `Configs/GameConfig/Datas/__tables__.xlsx` 注册，并运行 `gen_code_bin_to_project.bat` 生成代码与 JSON。

### 4.1 新增配置表清单

| 表名 | 文件 | 说明 |
|------|------|------|
| `TbBuilding` | `building.xlsx` | 建筑配置 |
| `TbProduction` | `production.xlsx` | 生产配方 |
| `TbOrder` | `order.xlsx` | 订单配置 |
| `TbSimTimeConfig` | `sim_time_config.xlsx` | 时间推进参数 |
| `TbSimulationReward` | `simulation_reward.xlsx` | 经营奖励/掉落（可选） |

### 4.2 `TbBuilding` 字段设计

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `int` | 建筑 ID |
| `name` | `string` | 名称 |
| `desc` | `string` | 描述 |
| `buildingType` | `enum EBuildingType` | 建筑类型（Workshop/Farm/Trade/Decor） |
| `icon` | `string` | 图标资源地址 |
| `maxLevel` | `int` | 最大等级 |
| `buildCostGold` | `int` | 建造消耗金币 |
| `buildCostItems` | `list<ItemCost>` | 建造消耗材料（itemId, count） |
| `buildTime` | `float` | 建造耗时（秒） |
| `upgradeCostGold` | `int` | 每级升级消耗金币 |
| `upgradeCostItems` | `list<ItemCost>` | 每级升级消耗材料 |
| `upgradeTime` | `float` | 每级升级耗时（秒） |
| `productionSlotCount` | `int` | 该建筑拥有的生产队列槽位数 |
| `unlockLevel` | `int` | 玩家等级解锁条件（本期可填 1） |

> `ItemCost` 为通用 Bean，已在 `item.xlsx` 中有 `ItemExchange` 先例，建议统一为 `cfg.ItemCost { itemId:int, count:int }`。

### 4.3 `TbProduction` 字段设计

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `int` | 配方 ID |
| `name` | `string` | 名称 |
| `desc` | `string` | 描述 |
| `buildingId` | `int` | 可使用的建筑 ID（引用 TbBuilding） |
| `levelRequired` | `int` | 建筑等级要求 |
| `inputItems` | `list<ItemCost>` | 输入材料 |
| `outputItemId` | `int` | 产出物品 ID |
| `outputCount` | `int` | 产出数量 |
| `productionTime` | `float` | 生产耗时（秒） |
| `icon` | `string` | 图标资源地址 |

### 4.4 `TbOrder` 字段设计

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `int` | 订单 ID |
| `requiredItems` | `list<ItemCost>` | 交付所需物品 |
| `rewardGold` | `int` | 奖励金币 |
| `rewardItems` | `list<ItemCost>` | 奖励物品 |
| `rewardExp` | `int` | 奖励经验（本期可忽略） |
| `timeLimit` | `float` | 订单存在时限（秒，0 表示无限制） |
| `weight` | `int` | 随机权重 |
| `minPlayerLevel` | `int` | 最低玩家等级 |

### 4.5 `TbSimTimeConfig` 字段设计

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `int` | 配置 ID（固定 1） |
| `baseSpeed` | `float` | 基础时间倍率（1x） |
| `fastSpeed` | `float` | 加速倍率（如 2x） |
| `maxSpeed` | `float` | 最大倍率（如 4x） |
| `orderRefreshInterval` | `float` | 订单自动刷新间隔（秒） |
| `maxOrderCount` | `int` | 订单板最大订单数量 |

### 4.6 配置 Bean 建议

在 `Configs/GameConfig/Datas/__beans__.xlsx` 新增通用 Bean：

```
##var
full_name   value_type
##type
string      string
##
名称         类型

bean.ItemCost
    itemId:int
    count:int
```

> 如 `ItemExchange` 已能满足 `itemId,count` 两字段，可复用 `ItemExchange`，但建议命名清晰的 `ItemCost` 避免语义混淆。

---

## 5. 系统架构

### 5.1 模块目录结构

```
Assets/GameScripts/HotFix/GameLogic/
├── Shared/
│   ├── CurrencySystem.cs          # 新增：金币/钻石/体力管理
│   ├── InventorySystem.cs         # 新增：包装 Warehouse 的通用接口
│   └── PlayerProfileSystem.cs     # 新增：玩家等级、经验（本期可简化）
├── Simulation/
│   ├── SimulationSystem.cs        # 经营总控
│   ├── SimTimeSystem.cs           # 时间推进
│   ├── BuildingSystem.cs          # 建筑管理
│   ├── ProductionSystem.cs        # 生产管理
│   ├── OrderSystem.cs             # 订单管理
│   └── Entity/
│       └── BuildingEntity.cs      # 建筑场景实体（本期可简化）
├── IEvent/
│   ├── ISimulationEvent.cs        # 新增
│   ├── ICurrencyEvent.cs          # 新增
│   ├── IInventoryEvent.cs         # 新增（已有 IItemEvent，建议统一或新增）
│   └── IPlayerProfileEvent.cs     # 新增
├── Config/
│   ├── BuildingConfigMgr.cs       # 新增
│   ├── ProductionConfigMgr.cs     # 新增
│   ├── OrderConfigMgr.cs          # 新增
│   └── SimTimeConfigMgr.cs        # 新增
├── UI/
│   └── SimulationMainUI/
│       └── SimulationMainUI.cs    # 经营主界面
└── Procedure/
    └── ProcedureSimulation.cs     # 经营流程
```

### 5.2 系统交互图

```
                      ┌──────────────────┐
                      │  SimulationMainUI │
                      └────────┬─────────┘
                               │ ISimulationEvent / ICurrencyEvent / IInventoryEvent
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                      SimulationSystem (总控)                     │
│  - 初始化 BuildingSystem / ProductionSystem / OrderSystem          │
│  - 初始化 SimTimeSystem                                          │
└────────────┬──────────────────────┬───────────────────────────────┘
             │                      │
             ▼                      ▼
    ┌──────────────┐        ┌────────────────┐
    │ SimTimeSystem │        │  Shared Layer   │
    │  - 推进时间   │        │ CurrencySystem  │
    │  - 暂停/加速  │◄──────►│ InventorySystem │
    └──────┬───────┘        │ (Warehouse)     │
           │                 └────────────────┘
           │ OnSimulationTimeAdvanced
           ▼
    ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
    │BuildingSystem │      │ProductionSystem│     │  OrderSystem  │
    │ 建造/升级/拆除 │      │  生产队列/结算  │      │ 生成/交付/刷新 │
    └──────────────┘      └──────────────┘      └──────────────┘
```

### 5.3 事件接口定义

#### `ISimulationEvent`（新增）

```csharp
[EventInterface(EEventGroup.GroupSimulation)]
public interface ISimulationEvent
{
    void OnBuildingCompleted(int buildingId, int instanceId, int level);
    void OnBuildingUpgraded(int buildingId, int instanceId, int level);
    void OnProductionStarted(int productionId, int instanceId);
    void OnProductionFinished(int productionId, int instanceId, int itemId, int count);
    void OnOrderGenerated(int orderId, int orderInstanceId);
    void OnOrderCompleted(int orderId, int orderInstanceId);
    void OnSimulationTimeAdvanced(float deltaTime, float totalTime);
    void OnSimulationSpeedChanged(ESimSpeed speed);
}
```

#### `ICurrencyEvent`（新增）

```csharp
[EventInterface(EEventGroup.GroupCommon)]
public interface ICurrencyEvent
{
    void OnGoldChanged(long currentGold);
    void OnDiamondChanged(long currentDiamond);
    void OnEnergyChanged(int currentEnergy, int maxEnergy);
}
```

#### `IInventoryEvent`（新增，与 `IItemEvent` 并存）

`IItemEvent` 已覆盖 `RunInventory` / `Warehouse` 的变化。为经营系统提供通用查询/消耗接口，建议新增 `IInventoryEvent`：

```csharp
[EventInterface(EEventGroup.GroupCommon)]
public interface IInventoryEvent
{
    void OnItemChanged(int itemId, int count);
}
```

实际库存操作由 `InventorySystem` 提供静态方法（或包装 `Warehouse`）。

#### `IPlayerProfileEvent`（新增，可选）

```csharp
[EventInterface(EEventGroup.GroupCommon)]
public interface IPlayerProfileEvent
{
    void OnPlayerLevelUp(int newLevel);
    void OnExpChanged(int currentExp, int maxExp);
}
```

---

## 6. 关键数据结构

### 6.1 运行时建筑实例

```csharp
public class BuildingInstance
{
    public int InstanceId;        // 运行时唯一 ID
    public int ConfigId;          // TbBuilding.id
    public int Level;             // 当前等级
    public BuildingState State;   // Idle / Building / Upgrading
    public float Progress;        // 当前建造/升级进度（0~1）
    public int[] ProductionSlots; // 生产队列槽位（存储 ProductionInstanceId 或 0）
}
```

### 6.2 运行时生产实例

```csharp
public class ProductionInstance
{
    public int InstanceId;        // 运行时唯一 ID
    public int ConfigId;          // TbProduction.id
    public int BuildingInstanceId;// 所属建筑实例 ID
    public float Progress;        // 生产进度（0~1）
    public int OutputItemId;      // 产出物品 ID
    public int OutputCount;       // 产出数量
}
```

### 6.3 运行时订单实例

```csharp
public class OrderInstance
{
    public int InstanceId;        // 运行时唯一 ID
    public int ConfigId;          // TbOrder.id
    public float RemainingTime;   // 剩余时间（秒）
}
```

---

## 7. 实现步骤

### 7.1 第一阶段：配置表与共享层（1~2 天）

1. 在 `__tables__.xlsx` 注册 `TbBuilding`、`TbProduction`、`TbOrder`、`TbSimTimeConfig`。
2. 创建 `building.xlsx`、`production.xlsx`、`order.xlsx`、`sim_time_config.xlsx` 并填充示例数据。
3. 运行 `gen_code_bin_to_project.bat` 生成代码与 JSON。
4. 实现 `BuildingConfigMgr`、`ProductionConfigMgr`、`OrderConfigMgr`、`SimTimeConfigMgr`。
5. 实现 `Shared/CurrencySystem.cs`：金币增减、校验、事件广播。
6. 实现 `Shared/InventorySystem.cs`：包装 `Warehouse`，提供 `HasItems`、`ConsumeItems`、`AddItems`。
7. 实现 `Shared/PlayerProfileSystem.cs`：玩家等级/经验（本期可简化）。
8. 新增事件接口：`ISimulationEvent.cs`、`ICurrencyEvent.cs`、`IInventoryEvent.cs`、`IPlayerProfileEvent.cs`。

### 7.2 第二阶段：经营系统核心（2~3 天）

1. 实现 `SimTimeSystem`：
   - 维护 `currentTime`、`speed`、`isPaused`。
   - 每帧按 `deltaTime * speed` 推进，广播 `OnSimulationTimeAdvanced`。
   - 支持 `Pause()` / `Resume()` / `SetSpeed(ESimSpeed)`。
2. 实现 `BuildingSystem`：
   - `TryBuild(configId)`：校验货币/材料，扣除资源，创建 `BuildingInstance`。
   - `TryUpgrade(instanceId)`：校验等级/资源，进入升级状态。
   - 监听时间推进，推进 `Building/Upgrading` 进度，完成后广播事件。
3. 实现 `ProductionSystem`：
   - `TryStartProduction(buildingInstanceId, productionId)`：校验建筑类型、等级、材料，占用队列槽位。
   - 监听时间推进，推进生产进度，完成后产物入仓库并广播事件。
4. 实现 `OrderSystem`：
   - 按 `TbSimTimeConfig.orderRefreshInterval` 定时生成随机订单。
   - `TryDeliverOrder(orderInstanceId)`：校验库存，扣除物品，发放奖励，广播事件。

### 7.3 第三阶段：经营流程与 UI（2~3 天）

1. 新建 `SimulationScene`（可先复制 `LobbyScene` 作为背景）。
2. 实现 `ProcedureSimulation`：
   - 加载 `SimulationScene`。
   - 初始化 `SimulationSystem` 及各子系统。
   - 打开 `SimulationMainUI`。
3. 实现 `SimulationSystem`：
   - 在 `SimulationRoot` GameObject 上挂载/AddComponent 各子系统。
   - 提供 `Enter()` / `Leave()` 生命周期。
4. 实现 `SimulationMainUI`：
   - 顶部：金币、时间、暂停/加速按钮。
   - 左侧：建筑列表（可建造/升级）。
   - 中间：已建造建筑及其生产队列。
   - 右侧：订单板。
5. 在 `MainMenuUI` / `LobbyUI` 增加“进入经营”按钮，调用 `GameApp.ChangeProcedure<ProcedureSimulation>()`。

### 7.4 第四阶段：验证与文档同步（1 天）

1. Play Mode 验证：主菜单 → 经营 → 建造 → 生产 → 订单交付 → 返回主菜单。
2. 检查 Console 无报错、无配置表解析异常。
3. 更新各模块 `README.md` / `progress.md`。
4. 将本提案归档或更新为“已接受”。

---

## 8. 风险与决策

| 风险 | 影响 | 应对 |
|------|------|------|
| `CurrencySystem` / `PlayerProfileSystem` 尚未实现 | 中 | 本期必须新增这两个共享系统，否则无法完成建造消耗与奖励。 |
| 经营场景与战斗场景共享 `Warehouse` | 中 | 明确 `Warehouse` 为长期仓库，经营与战斗共用；`RunInventory` 仅战斗场景使用。 |
| 建筑场景摆放本期不做 | 低 | 先用 UI 列表形式，场景用静态背景或装饰物，降低美术与交互成本。 |
| 订单/生产并发推进可能导致 GC | 低 | 实例数量可控，使用 List 管理，必要时后续再引入对象池。 |
| 新增事件接口后 Source Generator 异常 | 中 | 每次新增 `[EventInterface]` 后强制刷新编译并检查 Console。 |

---

## 9. 结论

建议先按本方案落地 **模拟经营 MVP**，实现：

- `SimTimeSystem` → `BuildingSystem` → `ProductionSystem` → `OrderSystem` → `SimulationMainUI` → `ProcedureSimulation`。

农场（`FarmSystem`）与工人（`WorkerSystem`）作为第二期扩展，等 MVP 跑通后再投入。

**下一步行动**：
1. 评审本提案。
2. 确认配置表字段与示例数据。
3. 开始第一阶段实现：配置表 + 共享层 + 事件接口。
