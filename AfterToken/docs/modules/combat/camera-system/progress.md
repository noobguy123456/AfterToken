# Camera System 进度

## 已完成
- [x] 相机平滑跟随（`LateUpdate` 直接读取玩家 `Transform` + `Rigidbody2D.Interpolate` 插值 + 三种跟随模式：Hard/Exponential/SmoothDamp）
- [x] 边界限制（基础框架）
- [x] FOV / OrthographicSize 动态调整
- [x] 狙击镜 `RenderTexture` 基础集成
- [x] 受击方向指示数据计算
- [x] 受击/开火相机抖动基础接入
- [x] 开火后坐力相机抖动：根据武器 `recoilIntensity` 计算幅度，以上跳为主，指数衰减
- [x] `CameraSystem3D`（3D 俯视角）：修复 `ApplyRotation` 偏航偏移未旋转导致的跟随失真；删除滚轮缩放与 Q/E 旋转（与切武器/交互键冲突，仅留鼠标中键拖拽偏航）；接线 `Camera3DConfigMgr`（俯角/高度/距离/FOV/平滑时间等）
- [x] 玩法层迁移 XZ 平面后，旧 2D `CameraSystem` 标记为遗留不再维护（其中残留一处 `Rigidbody2D` 引用属计划内）
- [x] 2026-08-05 移动抖动治理（两段）：
  1. 移除每 15 帧爆发的 `[hb]` 心跳日志（编辑器 Console 写入造成周期性掉帧）；
  2. **根因：帧生产差拍**。原 `targetFrameRate=120`（RootModule 序列化值）与 144Hz 显示器非整数倍错配，限帧器睡眠粒度粗导致帧时间 3ms/15ms 交替（实测），呈现节奏不均——静止时不可见，移动时变成可见顿挫，且平滑跟随的相机把世界平移也染上同样节奏（"摄像机没跟上"的错觉来源）。修复：`CameraSystem3D` 改**硬跟随**（玩家与屏幕像素级锁定，删除 `_followSmoothTime` 与阻尼代码；`TbCamera3D.FollowSmoothTime` 字段遗留未用）；`GameEntry.Start` 按 `Screen.currentResolution` 动态设置 `targetFrameRate`（覆盖 RootModule 的 120）。用户实测确认不抖。教训：位置时间戳正确 ≠ 呈现平滑，帧生产节奏必须与显示刷新率整数倍对齐

## 进行中
- [ ] 狙击镜 RenderTexture 集成优化

## 待办
- [ ] 死亡/胜利镜头表现
- [ ] 多分辨率适配细节
- [ ] 根据实际关卡尺寸实现边界限制

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
