# Effect System 进度

## 已完成
- [x] 爆炸特效 shader 化落地（火球+冲击波，2026-08-30，在 projectile-system 内）——作为 EffectSystem 的参考实现与首个迁移对象
- [x] 特效系统设计方案：`docs/Proposal/infra/effect-system.md`（2026-09-06）
- [x] M1（2026-09-06）：`EffectSystem` 统一入口 + `TbEffect` 配表 + 池化 + `IEffectEvent`；爆炸特效迁入 prefab
  - TbEffect：effect.csv + __beans__/__enums__/__tables__ 登记 + `_tableFiles` 白名单 `cfg_tbeffect`（注意白名单源头在 `Configs/GameConfig/CustomTemplate/ConfigSystem.cs`，导表 bat 会覆盖 GameProto 副本）
  - EffectSystem：EffectPool 按 ID 隔离、poolCapacity 上限、满回收最老；句柄池化；Preload 分帧预热一半容量；ClearAll 防跨场景泄漏
  - 资产闭环：shader `AssetDatabase.MoveAsset` 到 AssetRaw/Shaders（GUID 不变）、.mat 落 AssetRaw/Materials、Explosion.prefab 挂热更 EffectDriver（同 Enemy.prefab 序列化方式）
  - ProjectileSystem 爆炸点改发 IEffectEvent + 相机抖动；`ExplosionEffectDriver` 已删除（Shader.Find/new Material/CreatePrimitive 全部移除）
  - 附带修复：CameraSystem3D 补 ICameraEvent.OnCameraShake 订阅（此前 3D 战斗无人消费抖动事件，是死代码）
  - 实测：编译 0 error；101 关预热 idle=4/8；火箭筒开火爆炸正常；同帧 12 播放压到 active=8；撤离回基地 0 泄漏、重进重预热；截图 `Assets/Screenshots/effect_m1_explosion.png`
- [x] 去配置表简化（2026-09-06）：TbEffect 全链路拆除（effect.csv / EffectConfigMgr / cfg.Effect+TbEffect+EEffectAttachMode 生成代码 / cfg_tbeffect.json / 三个 xlsx 登记行 / `_tableFiles` 白名单），元数据改挂 prefab `EffectDriver` 序列化字段（duration/poolCapacity/attachMode/scaleByRadius/preload），播放 API 改 string address（`EffectIds.Explosion="Explosion"`），IEffectEvent 同步改参；EffectSystem 池按地址隔离、加载 prefab 后从资产读元数据缓存、Preload 走内置 `PreloadEffects` 注册列表
  - 实测：编译 0 error、导表 bat 干净；101 关预热 idle=4/8（容量从 prefab 读）；火箭筒开火爆炸视觉正常；同帧 10 连发压到 active=8 节点数=8；撤离主菜单 0 残留、重进重预热；全程 Console 0 error；截图 `Assets/Screenshots/effect_m1_explosion_v2.png`

- [x] M2（2026-09-07）：枪口火焰、命中火花（敌/环境）、拾取光晕接入；爆炸补齐方案 §7.2 三段式（预警压缩闪光 + 焦痕 decal）
  - 新增 prefab（`AssetRaw/Effects/`，根挂 EffectDriver）：MuzzleFlash（0.06s/24 池/World）、HitSpark 与 HitSparkEnv（0.25s/32 池/World，共用 HitSpark.shader 靠材质 _Tint 区分橙红/灰白）、PickupGlow（duration=0 持久/8 池/Follow，目标销毁自动回收）
  - 新增 shader/材质（AssetRaw 闭环）：MuzzleFlash.shader（星形定向闪光）、HitSpark.shader（径向火花+衰减）、PickupGlow.shader（呼吸圆环，加色 Blend SrcAlpha One）、ScorchDecal.shader（焦痕 + _Fade 线性淡出）
  - EffectDriver 分派两条时间轴：`_fireball` 非空走爆炸三段式（预警段 `_warningDuration` 10% 尺度高亮白闪 _Flash=1、冲击波不露头 → 主体 `_blastDuration` 火球膨胀侵蚀+冲击波 1.5 倍领先外扩 → 余韵 `_scorchDecal` 焦痕 `_scorchFadeDuration` 线性淡出）；`_fireball` 为空走通用面片（`_genericVisual` 缩放 ease-out + _Progress 推进）
  - 触发点：枪口 BallisticSystem.OnFire（:235，按弹道方向定向）；命中 BallisticSystem:247 与 ProjectileSystem:397（敌=HitSpark / 场景=HitSparkEnv）；拾取 PickupEntity:55（PlayAttached Follow）；爆炸仍 ProjectileSystem:270 发 IEffectEvent
  - 实测：编译 0 error；101 关预热 MuzzleFlash idle=12/24、HitSpark/Env idle=16/32、PickupGlow idle=4/8、Explosion idle=4/8；真实开火链路枪口闪光与命中火花均触发；RPG 慢动作（timeScale=0.03）确认预警段压缩闪光球、恢复后焦痕清晰；killall 后 7 个掉落物光晕 active 且跟随正确（截图确认淡金呼吸环）；全程 Console 0 error。截图 `Assets/Screenshots/muzzle_flash3.png`、`explosion_phase.png`、`pickup_glow8.png`

## 待办（按方案三期推进）
- [ ] M3：编辑器预览工具 + 美术替换规范

## 遗留
- 非预热特效首次 Play 丢弃并告警（懒加载后生效）——新特效 prefab 默认应勾 preload 并登记 `EffectSystem.PreloadEffects`
- 真机 AOT 下 prefab 挂热更脚本验证在 M5 清单
- 高频点 GC profiler 抽查未做（M2 原范围，代码路径零分配设计，待真机/Profiler 复核）

## 阻塞
- 无。

---

> 状态说明：
> - 当前总状态：🟡（M1/M2 已落地，M3 待推进）
> - 每次更新后同步 `docs/TODO.md`
