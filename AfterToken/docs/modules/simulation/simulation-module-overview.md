# 模拟经营模块功能梳理

> 本文档按功能分类梳理模拟经营模块的实现方法、涉及文件、关键技术点与数据流。
> 技术栈：TEngine + HybridCLR + YooAsset + UniTask + Luban。

---

## 目录

1. [场景与摄像机功能](#1-场景与摄像机功能)
2. [经营时间系统](#2-经营时间系统)
3. [建筑系统](#3-建筑系统)
4. [生产系统](#4-生产系统)
5. [订单系统](#5-订单系统)
6. [共享系统（货币/背包/玩家档案）](#6-共享系统)
7. [UI 系统](#7-ui-系统)
8. [流程与入口](#8-流程与入口)
9. [配置表](#9-配置表)
10. [事件接口](#10-事件接口)

---

## 1. 场景与摄像机功能

### 1.1 实现方法

经营场景采用 **3D 俯视角**，场景内容（地面、相机、灯光）在 Unity 编辑器中设计，通过场景文件加载。

虚拟玩家角色（透明胶囊体）通过代码动态创建，作为摄像机的跟随目标。

### 1.2 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/AssetRaw/Scenes/SimulationScene.unity` | 经营场景文件（在 Unity 编辑器中设计） |
| `Assets/GameScripts/HotFix/GameLogic/Procedure/ProcedureSimulation.cs` | 经营流程，负责加载场景并初始化虚拟玩家和相机跟随 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/SimulationCameraController.cs` | 相机控制器，支持跟随目标、WASD 移动、鼠标拖动、滚轮缩放 |

### 1.3 关键技术点

#### 场景初始化

```csharp
// ProcedureSimulation.InitializeSceneContent()
private void InitializeSceneContent()
{
    // 1. 创建虚拟玩家角色（透明胶囊体，作为摄像机跟随目标）
    _virtualPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
    _virtualPlayer.name = "VirtualPlayer";
    _virtualPlayer.transform.position = Vector3.zero;
    _virtualPlayer.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    
    // 设置半透明材质
    var renderer = _virtualPlayer.GetComponent<Renderer>();
    renderer.material.color = new Color(1f, 1f, 1f, 0.3f);
    
    // 移除碰撞体，避免干扰点击检测
    Object.Destroy(_virtualPlayer.GetComponent<Collider>());

    // 2. 设置相机跟随虚拟玩家
    var mainCamera = Camera.main;
    var cameraController = mainCamera.GetComponent<SimulationCameraController>();
    cameraController.SetFollowTarget(_virtualPlayer.transform);
}
```

#### 相机控制器

```csharp
// SimulationCameraController
public class SimulationCameraController : MonoBehaviour
{
    private Transform _followTarget;
    private Vector3 _followOffset = new Vector3(0f, 15f, -10f);
    private bool _isFollowing = true;
    
    private void Update()
    {
        HandleKeyboardInput();  // WASD 移动（取消跟随）
        HandleMouseInput();     // 鼠标拖动（取消跟随）+ 滚轮缩放
        UpdateCameraPosition(); // 跟随目标或手动控制
    }
    
    private void UpdateCameraPosition()
    {
        if (_isFollowing && _followTarget != null)
        {
            // 跟随目标
            Vector3 targetPos = _followTarget.position + _followOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        }
        else
        {
            // 手动控制时，确保 Y 坐标为缩放值
            Vector3 position = transform.position;
            position.y = _currentZoom;
            transform.position = position;
        }
    }
    
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        _isFollowing = true;
    }
}
```

### 1.4 数据流

```
用户输入（WASD/鼠标）
    ↓
SimulationCameraController.HandleKeyboardInput / HandleMouseInput
    ↓
取消跟随（_isFollowing = false）或更新缩放（_currentZoom）
    ↓
UpdateCameraPosition()
    ↓
如果 _isFollowing = true，跟随虚拟玩家（VirtualPlayer）
如果 _isFollowing = false，手动控制相机位置
    ↓
相机移动/缩放
```

---

## 2. 经营时间系统

### 2.1 实现方法

经营时间系统采用 **事件驱动** 模式，每帧推进时间并广播事件，其他系统监听事件并更新状态。

### 2.2 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Simulation/SimTimeSystem.cs` | 时间推进、暂停、加速 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/ESimSpeed.cs` | 时间倍率枚举 |
| `Assets/GameScripts/HotFix/GameLogic/Config/SimTimeConfigMgr.cs` | 时间配置管理器 |

### 2.3 关键技术点

#### 时间推进

```csharp
// SimTimeSystem.Update()
private void Update()
{
    if (IsPaused) return;
    
    float speedMultiplier = GetSpeedMultiplier(_speed);
    float deltaTime = Time.deltaTime * speedMultiplier;
    _currentTime += deltaTime;
    
    // 广播时间推进事件
    GameEvent.Get<ISimulationEvent>().OnSimulationTimeAdvanced(deltaTime, _currentTime);
}
```

#### 速度控制

```csharp
// 速度倍率从配置表读取
private float GetSpeedMultiplier(ESimSpeed speed)
{
    var cfg = SimTimeConfigMgr.Instance;
    return speed switch
    {
        ESimSpeed.Pause => 0f,
        ESimSpeed.Normal => cfg.BaseSpeed,  // 1x
        ESimSpeed.Fast => cfg.FastSpeed,    // 2x
        ESimSpeed.Max => cfg.MaxSpeed,      // 4x
        _ => 1f,
    };
}
```

### 2.4 数据流

```
SimTimeSystem.Update()
    ↓
计算 deltaTime = Time.deltaTime * speedMultiplier
    ↓
GameEvent.Get<ISimulationEvent>().OnSimulationTimeAdvanced(deltaTime, totalTime)
    ↓
BuildingSystem.OnTimeAdvanced / ProductionSystem.OnTimeAdvanced / OrderSystem.OnTimeAdvanced
    ↓
更新建筑/生产/订单状态
```

---

## 3. 建筑系统

### 3.1 实现方法

建筑系统采用 **逻辑数据 + 场景实体** 分离的设计：
- `BuildingInstance`：纯 C# 逻辑数据（配置 ID、等级、状态、进度）
- `BuildingEntity`：MonoBehaviour 场景实体（加载 3D 模型、显示状态、处理点击）

### 3.2 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Simulation/BuildingSystem.cs` | 建筑管理入口：建造、升级、拆除、状态推进 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/BuildingInstance.cs` | 运行时建筑逻辑数据 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/Entity/BuildingEntity.cs` | 建筑场景实体：加载模型、显示状态 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/BuildingState.cs` | 建筑状态枚举 |
| `Assets/GameScripts/HotFix/GameLogic/Config/BuildingConfigMgr.cs` | 建筑配置管理器 |

### 3.3 关键技术点

#### 建造流程

```csharp
// BuildingSystem.TryBuild()
public bool TryBuild(int configId, Vector3 position, out int instanceId)
{
    // 1. 校验配置
    var cfg = BuildingConfigMgr.Instance.Get(configId);
    
    // 2. 校验重复建造
    foreach (var b in _buildings) if (b.ConfigId == configId) return false;
    
    // 3. 校验资源
    if (!CurrencySystem.HasGold(cfg.BuildCostGold)) return false;
    if (!InventorySystem.HasItems(cfg.BuildCostItems)) return false;
    
    // 4. 创建逻辑实例
    instanceId = _nextInstanceId++;
    var building = new BuildingInstance(instanceId, configId, 1, cfg.ProductionSlotCount);
    
    // 5. 扣除资源
    CurrencySystem.TryConsumeGold(cfg.BuildCostGold);
    InventorySystem.TryConsumeItems(cfg.BuildCostItems);
    
    // 6. 创建场景实体
    CreateBuildingEntityAsync(building, position).Forget();
    
    _buildings.Add(building);
    return true;
}
```

#### 状态推进

```csharp
// BuildingSystem.OnTimeAdvanced()
private void OnTimeAdvanced(float deltaTime, float totalTime)
{
    foreach (var building in _buildings)
    {
        if (building.State == BuildingState.Building)
        {
            building.Progress += deltaTime / cfg.BuildTime;
            if (building.Progress >= 1f)
            {
                building.State = BuildingState.Idle;
                GameEvent.Get<ISimulationEvent>().OnBuildingCompleted(...);
            }
            UpdateBuildingEntityState(building); // 同步场景实体状态
        }
    }
}
```

#### 场景实体状态显示

```csharp
// BuildingEntity.UpdateState()
public void UpdateState(BuildingState state, float progress)
{
    Color targetColor = state switch
    {
        BuildingState.Building => Color.Lerp(_buildingColor, _originalColor, progress),
        BuildingState.Upgrading => Color.Lerp(_upgradingColor, _originalColor, progress),
        _ => _originalColor,
    };
    
    foreach (var renderer in _renderers)
    {
        renderer.material.color = targetColor;
    }
}
```

### 3.4 数据流

```
玩家点击建筑选择 UI
    ↓
BuildingSelectionUI.SelectBuilding(configId)
    ↓
BuildingPlacementSystem.StartPlacement(configId)
    ↓
玩家点击空地
    ↓
BuildingPlacementSystem.TryPlaceBuilding()
    ↓
BuildingSystem.TryBuild(configId, position, out instanceId)
    ↓
创建 BuildingInstance + 扣除资源 + 创建 BuildingEntity
    ↓
SimTimeSystem 推进时间
    ↓
BuildingSystem.OnTimeAdvanced() 更新进度
    ↓
建造完成，广播 OnBuildingCompleted
```

---

## 4. 生产系统

### 4.1 实现方法

生产系统采用 **队列管理** 模式，每个建筑拥有固定数量的生产槽位，生产占用槽位，完成后释放。

### 4.2 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Simulation/ProductionSystem.cs` | 生产管理入口：配方校验、队列管理、产出结算 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/ProductionInstance.cs` | 运行时生产实例数据 |
| `Assets/GameScripts/HotFix/GameLogic/Config/ProductionConfigMgr.cs` | 生产配方配置管理器 |

### 4.3 关键技术点

#### 生产启动

```csharp
// ProductionSystem.TryStartProduction()
public bool TryStartProduction(int buildingInstanceId, int productionId, out int productionInstanceId)
{
    // 1. 校验建筑
    var building = _buildingSystem.GetBuilding(buildingInstanceId);
    if (building == null || building.State != BuildingState.Idle) return false;
    
    // 2. 校验配方
    var cfg = ProductionConfigMgr.Instance.Get(productionId);
    if (cfg.BuildingId != building.ConfigId) return false;
    if (building.Level < cfg.LevelRequired) return false;
    
    // 3. 校验材料
    if (!InventorySystem.HasItems(cfg.InputItems)) return false;
    
    // 4. 查找空闲槽位
    int slotIndex = _buildingSystem.FindFreeSlot(buildingInstanceId);
    if (slotIndex < 0) return false;
    
    // 5. 创建生产实例并占用槽位
    productionInstanceId = _nextInstanceId++;
    var production = new ProductionInstance(productionInstanceId, productionId, buildingInstanceId, cfg.OutputItemId, cfg.OutputCount);
    _productions.Add(production);
    _buildingSystem.TryOccupySlot(buildingInstanceId, slotIndex, productionInstanceId);
    
    // 6. 扣除材料
    InventorySystem.TryConsumeItems(cfg.InputItems);
    
    GameEvent.Get<ISimulationEvent>().OnProductionStarted(productionId, productionInstanceId);
    return true;
}
```

#### 生产完成

```csharp
// ProductionSystem.CompleteProduction()
private void CompleteProduction(ProductionInstance production)
{
    // 1. 产出入仓库
    InventorySystem.AddItem(production.OutputItemId, production.OutputCount);
    
    // 2. 广播事件
    GameEvent.Get<ISimulationEvent>().OnProductionFinished(
        production.ConfigId, production.InstanceId, production.OutputItemId, production.OutputCount);
}
```

### 4.4 数据流

```
玩家选择建筑 → 选择配方
    ↓
ProductionSystem.TryStartProduction(buildingInstanceId, productionId)
    ↓
校验建筑/配方/材料/槽位
    ↓
创建 ProductionInstance + 占用槽位 + 扣除材料
    ↓
SimTimeSystem 推进时间
    ↓
ProductionSystem.OnTimeAdvanced() 更新进度
    ↓
生产完成，产出入仓库，广播 OnProductionFinished
```

---

## 5. 订单系统

### 5.1 实现方法

订单系统采用 **定时刷新 + 随机生成** 模式，按配置间隔生成随机订单，玩家交付订单获得奖励。

### 5.2 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Simulation/OrderSystem.cs` | 订单管理入口：生成、交付、刷新、奖励 |
| `Assets/GameScripts/HotFix/GameLogic/Simulation/OrderInstance.cs` | 运行时订单实例数据 |
| `Assets/GameScripts/HotFix/GameLogic/Config/OrderConfigMgr.cs` | 订单配置管理器 |

### 5.3 关键技术点

#### 订单生成

```csharp
// OrderSystem.GenerateRandomOrder()
private void GenerateRandomOrder()
{
    var allOrders = OrderConfigMgr.Instance.GetAll();
    
    // 按权重随机选择订单
    int totalWeight = 0;
    foreach (var o in allOrders) totalWeight += o.Weight;
    
    int random = UnityEngine.Random.Range(0, totalWeight);
    int current = 0;
    foreach (var o in allOrders)
    {
        current += o.Weight;
        if (random < current)
        {
            int instanceId = _nextInstanceId++;
            var order = new OrderInstance(instanceId, o.Id, o.TimeLimit);
            _orders.Add(order);
            GameEvent.Get<ISimulationEvent>().OnOrderGenerated(o.Id, instanceId);
            return;
        }
    }
}
```

#### 订单交付

```csharp
// OrderSystem.TryDeliverOrder()
public bool TryDeliverOrder(int orderInstanceId)
{
    var order = GetOrder(orderInstanceId);
    var cfg = OrderConfigMgr.Instance.Get(order.ConfigId);
    
    // 1. 校验库存
    if (!InventorySystem.HasItems(cfg.RequiredItems)) return false;
    
    // 2. 扣除物品
    InventorySystem.TryConsumeItems(cfg.RequiredItems);
    
    // 3. 发放奖励
    CurrencySystem.AddGold(cfg.RewardGold);
    InventorySystem.AddItems(cfg.RewardItems);
    PlayerProfileSystem.AddExp(cfg.RewardExp);
    
    // 4. 移除订单
    _orders.Remove(order);
    GameEvent.Get<ISimulationEvent>().OnOrderCompleted(order.ConfigId, order.InstanceId);
    return true;
}
```

### 5.4 数据流

```
SimTimeSystem 推进时间
    ↓
OrderSystem.OnTimeAdvanced() 累计刷新计时
    ↓
达到刷新间隔，GenerateRandomOrder() 生成随机订单
    ↓
玩家点击订单 → TryDeliverOrder(orderInstanceId)
    ↓
校验库存 → 扣除物品 → 发放奖励 → 移除订单
    ↓
广播 OnOrderCompleted
```

---

## 6. 共享系统

### 6.1 货币系统 `CurrencySystem`

#### 实现方法

静态类管理金币、钻石、体力，提供增减、校验、事件广播。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Shared/CurrencySystem.cs` | 货币管理 |

#### 关键技术点

```csharp
// CurrencySystem
public static class CurrencySystem
{
    private static long _gold = 500;
    
    public static bool TryConsumeGold(long amount)
    {
        if (amount <= 0 || !HasGold(amount)) return false;
        _gold -= amount;
        GameEvent.Get<ICurrencyEvent>().OnGoldChanged(_gold);
        return true;
    }
}
```

### 6.2 背包系统 `InventorySystem`

#### 实现方法

静态类包装 `Warehouse`，提供通用查询、消耗、添加接口。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Shared/InventorySystem.cs` | 背包/仓库通用接口 |
| `Assets/GameScripts/HotFix/GameLogic/Item/Warehouse.cs` | 仓库实现 |

#### 关键技术点

```csharp
// InventorySystem
public static class InventorySystem
{
    public static bool TryConsumeItems(IReadOnlyList<ItemExchange> items)
    {
        if (!HasItems(items)) return false;
        foreach (var item in items)
        {
            Warehouse.TryConsume(item.Id, item.Num);
        }
        GameEvent.Get<IInventoryEvent>().OnItemChanged(0, 0);
        return true;
    }
}
```

### 6.3 玩家档案系统 `PlayerProfileSystem`

#### 实现方法

静态类管理玩家等级、经验，提供升级、事件广播。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Shared/PlayerProfileSystem.cs` | 玩家档案管理 |

---

## 7. UI 系统

### 7.1 经营主界面 `SimulationMainUI`

#### 实现方法

使用代码动态创建 UI 元素（文本、按钮、滚动列表），避免依赖复杂的 Prefab。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/UI/SimulationMainUI/SimulationMainUI.cs` | 经营主界面 |

#### 关键技术点

```csharp
// SimulationMainUI.BuildUI()
private void BuildUI()
{
    // 1. 创建标题
    CreateText("Simulation", new Vector2(0, 400), 48, TextAlignmentOptions.Center);
    
    // 2. 创建资源栏
    _goldText = CreateText("Gold: 0", new Vector2(-400, 350), 24, TextAlignmentOptions.Left);
    
    // 3. 创建时间控制按钮
    _pauseButton = CreateButton("Pause", new Vector2(-300, 280), () => SetSpeed(ESimSpeed.Pause));
    
    // 4. 创建建筑列表
    _buildingListRoot = CreateScrollList(new Vector2(-600, -50), new Vector2(400, 500));
    
    // 5. 创建订单列表
    _orderListRoot = CreateScrollList(new Vector2(200, -50), new Vector2(400, 500));
}
```

### 7.2 建筑选择界面 `BuildingSelectionUI`

#### 实现方法

使用代码动态创建建筑列表，显示建筑名称、描述、消耗、耗时。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/UI/BuildingSelectionUI/BuildingSelectionUI.cs` | 建筑选择界面 |

---

## 8. 流程与入口

### 8.1 经营流程 `ProcedureSimulation`

#### 实现方法

继承 `GameplayProcedureBase`，负责加载场景、初始化系统、打开 UI、清理资源。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Procedure/ProcedureSimulation.cs` | 经营流程 |

#### 关键技术点

```csharp
// ProcedureSimulation.EnterAsync()
protected override UniTaskVoid EnterAsync()
{
    return LoadSceneWithLoadingAsync("SimulationScene", async ct =>
    {
        InitializeSceneContent();      // 初始化地面、相机、灯光
        InitializeSimulationSystems(); // 初始化 SimulationSystem
        await GameModule.UI.ShowUIAsyncAwait<SimulationMainUI>();
        _simulationSystem?.Enter();
    });
}
```

### 8.2 入口 `MainMenuUI`

#### 实现方法

在主菜单动态创建 "Simulation" 按钮，点击后切换到经营流程。

#### 涉及文件

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/UI/MainMenuUI/MainMenuUI.cs` | 主菜单 |

---

## 9. 配置表

### 9.1 配置表清单

| 表名 | 文件 | 说明 |
|------|------|------|
| `TbBuilding` | `building.xlsx` | 建筑配置：建造/升级消耗、耗时、生产槽位、解锁等级 |
| `TbProduction` | `production.xlsx` | 生产配方：输入材料、产出物品、耗时、适用建筑与等级 |
| `TbOrder` | `order.xlsx` | 订单配置：需求物品、奖励、权重、时限 |
| `TbSimTimeConfig` | `sim_time_config.xlsx` | 时间配置：倍率、订单刷新间隔、订单上限 |

### 9.2 配置管理器

| 文件 | 说明 |
|------|------|
| `Assets/GameScripts/HotFix/GameLogic/Config/BuildingConfigMgr.cs` | 建筑配置管理器 |
| `Assets/GameScripts/HotFix/GameLogic/Config/ProductionConfigMgr.cs` | 生产配方配置管理器 |
| `Assets/GameScripts/HotFix/GameLogic/Config/OrderConfigMgr.cs` | 订单配置管理器 |
| `Assets/GameScripts/HotFix/GameLogic/Config/SimTimeConfigMgr.cs` | 时间配置管理器 |

---

## 10. 事件接口

### 10.1 事件接口清单

| 接口 | 说明 |
|------|------|
| `ISimulationEvent` | 建筑完成/升级、生产开始/完成、订单生成/完成、时间推进、速度切换 |
| `ICurrencyEvent` | 金币/钻石/体力变化 |
| `IInventoryEvent` | 物品变化 |
| `IPlayerProfileEvent` | 等级提升、经验变化 |

### 10.2 事件使用示例

```csharp
// 发送事件
GameEvent.Get<ISimulationEvent>().OnBuildingCompleted(buildingId, instanceId, level);

// 监听事件
_eventMgr.AddEvent<float, float>(ISimulationEvent_Event.OnSimulationTimeAdvanced, OnTimeAdvanced);

// UI 监听事件
AddUIEvent<long>(ICurrencyEvent_Event.OnGoldChanged, OnGoldChanged);
```

---

## 总结

模拟经营模块采用 **事件驱动 + 配置驱动 + 逻辑/表现分离** 的架构：

- **事件驱动**：系统间通过 `GameEvent` 解耦，避免直接引用。
- **配置驱动**：所有数值通过 Luban 配置表管理，不在代码中硬编码。
- **逻辑/表现分离**：`BuildingInstance` 负责逻辑，`BuildingEntity` 负责表现。

核心闭环：**建造 → 生产 → 订单 → 获得货币 → 再建造**。
