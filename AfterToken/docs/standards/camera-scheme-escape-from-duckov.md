# Escape from Duckov 摄像机方案

> 本文档定义了固定俯视角 3D 游戏的摄像机方案，参考 Escape from Duckov 的风格。
> 技术栈：TEngine + HybridCLR + YooAsset + UniTask。

---

## 一、方案概述

针对固定俯视角 3D 游戏，采用 **固定俯视角 + 有限缩放 + 平滑跟随** 的摄像机方案。

### 核心原则

1. **固定俯视角**：相机固定在俯视角度（60°），不能旋转
2. **有限缩放**：相机可以缩放（5m ~ 30m），但不能旋转
3. **平滑跟随**：相机平滑跟随目标（玩家或虚拟玩家）
4. **手动控制**：玩家可以通过 WASD/鼠标拖动临时取消跟随，手动控制相机

### 玩法平面约定（XZ）

- **玩法层（移动/瞄准/弹道/导航/边界）统一在世界 XZ 平面运算**，实体贴地位于 y=0，弹道与视线检测高度为 0.5f。
- 玩法层中所有 `Vector2` 的语义是 **世界 (x, z)**；与 `Vector3` 互转必须走 `GameLogic.XZConvert`（`Vector3.ToXZ()` / `Vector2.ToWorld(y)`），禁止手写 `(Vector2)transform.position` 之类取 x/y 的转换。
- 物理组件一律 3D：Rigidbody（useGravity=false，constraints=FreezePositionY|FreezeRotationX|FreezeRotationZ）+ CapsuleCollider/SphereCollider，不再使用任何 2D 物理组件。
- 屏幕坐标转瞄准点：`Camera.ScreenPointToRay` 与 `Plane(Vector3.up, Vector3.zero)` 求交，取交点的 (x, z)。

---

## 二、摄像机参数

| 参数 | 值 | 说明 |
|------|-----|------|
| **俯视角度** | 60° | 相机固定在俯视 60° |
| **初始高度** | 5m | 相机初始高度 5m（角色约占屏幕高度 1/5） |
| **初始距离** | -3.5m | 相机初始距离 -3.5m |
| **FOV** | 45° | 相机视场角 45° |
| **最小缩放** | 5m | 相机最小高度 5m |
| **最大缩放** | 30m | 相机最大高度 30m |
| **移动速度** | 20m/s | 相机移动速度 20m/s |
| **缩放速度** | 5m/s | 相机缩放速度 5m/s |
| **跟随平滑时间** | 0.08s | 相机跟随平滑时间 0.08s |

---

## 三、摄像机控制

### 3.1 跟随模式

**默认模式**：相机平滑跟随目标（玩家或虚拟玩家）。

```csharp
// SimulationCameraController.UpdateCameraPosition()
private void UpdateCameraPosition()
{
    if (_isFollowing && _followTarget != null)
    {
        // 跟随目标
        Vector3 targetPos = _followTarget.position + _followOffset;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
    }
}
```

### 3.2 手动控制

**触发条件**：玩家按下 WASD 键或鼠标右键拖动。

**效果**：相机取消跟随，改为手动控制。

```csharp
// SimulationCameraController.HandleKeyboardInput()
private void HandleKeyboardInput()
{
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");

    if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
    {
        // 键盘输入时取消跟随，改为手动控制
        _isFollowing = false;
        
        Vector3 movement = new Vector3(horizontal, 0f, vertical) * _moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
}
```

### 3.3 缩放控制

**触发条件**：玩家滚动鼠标滚轮。

**效果**：相机高度变化，俯视角度不变。

```csharp
// SimulationCameraController.HandleMouseInput()
private void HandleMouseInput()
{
    float scroll = Input.GetAxis("Mouse ScrollWheel");
    if (Mathf.Abs(scroll) > 0.01f)
    {
        _currentZoom -= scroll * _zoomSpeed;
        _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
        _followOffset.y = _currentZoom;
    }
}
```

---

## 四、摄像机方案对比

