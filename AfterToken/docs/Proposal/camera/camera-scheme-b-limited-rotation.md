# 方案 B：有限旋转摄像机方案（Hades 风格）

> 本文档定义了2D人物 + 3D场景的固定俯视角 TPS 游戏的摄像机系统设计方案，参考 Hades 的风格。
> 技术栈：TEngine + HybridCLR + YooAsset + UniTask + Luban。

---

## 一、方案概述

针对2D人物 + 3D场景的固定俯视角 TPS 游戏，采用 **固定俯视角 + 有限旋转 + 平滑跟随** 的摄像机方案，参考 Hades 的风格。

### 核心原则

1. **固定俯视角**：相机默认固定在俯视角度（60°），不能自由旋转。
2. **有限旋转**：相机可以有限旋转（左右30°），提供更好的瞄准体验。
3. **平滑跟随**：相机平滑跟随目标（玩家或虚拟玩家）。
4. **手动控制**：玩家可以通过 WASD/鼠标拖动临时取消跟随，手动控制相机。
5. **2D人物渲染**：2D人物使用 Billboard 渲染，始终面向相机。

---

## 二、摄像机参数

| 参数 | 值 | 说明 |
|------|-----|------|
| **俯视角度** | 60° | 相机默认俯视角度（可调） |
| **初始高度** | 15m | 相机初始高度 15m |
| **初始距离** | -10m | 相机初始距离 -10m |
| **FOV** | 45° | 相机视场角 45° |
| **最小缩放** | 5m | 相机最小高度 5m |
| **最大缩放** | 30m | 相机最大高度 30m |
| **移动速度** | 20m/s | 相机移动速度 20m/s |
| **缩放速度** | 5m/s | 相机缩放速度 5m/s |
| **旋转角度** | 30° | 相机左右旋转角度 30° |
| **旋转速度** | 90°/s | 相机旋转速度 90°/s |
| **跟随平滑时间** | 0.08s | 相机跟随平滑时间 0.08s |

---

## 三、摄像机控制

### 3.1 跟随模式

**默认模式**：相机平滑跟随目标（玩家或虚拟玩家）。

```csharp
// CameraSystem3D.UpdateCameraPosition()
private void UpdateCameraPosition()
{
    if (_isFollowing && _followTarget != null)
    {
        // 计算目标位置（跟随目标位置 + 偏移）
        Vector3 targetPos = _followTarget.position + _followOffset;
        
        // 应用旋转
        targetPos = ApplyRotation(targetPos);
        
        // 平滑跟随
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
        
        // 应用俯视角度
        transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
    }
}
```

### 3.2 手动控制

**触发条件**：玩家按下 WASD 键或鼠标右键拖动。

**效果**：相机取消跟随，改为手动控制。

```csharp
// CameraSystem3D.HandleKeyboardInput()
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
// CameraSystem3D.HandleMouseInput()
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

### 3.4 旋转控制

**触发条件**：玩家按住鼠标中键或按住 Q/E 键。

**效果**：相机左右旋转，提供更好的瞄准体验。

```csharp
// CameraSystem3D.HandleRotationInput()
private void HandleRotationInput()
{
    // 鼠标中键旋转
    if (Input.GetMouseButton(2))
    {
        float mouseX = Input.GetAxis("Mouse X");
        _yawAngle += mouseX * _rotationSpeed * Time.deltaTime;
        _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
    }
    
    // Q/E 键旋转
    if (Input.GetKey(KeyCode.Q))
    {
        _yawAngle -= _rotationSpeed * Time.deltaTime;
        _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
    }
    if (Input.GetKey(KeyCode.E))
    {
        _yawAngle += _rotationSpeed * Time.deltaTime;
        _yawAngle = Mathf.Clamp(_yawAngle, -_maxRotationAngle, _maxRotationAngle);
    }
}
```

---

## 四、2D人物渲染

### 4.1 Billboard 渲染

**原理**：2D人物使用 Billboard 渲染，始终面向相机。

**实现**：

```csharp
// BillboardRenderer.cs
public class BillboardRenderer : MonoBehaviour
{
    private Camera _mainCamera;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // 使2D人物始终面向相机
            transform.LookAt(_mainCamera.transform);
            transform.Rotate(0f, 180f, 0f); // 翻转，使文本正面朝向相机
        }
    }
}
```

### 4.2 2D人物与3D场景融合

**原理**：2D人物在3D场景中移动，与3D场景保持一定的视觉融合。

**实现**：
- 2D人物使用 SpriteRenderer 渲染
- 2D人物的位置和旋转与3D场景同步
- 2D人物的缩放与3D场景保持一定的比例

---

## 五、摄像机系统架构

### 5.1 核心组件

| 组件 | 说明 |
|------|------|
| **CameraSystem3D** | 3D摄像机系统，负责跟随、旋转、缩放、手动控制 |
| **BillboardRenderer** | 2D人物 Billboard 渲染，负责使2D人物始终面向相机 |
| **CameraConfig** | 摄像机配置，负责管理摄像机参数 |

### 5.2 系统交互

```
用户输入（WASD/鼠标/滚轮/QE键）
    ↓
