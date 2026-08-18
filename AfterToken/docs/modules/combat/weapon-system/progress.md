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
- [x] 2026-08-08 狙击镜视觉模型修正：① 修复开镜后切武器卡镜——`SwitchToSlot` 把 `SetAimState(false)` 移到换槽之前（原顺序下 `CurrentWeapon` 已是新武器，狙击枪分支被跳过，镜 UI/相机残留）；② 镜相机从"位移到瞄准点上方"改为"视场放大镜"——与主相机同位、只旋转对准瞄准点，镜内看到主相机视野的放大裁剪，远处物体可见，活动范围=主相机渲染区域；③ scopeFov 75→37.5 恢复 1.2x 放大（0.6x 广角不符合放大镜定位）。Play Mode 实测：切武器后 aiming/UI/镜相机全部关闭、镜内可见远处物体
- [x] 2026-08-08 狙击镜抬高+压边平移（二版，后废弃）：① 镜相机架到瞄准点上空 12m，FOV 按高度比例换算保持视觉 1.2x；② 开镜瞄准射线改从镜相机发出，准星压边驱动瞄准点限速平移（8m/s），射程不受主相机视锥限制；③ 修复两个反馈环陷阱（LookRotation 跟踪致瞄准距离发散、平移不限速失控）。Play Mode 实测通过，但用户按鸭科夫参考图拍板改纯放大镜，机制回退
- [x] 2026-08-08 狙击镜改纯视觉镜窗（三版，按鸭科夫参考图+用户四点需求）：① 开镜 = 全屏均匀灰色蒙版（alpha 0.3，不遮挡场景信息，删除带圆孔暗角）+ 跟随准星的狙击镜图案（圆环+贯径十字线，直径 250 逻辑像素≈0.59 屏高，去中心点）；② **默认无放大无畸变**——`TbWeapon.scopeFov` 语义改为 0=无放大纯视觉镜窗、>0=启用放大镜模式（1004 狙击默认 0，Excel+JSON+__beans__ 注释同步）；③ `WeaponInstance.ScopeFov` 删除 15 兜底直接透传（0 不再被吞），`IsScopedSniping` 不再要求 scopeFov>0；④ 开镜射击仍直接命中镜窗中心（瞄准射线始终来自主相机过准星）。Play Mode 实测：灰蒙版/镜窗图案/无放大画面连续/开火 5→4 直接命中、Console 0 错误
- [x] 2026-08-08 狙击镜蒙版改带圆孔（用户：镜窗内要正常渲染、注意力聚焦镜内）：灰色蒙版从全屏均匀改为带圆孔遮罩——圆孔与镜窗同径、跟随准星，孔内 alpha=0 零遮挡正常渲染，孔外中性灰 alpha 0.3 压灰（`SniperScopeUI.CreateVignetteSprite` 白色 alpha 渐变纹理 × `Image.color` 灰色，复用原暗角的圆孔机制）。像素级验证：孔内 = 原场景色、孔外 = 0.7×场景+0.3×灰 精确吻合
- [x] 2026-08-08 狙击镜命中/后坐力反馈：① 命中标记——订阅 `IHitFeedbackEvent.OnHitTarget`（白/暴击橙）与 `IBattleEvent.OnEntityKilled`（红，仅玩家击杀），镜窗中心四刺 × 标记（运行时生成精灵），punch 缩放 1.35→1 + 0.25s 淡出（开镜期间 HitFeedbackUI 被框架隐藏，镜内自绘）；② 后坐力——订阅 `IWeaponEvent.OnFire`，镜窗连同蒙版圆孔上跳 30px+横向随机 ±7px，指数回弹 12/s，与相机抖动叠加。Play Mode 实测：开火 kick=(2.3,30) 正常回弹归零、命中标记白色 punch+淡出动画逐帧衰减、弹药 5→4、Console 0 错误（事件中数据为帧末派发，同帧立即查 enabled 可能仍为 false，属正常）
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
