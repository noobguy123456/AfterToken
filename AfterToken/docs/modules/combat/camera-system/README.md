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
- 狙击镜使用 RenderTexture 实现局部放大效果（2D 遗留）。

## 3D Duckov 式狙击镜（CameraSystem3D）

- 形态：跟鼠标的圆形镜窗（`SniperScopeUI`，Overlay Canvas），镜窗外全屏暗角半透明压暗（隐约可见）。
- 倍率：走 `TbWeapon.scopeFov`（0=无狙击镜；1004 狙击=37.5，即主相机 FOV 45 的 1.2 倍放大），运行时经 `WeaponInstance.ScopeFov` 读取，配件系统将来在该属性叠加修正。
- 开镜模式：Hold/Toggle 由设置面板开关决定（`SniperAimModeSetting`，仅狙击枪生效）。
- 开镜射击：直接命中镜窗中心——`WeaponSystem.IsScopedSniping` 为 true 时跳过辅助瞄准与扩散，`BallisticSystem` 不生成 tracer 飞行动画。
- 链路：`WeaponSystem.SetAimState` → `CameraSystem3D.SetScopeActive(bool, scopeFov)` → 懒创建 ScopeCamera（`CopyFrom` 主相机，1024² RT）→ `SniperScopeUI.m_raw_Scope` 显示。
- 镜窗中心 = 子弹落点：视轴对准 `PlayerEntity.AimPosition`。
- **定位坑**：ScopeCamera 位置不能用「瞄准点 + 主相机 offset」——主相机 offset(0,5,-3.5) + 俯角 60° 的视轴落点偏移约 0.61m，小 FOV 下瞄准点会出画。需沿当前俯仰/偏航的视线方向从瞄准点回推到主相机等高处：`t = _followOffset.y / -forward.y`，`pos = aimPoint - forward * t`。
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

