# Input System 进度

## 已完成
- [x] 移动、瞄准、开火、换弹、切枪输入事件
- [x] **移动改相机系相对（2026-08-06）**：相机偏航（中键拖拽）后 WASD 仍对齐屏幕方向。原实现输入向量直接当世界系 XZ 用，偏航 ≠0 时 W 在屏幕上呈斜向。修复：`InputSystem.ToCameraSpace` 用主相机 forward/right 的 XZ 投影旋转输入向量（yaw=0 时与原行为一致；`_mainCamera` 懒获取兜底）。实测 yaw=30° 时 W→(0.50,0.87) 与相机前向一致
- [x] IBattleInputEvent 接口定义与发送
- [x] 瞄准迁移 XZ 平面：相机射线与 y=0 平面求交发 (x, z)；修复锁定光标下滚轮恒选 slot 0（改用 CrosshairUpdater 位置计算）
- [x] **菜单 UI 打开时屏蔽射击/瞄准（2026-08-19）**：修复开箱（LootContainerUI）状态下左键点击仍会开枪。`IsMenuUIOpen()`（BattleBagUI/LootContainerUI/NoteUI 任一打开）期间跳过 `HandleAimInput`/`HandleFireInput`/`HandleAimButtonInput`；UI 打开瞬间补发 `OnFireReleased`/`OnAimReleased`，防按住开火键开 UI 后武器卡按下状态。移动/换弹/切枪不受影响（开箱可移动属搜打撤风险设计）。实测：NoteUI 打开期间拦截门=true、E 关闭后恢复=false
- [x] **菜单 UI 打开时冻结准星（2026-08-19，已被 2026-08-23 同步方案取代）**：修复看纸条时鼠标移动仍在后台驱动隐藏准星、关掉后瞄准点被"强制挪动"。`CrosshairUpdater.Update` 在 `CursorManager.IsCursorVisible`（系统光标可见=菜单类 UI 打开）时直接 return，准星位置不再累加鼠标位移；实测开/关纸条准星位置不变、光标隐藏锁定恢复
- [x] **准星/系统光标位置同步（2026-08-23，同日被"准星位置神圣不可侵犯"方案取代）**：修复"切换准星后系统鼠标显示且与游戏准星位置不一致"。两个缺口：①旧"冻结"方案下，设置面板（timeScale=0 不冻结、且 SettingsUI 漏了隐藏准星）期间真实鼠标移动而准星停在原位，双光标同屏不同位；②准星被 SetVisible(false) 隐藏的 UI（背包/开箱/纸条）关闭后，准星从打开前的旧位置继续累加，与鼠标脱节。修复：`CrosshairUpdater.Update` 光标可见分支由"冻结"改为"同步"——未锁定时 `Input.mousePosition` 有效，直接对齐（含 SettingsUI/WeaponWheelUI 豁免的泄漏兜底不变）；新增 `OnEnable` 对齐（各 UI 约定先 `SetVisible(true)` 再 `HideCursor`，恢复瞬间光标仍可见未锁定，位置有效）；`SettingsUI` 补上 `SetVisible(false/true)` 配对（OnCreate/OnDestroy，与背包等一致）。Play 验证：设置开→准星隐藏+光标可见；关→准星恢复+光标隐藏锁定+位置同步为真实鼠标位置（(1920,124) 而非旧的屏幕中心）
- [x] **准星位置神圣不可侵犯（2026-08-23，用户拍板，取代同日的"同步"方案）**：核心原则——准星位置是玩家的瞄准状态，只有战斗中的鼠标位移能驱动它；任何 UI 操作（背包/轮盘/设置/切准星样式）都不得强制移动准星，关 UI 后瞄点原样保留，系统鼠标只在点选类 UI 期间出现、用完可靠收回。落地：①`CrosshairUpdater` 撤销"光标可见时同步真实鼠标"与 `OnEnable` 对齐，回归纯冻结（光标可见或轮盘打开时直接 return；泄漏兜底 ForceHideCursor 保留）；②`WeaponWheelUI` 改为行业标准 radial menu 做法——**不再 ShowCursor/HideCursor**（光标全程锁定隐藏，系统鼠标不出现），选择改由打开轮盘以来的鼠标位移增量累积决定方向（死区 20px 内保持原武器），并 `SetVisible(false/true)` 隐藏/恢复准星；③`WeaponSystem.OnWeaponSelected` 已有 slot<0 保护，死区未推满时松开保持原武器，无需改 InputSystem。Play 验证：背包/设置/轮盘三条路径开→关，准星位置全程恒为 (960,540) 零漂移，光标显隐/锁定全部正确，样式切换生效。轮盘增量的手感（灵敏度/死区）待用户实测微调
- [x] **菜单 UI 打开时冻结角色朝向（2026-08-19）**：修复看纸条时"瞄点还是会变"——瞄准输入被屏蔽后 AimPosition 冻结，但 `PlayerEntity.Update` 每帧仍朝旧瞄点旋转，玩家移动时角色原地自转。修复：`PlayerEntity.Update` 在 `InputSystem.IsMenuUIOpen()`（改 public static）时直接 return，朝向完全冻结
- [x] **战斗光标泄漏兜底（2026-08-19）**：修复关闭纸条后 Windows 系统鼠标仍显示（ShowCursor/HideCursor 引用计数泄漏，实测关闭后 refCount=1）。`CrosshairUpdater.Update`（战斗常驻 HUD 组件）新增兜底：无菜单类 UI（含 WeaponWheelUI）且 `CursorManager.IsCursorVisible` 为 true 时调 `ForceHideCursor()` 重置计数并恢复隐藏+锁定；死亡/设置面板走 timeScale=0 提前 return，武器轮盘走 HasWindow 豁免，均不受影响
- [x] **按键运行时改绑（2026-08-23）**：`InputSystem` 的 8 个动作键（Fire/Aim/Reload/Dodge/Interact/WeaponWheel/Bag/CrosshairStyle，含原硬编码的 Mouse0 开火与 C 准星样式）从 `[SerializeField]` 改为运行时读 `KeyBindingSetting.GetKey()`，即时生效；ESC 全局 UI 键固定不可改绑，且改绑捕获期间（`SettingsUI.IsCapturingKey`）跳过 ESC 关 UI 逻辑。移动 WASD 与滚轮切枪走 Input Manager Axis，不在改绑范围内

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`