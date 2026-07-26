# 经营场景与建筑摆放实现方案

## 1. 场景结构

```
SimulationScene
├── Ground（地面，用于点击检测）
├── Camera（俯视相机）
├── Directional Light（方向光）
├── BuildingRoot（建筑根节点，动态生成）
└── SimulationRoot（经营系统根节点，动态生成）
```

## 2. 核心组件

### 2.1 BuildingEntity
- 挂载在建筑 GameObject 上
- 负责加载 3D 建筑模型（通过 `GameModule.Resource.LoadGameObjectAsync`）
- 显示建筑状态（建造中/升级中/空闲）
- 处理建筑点击事件（选中/取消选中）

### 2.2 BuildingPlacementSystem
- 监听鼠标点击事件
- 检测点击位置是否在空地上
- 显示建筑摆放预览（半透明模型）
- 确认摆放后调用 `BuildingSystem.TryBuild`

### 2.3 BuildingSelectionUI
- 显示可建造的建筑列表
- 玩家选择建筑后进入摆放模式
- 显示建筑信息（名称、消耗、描述）

## 3. 3D 建筑 Prefab

使用简单的 3D 几何体作为占位模型：
- `Building_Workshop`：Cube（立方体）
- `Building_Farm`：Cylinder（圆柱体）
- `Building_TradePost`：Sphere（球体）
- `Building_Decor`：Capsule（胶囊体）

后续由美术替换为正式的 3D 模型。

## 4. 实现步骤

1. 创建 `SimulationScene` 场景（包含地面、相机、灯光）
2. 创建 `BuildingEntity` 组件
3. 创建 3D 建筑 Prefab（占位模型）
4. 创建 `BuildingSelectionUI`（建筑选择界面）
5. 实现 `BuildingPlacementSystem`（建筑摆放系统）
6. 修改 `BuildingSystem`，支持生成场景实体
7. 修改 `ProcedureSimulation`，加载 `SimulationScene`

## 5. 关键技术点

- **点击检测**：使用 `Physics.Raycast` 检测鼠标点击位置
- **地面标记**：使用 `LayerMask` 标记地面层，只在地面层检测点击
- **建筑预览**：使用半透明材质显示建筑摆放预览
- **模型加载**：使用 `GameModule.Resource.LoadGameObjectAsync` 异步加载 3D 模型