| 特性 | Escape from Duckov | 我们的方案 |
|------|-------------------|------------|
| **视角** | 固定俯视角（60°） | 固定俯视角（60°） |
| **缩放** | 有限缩放（5m ~ 30m） | 有限缩放（5m ~ 30m） |
| **跟随** | 平滑跟随玩家 | 平滑跟随目标（玩家或虚拟玩家） |
| **手动控制** | WASD/鼠标拖动 | WASD/鼠标拖动 |
| **FOV** | 45° | 45° |

---

## 五、实现细节

### 5.1 相机控制器

**文件**：`Assets/GameScripts/HotFix/GameLogic/Simulation/SimulationCameraController.cs`

**核心功能**：
- 平滑跟随目标
- WASD/鼠标拖动手动控制
- 鼠标滚轮缩放

**关键代码**：

```csharp
public class SimulationCameraController : MonoBehaviour
{
    private Transform _followTarget;
    private Vector3 _followOffset = new Vector3(0f, 15f, -10f);
    private float _currentZoom = 15f;
    private bool _isFollowing = true;

    private void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        if (_isFollowing && _followTarget != null)
        {
            Vector3 targetPos = _followTarget.position + _followOffset;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        }
        else
        {
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

### 5.2 场景设置

**战斗场景**：
- 相机跟随玩家（Player）
- 玩家移动时，相机平滑跟随
- 玩家可以通过 WASD/鼠标拖动临时取消跟随

**经营场景**：
- 相机跟随虚拟玩家（VirtualPlayer）
- 虚拟玩家不移动，相机固定在初始位置
- 玩家可以通过 WASD/鼠标拖动临时取消跟随

---

## 六、现有计划调整

### 6.1 战斗场景摄像机方案

**当前实现**：使用 `CameraSystem` 跟随玩家，支持 FOV、震动、狙击镜。

**调整方案**：
- 保持 `CameraSystem` 的核心功能
- 添加固定俯视角（60°）和有限缩放（5m ~ 30m）
- 添加 WASD/鼠标拖动手动控制

### 6.2 经营场景摄像机方案

**当前实现**：使用 `SimulationCameraController` 跟随虚拟玩家，支持 WASD/鼠标拖动、滚轮缩放。

**调整方案**：
- 保持 `SimulationCameraController` 的核心功能
- 确保固定俯视角（60°）和有限缩放（5m ~ 30m）
- 优化手动控制和跟随切换

---

## 七、新建战斗场景

### 7.1 场景列表

| 场景名称 | 说明 | 状态 |
|----------|------|------|
| `BattleScene_3D_L01` | 俯视角 3D 战斗场景 1 | ✅ 已创建 |
| `BattleScene_3D_L02` | 俯视角 3D 战斗场景 2 | ✅ 已创建 |
| `BattleScene_3D_L03` | 俯视角 3D 战斗场景 3 | ✅ 已创建 |

### 7.2 场景内容

**基本组件**：
- `Ground`（地面，Plane，用于点击检测和导航）
- `PlayerSpawnPoint`（玩家生成点）
- `Main Camera`（主相机，带 `CameraSystem`）

**相机设置**：
- 位置：`(0, 15, -10)`
- 旋转：`(60, 0, 0)`（俯视 60°）
- FOV：45°
- 正交：false（透视相机）

---

## 八、后续建议

1. **场景文件化**：将新建的战斗场景的基本组件移到场景文件中，减少代码动态创建。
2. **摄像机方案统一**：将 Escape from Duckov 的摄像机方案应用到所有场景（战斗、经营）。
3. **摄像机参数配置化**：将摄像机参数（俯视角度、缩放范围、移动速度等）配置到 Luban 配置表中。
4. **摄像机效果优化**：添加摄像机震动、狙击镜、伤害指示器等效果。

---

## 九、总结

针对固定俯视角 3D 游戏，采用 **固定俯视角 + 有限缩放 + 平滑跟随** 的摄像机方案。

**核心优势**：
1. **符合参考游戏风格**：与 Escape from Duckov 一致
2. **兼顾跟随和手动控制**：平滑跟随目标，同时支持手动控制
3. **参数可配置**：摄像机参数可以通过配置表调整

**适用场景**：
- 固定俯视角 3D 游戏
- 3D 场景 + 3D 模型
- 需要平滑跟随和手动控制的游戏
