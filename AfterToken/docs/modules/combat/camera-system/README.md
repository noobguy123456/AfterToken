# 相机系统

## 职责

管理战斗场景中的相机行为，包括跟随玩家、边界限制、抖动等效果。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `CameraSystem` | `Assets/GameScripts/HotFix/GameLogic/System/CameraSystem.cs` | 相机控制 |

## 设计要点

- 相机在 `LateUpdate` 中直接读取 `PlayerSystem.Instance.GetPlayerEntity().transform.position` 进行跟随。
- 玩家 `Rigidbody2D` 启用 `Interpolate`，确保 `transform.position` 在渲染帧经过插值。
- 支持三种跟随模式：
  - `Hard`：相机位置与玩家位置完全同步，零延迟，默认模式。
  - `Exponential`：指数平滑，无最大速度上限，适合希望轻微平滑感的场景。
  - `SmoothDamp`：传统 SmoothDamp，有明显平滑滞后感。
- 支持 `_lookAheadFactor` 根据玩家速度加入提前量。
- 狙击镜使用 RenderTexture 实现局部放大效果。
- 受击/开火相机抖动已接入 `ICameraEvent.OnCameraShake`。
- 边界限制待根据实际关卡尺寸接入。

## 后坐力相机抖动

开火时根据武器 `recoilIntensity` 触发相机抖动，模拟真实射击反馈。

### 触发流程

1. `WeaponSystem.TryFire()` 中调用 `ICameraEvent.OnCameraShake(magnitude, duration)`。
2. `CameraSystem.OnCameraShake()` 接收并设置当前震动幅度与持续时间。
3. `CameraSystem.LateUpdate()` 中根据 `_shakeMagnitude` 计算偏移并叠加到相机位置。

### 参数计算

```csharp
float recoil = weapon.Config.recoilIntensity > 0f ? weapon.Config.recoilIntensity : 2f;
float shakeMag = recoil * 0.25f;
GameEvent.Get<ICameraEvent>()?.OnCameraShake(shakeMag, 0.1f);
```

- 未配置 `recoilIntensity` 的武器会获得基础抖动（`recoil = 2f`）。
- 抖动幅度随武器后坐力强度线性增长。
- 持续时间为 0.1 秒，比早期 0.05 秒更易感知。

### 抖动表现

- **方向**：以上跳（Y 轴正方向）为主，配合少量横向随机，模拟枪口上跳。
- **衰减**：使用指数衰减（`Mathf.Lerp`），比线性衰减更自然。
- **阻尼**：默认 `_shakeDamping = 2.5f`，抖动消散更柔和。

### Inspector 可调参数

| 字段 | 路径 | 说明 |
|---|---|---|
| `_shakeDamping` | `CameraSystem` 脚本 Inspector | 震动衰减速度，越小抖动持续越久 |

