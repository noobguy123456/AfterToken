# 特效系统

## 职责

管理战斗中的视觉特效（枪口火焰、命中火花、爆炸等）的加载、播放与池化回收。玩法代码只发"播放请求"（IEffectEvent 或直调 Play），不碰 GameObject。

## 已实现（M1 2026-09-06 去配表简化；M2 2026-09-07）

| 类/文件 | 说明 |
|---|---|
| `GameLogic/System/EffectSystem.cs` | 统一入口：MonoBehaviour 挂 BattleRoot + 静态门面（`Play` / `PlayAttached` / `Preload` / `ClearAll` / `GetPoolStats`）；内嵌 `EffectPool`（每特效地址一池，容量到顶强制回收最老；通用池 GameObjectPool 无容量/在播追踪语义，故自研）；`PreloadEffects` 静态注册列表驱动预热 |
| `GameLogic/Effect/EffectIds.cs` | 特效资源地址常量（YooAsset 文件名寻址），新增特效在此登记 |
| `GameLogic/Effect/EffectContext.cs` | 播放参数 struct（Scale 等），热路径零分配 |
| `GameLogic/Effect/EffectDriver.cs` | 特效 prefab 根节点驱动；**序列化元数据字段**：`_duration`(0.5) / `_poolCapacity`(8) / `_attachMode`(World/Follow) / `_scaleByRadius` / `_preload`，EffectSystem 加载 prefab 后直接从资产读取缓存（无需实例化）；`Init(ctx)` / `OnRecycle()` / `OnFinished`（duration=0 自报备用）；两条时间轴按字段分派——`_fireball` 非空走爆炸三段式（预警压缩闪光 `_warningDuration` → 主体膨胀/冲击波 `_blastDuration` → 焦痕 `_scorchDecal`+`_scorchFadeDuration` 淡出），`_fireball` 为空走通用面片（`_genericVisual` 缩放曲线 + _Progress，MaterialPropertyBlock） |
| `GameLogic/Effect/EffectInstance.cs` | 播放句柄（Stop 提前结束），对象池化复用 |
| `GameLogic/IEvent/IEffectEvent.cs` | `OnPlayEffect(address, pos, rot, ctx)`（address 为 string），EffectSystem Awake 订阅 |
| `Assets/AssetRaw/Effects/*.prefab` | 5 个特效 prefab，根挂 EffectDriver：Explosion（三段式，Fireball+Shockwave+Scorch 子节点）、MuzzleFlash（0.06s/池24/World，星形定向闪光）、HitSpark / HitSparkEnv（0.25s/池32/World，共用 HitSpark.shader 靠材质 _Tint 区分橙红/灰白）、PickupGlow（duration=0 持久/池8/Follow，呼吸圆环，目标销毁自动回收） |
| `Assets/AssetRaw/Materials/*.mat` | 各特效材质资产（替代运行时 new Material，闭环 GUID 引用链）；HitSparkEnv 与 HitSpark 同 shader 不同 _Tint |
| `Assets/AssetRaw/Shaders/*.shader` | ExplosionFireball / ExplosionShockwave / MuzzleFlash / HitSpark / PickupGlow（呼吸圆环加色）/ ScorchDecal（_Fade 淡出），全部落 AssetRaw 由 YooAsset 依赖收集带入包 |

> 2026-09-06 简化：TbEffect 配置表（effect.csv / EffectConfigMgr / cfg.Effect / cfg.TbEffect / EEffectAttachMode 生成代码）已整体拆除。决策理由：特效与武器一一对应、参数为程序/美术属性、本项目 GameLogic 代码即热更内容，"不改代码热更"不成立。详见 `docs/Proposal/infra/effect-system.md` §3.2。

## 使用方式

```csharp
// 玩法侧：发事件（推荐，逻辑/视觉解耦）
GameEvent.Get<IEffectEvent>()?.OnPlayEffect(EffectIds.Explosion, pos, Quaternion.identity, new EffectContext(radius));
// 或直调
EffectSystem.Play(EffectIds.Explosion, pos, rot, new EffectContext(radius));
var handle = EffectSystem.PlayAttached(EffectIds.Explosion, targetTransform); // Follow 模式
handle.Stop();
```

- 资源寻址：YooAsset 收集器 AddressByFileName，EffectIds 常量填文件名（如 `Explosion`）。
- 生命周期：进战斗 `ProcedureBattle` 调 `EffectSystem.Preload()`（预热 `PreloadEffects` 列表中 prefab 上 `preload=true` 的特效，异步加载 + 分帧实例化一半容量）；退出流程 `ClearAll()` + EffectSystem.OnDestroy 双保险。
- 新增特效：做 prefab 放 `AssetRaw/Effects/`（根挂 EffectDriver 调字段）→ `EffectIds` 加常量 → 需预热则在 `EffectSystem.PreloadEffects` 登记 → 调用点用常量播放。

## 触发点（M2）

- 枪口火焰：`BallisticSystem.OnFire`（BallisticSystem.cs:235，按弹道方向 `Quaternion.LookRotation` 定向）。
- 命中火花：弹道命中 `BallisticSystem.cs:247`、火箭弹直击 `ProjectileSystem.cs:397`；命中敌人 = HitSpark（橙红），命中场景 = HitSparkEnv（灰白）。
- 拾取光晕：`PickupEntity.cs:55`（PlayAttached，Follow 掉落物，拾取/销毁自动回收）。
- 爆炸：`ProjectileSystem.cs:270` 发 IEffectEvent，同步发相机抖动。

## 规划中（M3）

- M3：编辑器预览工具（特效列表 + 点击播放）+ 美术替换规范。

## 设计要点

- 特效使用对象池复用；池满强制回收最老实例（特效可丢弃，不可卡顿）。
- 通过事件接收特效播放请求。
- 爆炸联动相机抖动：ProjectileSystem 爆炸点同时发 `ICameraEvent.OnCameraShake`（CameraSystem3D 已订阅，Perlin 水平面抖动）。
