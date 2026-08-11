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

- **当前形态（2026-08-08 三版起，纯视觉镜窗）**：开镜 = 全屏均匀灰色蒙版（不遮挡场景信息，alpha 0.3）+ 跟随准星的狙击镜图案（圆环 + 贯径十字线，直径约 0.59 屏高），**默认无放大、无镜头畸变**。镜相机/RenderTexture 只在配置要求放大时才启用。
- 倍率：走 `TbWeapon.scopeFov`——**0 = 无放大（纯视觉镜窗，当前 1004 狙击的默认值）**；>0 = 启用镜相机的放大镜模式（镜内 FOV，越小倍率越高）。运行时经 `WeaponInstance.ScopeFov` 读取（该属性直接透传配置值，不再兜底 15），配件系统将来在该属性叠加修正。
- 开镜模式：Hold/Toggle 由设置面板开关决定（`SniperAimModeSetting`，仅狙击枪生效）。
- 开镜射击：子弹从镜窗中心射出直接命中——`WeaponSystem.IsScopedSniping`（= 开镜 && 当前武器为狙击枪，不再要求 scopeFov>0）为 true 时跳过辅助瞄准与扩散，`BallisticSystem` 不生成 tracer 飞行动画。
- 链路：`WeaponSystem.SetAimState` →（仅 `ScopeFov > 0` 时）`CameraSystem3D.SetScopeActive(bool, scopeFov)` 懒创建 ScopeCamera（`CopyFrom` 主相机，1024² RT）→ `SniperScopeUI.m_raw_Scope` 显示；`ScopeFov == 0` 时 `m_raw_Scope` 自动隐藏（`RefreshScopeTexture` 按纹理是否存在开关）。
- 镜窗中心 = 子弹落点：镜窗跟随准星，瞄准射线始终来自主相机过准星（`InputSystem.HandleAimInput`），落点即 `PlayerEntity.AimPosition`。
- **放大模式的相机模型（视场放大镜）**：ScopeCamera 与主相机同位、只旋转 `LookRotation` 对准瞄准点——镜窗 = 主相机视野的放大裁剪，镜内外同视点无视差，活动范围 = 主相机渲染区域。
- 镜窗跟随：`SniperScopeUI.OnUpdate` 跟随 `CrosshairUpdater.CurrentScreenPos`（而非原始 `Input.mousePosition`），保证镜窗/准星/子弹落点一致；开镜灵敏度走 `SensitivitySetting.ScopedValue`。
- 切换武器自动关镜：`WeaponSystem.SwitchToSlot` 在换槽**之前** `SetAimState(false)`（之后调用的话 `CurrentWeapon` 已是新武器，狙击镜分支会被跳过导致卡镜）。
- 受击/开火相机抖动已接入 `ICameraEvent.OnCameraShake`。
- 边界限制待根据实际关卡尺寸接入。

### 狙击镜演进留档（已废弃方案）

- ~~抬高镜相机 + 压边平移~~（2026-08-08 二版）：ScopeCamera 架在瞄准点上空 12m、瞄准射线改从镜相机发出，准星压边驱动瞄准点限速平移（8m/s），射程可超出主相机视锥。实测可行但用户拍板不符合鸭科夫参考，回退。教训留档：①"相机跟随瞄准点 + 瞄准来自相机"是反馈环，镜相机旋转必须固定——LookRotation 跟踪会在平滑滞后期把视轴拉平趋向水平，瞄准距离正反馈发散（实测冲到 80 万米）；②平移必须限速，否则速度由 SmoothDamp 动态决定同样失控。
- ~~视场放大镜一版~~（2026-08-08 一版）：与三版放大模式同型，当时 scopeFov 37.5（1.2x）。
- ~~定位坑~~（更早的位移模型留档）：旧 ScopeCamera 位置不能用「瞄准点 + 主相机 offset」，需沿视线回推等高处。

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

