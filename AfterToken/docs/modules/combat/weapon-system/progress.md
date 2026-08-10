# Weapon System 进度

## 已完成
- [x] `WeaponSystem` 武器槽管理（3 槽位）
- [x] 开火、换弹、瞄准调度
- [x] 武器扩散、后坐力、移动速度系数
- [x] `AimAssistSystem` 辅助瞄准与火箭锁定
- [x] `WeaponInstance` 运行时实例
- [x] 狙击镜 Duckov 式实现（2026-08-05）：跟鼠标的圆形镜窗 + 镜外压暗半透明；倍率走 `TbWeapon.scopeFov`（0=无狙击镜，1004 狙击=37.5，主相机 FOV 45 的 1.2 倍放大）；`WeaponInstance.ScopeFov` 预留配件修正叠加口；镜窗中心 = 子弹落点（视轴对准 `PlayerEntity.AimPosition`）
- [x] 2026-08-06 狙击镜三件套：① 开镜模式（Hold/Toggle）进设置面板，走 `SniperAimModeSetting`（仅狙击枪生效，其他武器仍用序列化 `_aimMode`）；② 开镜状态射击直接命中镜窗中心——跳过辅助瞄准与扩散（`WeaponSystem.IsScopedSniping`），且不生成 tracer 飞行动画（`BallisticSystem.FireRaycast` 分支）；③ 倍率从 4x 调为 1.2x（scopeFov 15→37.5）
- [x] 2026-08-08 狙击镜优化：① 倍率减半——scopeFov 37.5→75（相对主相机 FOV 45 从 1.2x 变为 0.6x，镜内视野比主视角更广，Excel 与 JSON 配置同步更新）；② 镜外压暗调得更通透（暗角有效不透明度约 30%）；③ 开镜命中时伤害数字显示在镜窗内——`BattleSystem.ShowDamageNumber` 在 `IsScopedSniping` 时走 `SniperScopeUI.ShowScopeDamage`（按镜相机取景变换换算镜窗局部坐标），复用 `DamageNumberUI` 对象池与飘字动画（新增 `ShowLocal`）。附带修复两个存量问题：关镜销毁窗口时 `_activeNumbers` 残留已销毁飘字（加 null 剔除）；开镜期间 `DamageNumberUI` 被框架置 `Visible=false` 导致飘字动画冻结（新增 `TickExternal` 由 `SniperScopeUI.OnUpdate` 代驱动）。Play Mode 实测截图验证
- [x] 2026-08-08 开镜/不开镜灵敏度分离：`SensitivitySetting.ScopedValue` 独立存档（`settings.scopeSensitivity`，未初始化时跟随普通灵敏度），`CrosshairUpdater` 开镜时改用 ScopedValue；狙击镜镜窗从跟随原始 `Input.mousePosition` 改为跟随准星 `CrosshairUpdater.CurrentScreenPos`，保证镜窗/准星/子弹落点三者一致且都受灵敏度控制；设置面板新增 Scope Sensitivity 滑块。Play Mode 实测：滑块渲染与绑定、存档写入、镜窗跟随准星均通过
- [x] 武器轮盘 `WeaponWheelUI`
- [x] 弹匣为空自动换弹
- [x] 换弹状态事件 `OnReloadStateChanged`
- [x] 切换武器中断当前武器换弹
- [x] `TbWeapon` 配置已接入（`WeaponConfigMgr` 从 Luban 表读取）
- [x] 硬编码 `WeaponConfigMgr` 已替换为 Luban 表驱动
- [x] 武器切换冷却从 `TbPlayer` 读取，不再硬编码
- [x] 辅助瞄准参数（半径/角度/锁定距离/角度/时间）从 `TbWeapon` 读取，不再硬编码

## 进行中
- [ ] 无

## 待办
- [ ] 武器切换动画
- [ ] 武器开火/换弹/切换音效（依赖 `audio-system`）
- [ ] 武器特殊效果（如激光指示、追踪导弹）
- [ ] 换弹过程可被冲刺/受击等动作打断/加速（视玩法需求）

## 阻塞
- 无

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
