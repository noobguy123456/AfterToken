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
