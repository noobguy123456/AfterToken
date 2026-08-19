# Input System 进度

## 已完成
- [x] 移动、瞄准、开火、换弹、切枪输入事件
- [x] **移动改相机系相对（2026-08-06）**：相机偏航（中键拖拽）后 WASD 仍对齐屏幕方向。原实现输入向量直接当世界系 XZ 用，偏航 ≠0 时 W 在屏幕上呈斜向。修复：`InputSystem.ToCameraSpace` 用主相机 forward/right 的 XZ 投影旋转输入向量（yaw=0 时与原行为一致；`_mainCamera` 懒获取兜底）。实测 yaw=30° 时 W→(0.50,0.87) 与相机前向一致
- [x] IBattleInputEvent 接口定义与发送
- [x] 瞄准迁移 XZ 平面：相机射线与 y=0 平面求交发 (x, z)；修复锁定光标下滚轮恒选 slot 0（改用 CrosshairUpdater 位置计算）
- [x] **菜单 UI 打开时屏蔽射击/瞄准（2026-08-19）**：修复开箱（LootContainerUI）状态下左键点击仍会开枪。`IsMenuUIOpen()`（BattleBagUI/LootContainerUI/NoteUI 任一打开）期间跳过 `HandleAimInput`/`HandleFireInput`/`HandleAimButtonInput`；UI 打开瞬间补发 `OnFireReleased`/`OnAimReleased`，防按住开火键开 UI 后武器卡按下状态。移动/换弹/切枪不受影响（开箱可移动属搜打撤风险设计）。实测：NoteUI 打开期间拦截门=true、E 关闭后恢复=false
- [x] **菜单 UI 打开时冻结准星（2026-08-19）**：修复看纸条时鼠标移动仍在后台驱动隐藏准星、关掉后瞄准点被"强制挪动"。`CrosshairUpdater.Update` 在 `CursorManager.IsCursorVisible`（系统光标可见=菜单类 UI 打开）时直接 return，准星位置不再累加鼠标位移；实测开/关纸条准星位置不变、光标隐藏锁定恢复
- [x] **菜单 UI 打开时冻结角色朝向（2026-08-19）**：修复看纸条时"瞄点还是会变"——瞄准输入被屏蔽后 AimPosition 冻结，但 `PlayerEntity.Update` 每帧仍朝旧瞄点旋转，玩家移动时角色原地自转。修复：`PlayerEntity.Update` 在 `InputSystem.IsMenuUIOpen()`（改 public static）时直接 return，朝向完全冻结
- [x] **战斗光标泄漏兜底（2026-08-19）**：修复关闭纸条后 Windows 系统鼠标仍显示（ShowCursor/HideCursor 引用计数泄漏，实测关闭后 refCount=1）。`CrosshairUpdater.Update`（战斗常驻 HUD 组件）新增兜底：无菜单类 UI（含 WeaponWheelUI）且 `CursorManager.IsCursorVisible` 为 true 时调 `ForceHideCursor()` 重置计数并恢复隐藏+锁定；死亡/设置面板走 timeScale=0 提前 return，武器轮盘走 HasWindow 豁免，均不受影响

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`