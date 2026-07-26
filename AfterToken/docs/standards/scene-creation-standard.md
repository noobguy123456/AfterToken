# 游戏内场景创建规范

> 本文档定义了游戏内场景（战斗场景、经营场景等）的创建规范，确保场景结构一致、可维护。
> 技术栈：TEngine + HybridCLR + YooAsset + UniTask。

---

## 一、场景类型与创建方式

| 场景类型 | 创建方式 | 适用场景 | 示例 |
|----------|----------|----------|------|
| **场景文件** | 在 Unity 编辑器中设计 | 复杂场景、需要可视化编辑的场景 | `BattleScene_L01`、`LobbyScene` |
| **代码动态创建** | 通过代码动态创建 | 简单场景、快速验证的场景 | 经营场景（当前 MVP） |
| **混合方式** | 场景文件 + 代码动态创建 | 需要基础场景 + 动态内容的场景 | `SimulationScene` |

---

## 二、场景基本组件

### 2.1 战斗场景（BattleScene）

**必需组件**：

| 组件 | 说明 | 创建方式 |
|------|------|----------|
| `Global Light` | 全局光照（2D 场景使用） | 场景文件 |
| `Ground` | 地面，用于点击检测和导航 | 场景文件 |
| `PlayerSpawnPoint` | 玩家生成点 | 场景文件 |
| `Main Camera` | 主相机，带 `CameraSystem` | 场景文件 |
| `BattleRoot` | 战斗系统根节点 | 代码动态创建 |

**示例**：`BattleScene_L01.unity`

### 2.2 经营场景（SimulationScene）

**必需组件**：

| 组件 | 说明 | 创建方式 |
|------|------|----------|
| `Directional Light` | 方向光（3D 场景使用） | 代码动态创建 |
| `Ground` | 地面，用于点击检测 | 代码动态创建 |
| `Main Camera` | 主相机，带 `SimulationCameraController` | 场景文件 + 代码动态创建 |
| `VirtualPlayer` | 虚拟玩家，作为相机跟随目标 | 代码动态创建 |
| `SimulationRoot` | 经营系统根节点 | 代码动态创建 |
| `BuildingRoot` | 建筑根节点 | 代码动态创建 |

**示例**：`SimulationScene.unity`

---

## 三、场景创建流程

### 3.1 战斗场景创建流程

1. **创建场景文件**：在 Unity 编辑器中创建新场景，命名为 `BattleScene_LXX.unity`。
2. **添加基本组件**：添加 `Global Light`、`Ground`、`PlayerSpawnPoint`、`Main Camera`。
3. **配置相机**：在 `Main Camera` 上添加 `CameraSystem` 组件。
4. **保存场景**：保存场景文件到 `Assets/AssetRaw/Scenes/`。
5. **配置关卡**：在 `level.xlsx` 中配置关卡参数（场景名称、敌人数量、波次等）。

### 3.2 经营场景创建流程

1. **创建场景文件**：在 Unity 编辑器中创建新场景，命名为 `SimulationScene.unity`。
2. **添加基本组件**：添加 `Main Camera`（可选，代码动态创建时会检查）。
3. **保存场景**：保存场景文件到 `Assets/AssetRaw/Scenes/`。
4. **代码动态创建**：在 `ProcedureSimulation.InitializeSceneContent()` 中动态创建地面、光照、虚拟玩家等。

---

## 四、场景组件规范

### 4.1 地面（Ground）

**战斗场景**：
- 使用 `Plane` 或 `Quad` 作为地面
- 设置合适的材质和颜色
- 添加 `Collider` 用于点击检测和导航

**经营场景**：
- 使用 `Plane` 作为地面
- 设置合适的材质和颜色（如绿色）
- 添加 `Collider` 用于点击检测

### 4.2 光照（Light）

**战斗场景**：
- 使用 `Global Light`（2D 场景）
- 设置合适的颜色和强度

**经营场景**：
- 使用 `Directional Light`（3D 场景）
- 设置合适的颜色、强度和阴影
- 调整旋转角度（如 `Quaternion.Euler(50, -30, 0)`）

### 4.3 相机（Main Camera）

**战斗场景**：
- 使用 `CameraSystem` 组件
- 支持跟随玩家、FOV、震动、狙击镜
- 通过 `TbCamera` 配置参数

