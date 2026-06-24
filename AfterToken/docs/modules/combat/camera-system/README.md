# 相机系统

## 职责

管理战斗场景中的相机行为，包括跟随玩家、边界限制、抖动等效果。

## 核心类与文件

| 类/文件 | 路径 | 说明 |
|---|---|---|
| `CameraSystem` | `Assets/GameScripts/HotFix/GameLogic/System/CameraSystem.cs` | 相机控制 |

## 设计要点

- 相机跟随玩家位置，限制在关卡边界内。
- 狙击镜使用 RenderTexture 实现局部放大效果。
- 待补充相机抖动（受击/开火）。