CameraSystem3D.HandleKeyboardInput / HandleMouseInput / HandleRotationInput
    ↓
更新 _isFollowing / _currentZoom / _yawAngle
    ↓
CameraSystem3D.UpdateCameraPosition()
    ↓
计算目标位置（跟随目标位置 + 偏移）
    ↓
应用旋转
    ↓
平滑跟随
    ↓
应用俯视角度
    ↓
相机移动/旋转/缩放
```

---

## 六、实现细节

### 6.1 相机控制器

**文件**：`Assets/GameScripts/HotFix/GameLogic/System/CameraSystem3D.cs`

**核心功能**：
- 平滑跟随目标
- WASD/鼠标拖动手动控制
- 鼠标滚轮缩放
- 鼠标中键/QE键旋转

**关键代码**：

```csharp
public class CameraSystem3D : MonoBehaviour
{
    public static CameraSystem3D Instance { get; private set; }
    
    [Header("跟随")]
    [SerializeField] private Transform _followTarget;
    [SerializeField] private Vector3 _followOffset = new Vector3(0f, 15f, -10f);
    [SerializeField] private bool _isFollowing = true;
    
    [Header("缩放")]
    [SerializeField] private float _currentZoom = 15f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 30f;
    [SerializeField] private float _zoomSpeed = 5f;
    
    [Header("旋转")]
    [SerializeField] private float _yawAngle = 0f;
    [SerializeField] private float _maxRotationAngle = 30f;
    [SerializeField] private float _rotationSpeed = 90f;
    
    [Header("俯视角度")]
    [SerializeField] private float _pitchAngle = 60f;
    
    [Header("移动")]
    [SerializeField] private float _moveSpeed = 20f;
    
    private Camera _mainCamera;
    
    private void Awake()
    {
        Instance = this;
        _mainCamera = GetComponent<Camera>();
    }
    
    private void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        HandleRotationInput();
        UpdateCameraPosition();
    }
    
    private void UpdateCameraPosition()
    {
        if (_isFollowing && _followTarget != null)
        {
            // 计算目标位置（跟随目标位置 + 偏移）
            Vector3 targetPos = _followTarget.position + _followOffset;
            
            // 应用旋转
            targetPos = ApplyRotation(targetPos);
            
            // 平滑跟随
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 5f);
            
            // 应用俯视角度
            transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        }
        else
        {
            // 手动控制时，确保 Y 坐标为缩放值
            Vector3 position = transform.position;
            position.y = _currentZoom;
            transform.position = position;
            
            // 应用俯视角度
            transform.rotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
        }
    }
    
    private Vector3 ApplyRotation(Vector3 position)
    {
        // 绕 Y 轴旋转
        Quaternion rotation = Quaternion.Euler(0f, _yawAngle, 0f);
        return rotation * position;
    }
    
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        _isFollowing = true;
    }
}
```

### 6.2 2D人物 Billboard 渲染

**文件**：`Assets/GameScripts/HotFix/GameLogic/Component/BillboardRenderer.cs`

**核心功能**：
- 使2D人物始终面向相机

**关键代码**：

```csharp
public class BillboardRenderer : MonoBehaviour
{
    private Camera _mainCamera;
    
