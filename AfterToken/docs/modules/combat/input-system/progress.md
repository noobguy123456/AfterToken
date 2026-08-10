# Input System 进度

## 已完成
- [x] 移动、瞄准、开火、换弹、切枪输入事件
- [x] **移动改相机系相对（2026-08-06）**：相机偏航（中键拖拽）后 WASD 仍对齐屏幕方向。原实现输入向量直接当世界系 XZ 用，偏航 ≠0 时 W 在屏幕上呈斜向。修复：`InputSystem.ToCameraSpace` 用主相机 forward/right 的 XZ 投影旋转输入向量（yaw=0 时与原行为一致；`_mainCamera` 懒获取兜底）。实测 yaw=30° 时 W→(0.50,0.87) 与相机前向一致
- [x] IBattleInputEvent 接口定义与发送
- [x] 瞄准迁移 XZ 平面：相机射线与 y=0 平面求交发 (x, z)；修复锁定光标下滚轮恒选 slot 0（改用 CrosshairUpdater 位置计算）

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`