**经营场景**：
- 使用 `SimulationCameraController` 组件
- 支持跟随目标、WASD 移动、鼠标拖动、滚轮缩放
- 通过代码设置初始位置和角度

### 4.4 玩家生成点（PlayerSpawnPoint）

**战斗场景**：
- 使用空 GameObject 作为玩家生成点
- 玩家在该位置生成

**经营场景**：
- 使用虚拟玩家（`VirtualPlayer`）作为相机跟随目标
- 虚拟玩家是透明胶囊体，不参与游戏逻辑

---

## 五、场景切换流程

### 5.1 战斗场景切换

```
ProcedureMainMenu → ProcedureLobby → ProcedureBattle
    ↓
LoadSceneWithLoadingAsync("BattleScene_LXX")
    ↓
InitializeBattleSystems()（创建 BattleRoot、添加战斗系统）
    ↓
ShowUIAsyncAwait<BattleMainUI>()
```

### 5.2 经营场景切换

```
ProcedureMainMenu → ProcedureLobby → ProcedureSimulation
    ↓
LoadSceneWithLoadingAsync("SimulationScene")
    ↓
InitializeSceneContent()（创建地面、光照、虚拟玩家）
InitializeSimulationSystems()（创建 SimulationRoot、添加经营系统）
    ↓
ShowUIAsyncAwait<SimulationMainUI>()
```

---

## 六、场景清理流程

### 6.1 战斗场景清理

```
ProcedureBattle.OnLeave()
    ↓
CleanupBattleSystems()（销毁 BattleRoot、CameraSystem）
    ↓
GameModule.UI.CloseAll()
GameModule.Timer.RemoveAllTimer()
GameModule.Resource.UnloadUnusedAssets()
```

### 6.2 经营场景清理

```
ProcedureSimulation.OnLeave()
    ↓
CleanupSimulationSystems()（销毁 VirtualPlayer、SimulationRoot）
    ↓
GameModule.UI.CloseAll()
GameModule.Timer.RemoveAllTimer()
GameModule.Resource.UnloadUnusedAssets()
```

---

## 七、常见问题与解决方案

### 7.1 相机无法移动

**问题**：`SimulationCameraController` 无法接收输入。

**原因**：
- `SimulationMainUI` 的 `GraphicRaycaster` 拦截了鼠标事件
- `CameraSystem` 仍然存在于 Main Camera 上，覆盖了 `SimulationCameraController` 的修改

**解决方案**：
- 禁用 `GraphicRaycaster` 的 `Block Raycasts`
- 在进入经营场景时销毁 `CameraSystem`

### 7.2 场景缺少基本组件

**问题**：`SimulationScene` 缺少地面、光照等基本组件。

**原因**：场景文件只有一个 `Main Camera`，没有其他组件。

**解决方案**：
- 通过代码动态创建地面、光照等组件
- 或者在 Unity 编辑器中手动添加组件到场景文件

### 7.3 输入系统无法工作

**问题**：Esc 键无法调出菜单。

**原因**：`InputSystem` 只在战斗场景中创建，在经营场景中不存在。

**解决方案**：
- 创建专门用于经营场景的输入系统（`SimulationInputSystem`）
- 只处理 Esc 键和相机控制输入

---

## 八、后续建议

1. **场景文件化**：将 `SimulationScene` 的基本组件（地面、光照、虚拟玩家）移到场景文件中，减少代码动态创建。
2. **场景模板**：创建场景模板，包含基本组件，方便快速创建新场景。
3. **场景预览**：在编辑器中预览场景结构，确保组件齐全。
4. **场景验证**：在 Play Mode 中验证场景组件是否正常工作。

---

## 九、总结

游戏内场景创建需要遵循以下规范：

1. **场景基本组件**：确保场景包含必需的组件（地面、光照、相机等）。
2. **创建方式**：根据场景复杂度选择场景文件或代码动态创建。
3. **相机控制**：确保相机控制器能正常接收输入，避免被 UI 拦截。
4. **输入系统**：创建专门用于场景类型的输入系统，处理特定的输入。
5. **清理流程**：确保场景切换时正确清理资源，避免内存泄漏。

通过遵循这些规范，可以确保场景结构一致、可维护，避免出现相机无法移动、输入无法工作等问题。