    private void Awake()
    {
        _mainCamera = Camera.main;
    }
    
    private void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // 使2D人物始终面向相机
            transform.LookAt(_mainCamera.transform);
            transform.Rotate(0f, 180f, 0f); // 翻转，使文本正面朝向相机
        }
    }
}
```

### 6.3 摄像机配置

**文件**：`Assets/GameScripts/HotFix/GameLogic/Config/CameraConfig.cs`

**核心功能**：
- 管理摄像机参数

**关键代码**：

```csharp
public class CameraConfig
{
    public float pitchAngle = 60f;
    public float initialHeight = 15f;
    public float initialDistance = -10f;
    public float fov = 45f;
    public float minZoom = 5f;
    public float maxZoom = 30f;
    public float moveSpeed = 20f;
    public float zoomSpeed = 5f;
    public float maxRotationAngle = 30f;
    public float rotationSpeed = 90f;
    public float followSmoothTime = 0.08f;
}
```

---

## 七、配置表

### 7.1 摄像机配置表（TbCamera3D）

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | `int` | 配置 ID |
| `pitchAngle` | `float` | 俯视角度（默认 60°） |
| `initialHeight` | `float` | 初始高度（默认 15m） |
| `initialDistance` | `float` | 初始距离（默认 -10m） |
| `fov` | `float` | 视场角（默认 45°） |
| `minZoom` | `float` | 最小缩放（默认 5m） |
| `maxZoom` | `float` | 最大缩放（默认 30m） |
| `moveSpeed` | `float` | 移动速度（默认 20m/s） |
| `zoomSpeed` | `float` | 缩放速度（默认 5m/s） |
| `maxRotationAngle` | `float` | 最大旋转角度（默认 30°） |
| `rotationSpeed` | `float` | 旋转速度（默认 90°/s） |
| `followSmoothTime` | `float` | 跟随平滑时间（默认 0.08s） |

---

## 八、与现有系统的集成

### 8.1 战斗场景

**当前实现**：使用 `CameraSystem` 跟随玩家，支持 FOV、震动、狙击镜。

**调整方案**：
- 使用 `CameraSystem3D` 替代 `CameraSystem`
- 保持 FOV、震动、狙击镜等功能
- 添加有限旋转、缩放等功能

### 8.2 经营场景

**当前实现**：使用 `SimulationCameraController` 跟随虚拟玩家，支持 WASD/鼠标拖动、滚轮缩放。

**调整方案**：
- 使用 `CameraSystem3D` 替代 `SimulationCameraController`
- 保持 WASD/鼠标拖动、滚轮缩放等功能
- 添加有限旋转等功能

---

## 九、后续建议

1. **摄像机参数配置化**：将摄像机参数配置到 Luban 配置表中，便于调整。
2. **摄像机效果优化**：添加摄像机震动、狙击镜、伤害指示器等效果。
3. **2D人物渲染优化**：优化2D人物的 Billboard 渲染，确保与3D场景融合。
4. **摄像机碰撞处理**：添加摄像机碰撞处理，避免相机穿过障碍物。

---

## 十、总结

针对2D人物 + 3D场景的固定俯视角 TPS 游戏，采用 **固定俯视角 + 有限旋转 + 平滑跟随** 的摄像机方案，参考 Hades 的风格。

**核心优势**：
1. **兼顾全局视野和沉浸感**：默认俯视角度，玩家能看到全局战场；有限旋转，玩家能更好地瞄准。
2. **适合2D人物+3D场景**：2D人物使用 Billboard 渲染，始终面向相机；3D场景保持3D渲染，体现3D优势。
3. **符合 Hades 的风格**：Hades 是一款成功的俯视角动作游戏，其摄像机方案已被验证。

**适用场景**：
- 2D人物 + 3D场景的固定俯视角 TPS 游戏
- 需要瞄准的TPS游戏
- 需要兼顾全局视野和沉浸感的游戏
