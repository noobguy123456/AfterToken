# 特效系统设计方案

> 状态：M1/M2 已落地（M1 2026-09-06 三维评估见 §7；M2 2026-09-07 枪口/命中/拾取光晕 + 爆炸三段式预警/焦痕，实现见 `docs/modules/infra/effect-system/`）
> **2026-09-06 简化修订**：拆除 TbEffect 配置表，特效元数据（时长/池容量/挂载方式/缩放/预热）改为 prefab 上 `EffectDriver` 的序列化字段，播放 API 改为按 YooAsset 资源地址（`EffectIds` 常量）寻址。理由见 §3.2。
> 相关模块：`docs/modules/infra/effect-system/`
> 前置实现参考：爆炸特效（`docs/Proposal/combat/explosion-shader-proposal.md`）、子弹逻辑/视觉分离（`docs/Proposal/combat/bullet-logic-visual-separation.md`）

## 1. 背景与现状

目前特效只有一处落地：火箭筒爆炸（`ExplosionEffectDriver`）。它的实现方式是**占位性质**的：

- `Shader.Find("AfterToken/ExplosionFireball")` 按名找 shader（打包需手动加 Always Included Shaders）；
- 运行时 `new GameObject` + `CreatePrimitive` 拼视觉，无 prefab、无池化，每次爆炸都产生 GC 与实例化开销；
- 自驱动自毁（Update 里推进时间轴），生命周期写死在驱动代码里；
- 播放方（`ProjectileSystem`）直接 `Create(...)`，特效与玩法逻辑硬耦合。

命中反馈（伤害飘字/受击指示）走 UI 系统，是另一条独立链路，不在本方案范围内。

`Assets/AssetRaw/Effects/`（空目录）已在 YooAsset 收集器范围内，资源目录位已就位。

## 2. 目标

1. **统一生命周期**：特效的加载、播放、回收由唯一入口管理，玩法代码只发"播放请求"，不碰 GameObject。
2. **配置化**：特效地址、时长、池容量等元数据序列化在 prefab 的 `EffectDriver` 字段上（程序/美术属性随资产走，见 §3.2）。
3. **池化复用**：高频特效（枪口火焰、命中火花）零运行时实例化。
4. **逻辑/视觉分离**：沿用子弹系统的既定方针——逻辑态只产出"播放特效"事件/请求，视觉表现可整体替换为正式美术资源而不动逻辑。

## 3. 方案

### 3.1 结构总览

```
玩法代码（ProjectileSystem / WeaponSystem / PickupSystem ...）
    │  EffectSystem.Play(address, pos, rot, parent?)
    ▼
EffectSystem（static，热更域）
    ├── EffectIds 常量（YooAsset 文件名寻址）
    ├── EffectDriver 序列化字段（prefab 资产上：时长 / 池上限 / 挂载方式 / 缩放 / 预热）
    ├── EffectPool（每特效地址一个 GameObject 池）
    └── EffectInstance（播放句柄：进度、提前停止、跟随目标）
```

### 3.2 元数据：prefab 序列化字段 + EffectIds 常量（2026-09-06 简化，取代原 TbEffect 配表）

特效元数据以 `[SerializeField]` 挂在 prefab 根节点的 `EffectDriver` 上：

| 字段 | 默认值 | 说明 |
|---|---|---|
| `_duration` | 0.5 | 播放时长（秒），到时回收；0 = 由驱动自报完成 |
| `_poolCapacity` | 8 | 池上限（高频特效 16~32，低频 4） |
| `_attachMode` | World | `World`（生成即定死）/ `Follow`（跟随传入的 Transform），枚举内嵌于 EffectDriver |
| `_scaleByRadius` | true | 是否支持按半径缩放（爆炸类用，EffectContext.Scale = 半径） |
| `_preload` | true | 进战斗是否预热（仅对 `EffectSystem.PreloadEffects` 注册列表中的地址生效） |

`EffectSystem` 加载 prefab 后直接对资产 `GetComponent<EffectDriver>()` 读取并缓存（无需实例化）。调用方用 `EffectIds` 常量类（`GameLogic/Effect/EffectIds.cs`）引用地址，如 `EffectIds.Explosion = "Explosion"`（YooAsset 按文件名寻址）。

**为什么不用配置表**（2026-09-06 决策）：

1. 特效与武器/触发器一一对应，不存在"策划独立加特效不改代码"的场景——调用点本来就要写代码。
2. "不改代码热更"在本项目不成立：GameLogic 本身就是热更程序集，改代码与改配置的发布成本相同。
3. 时长/池容量/挂载方式是与 prefab 视觉强绑定的程序/美术属性，不是策划数据；放在 prefab 上与资产同生命周期，改一处不两处。
4. 砍掉 TbEffect 同时消灭了一条 Luban 维护链路（csv + 三个 xlsx 登记 + 白名单 + 生成代码）。

### 3.3 特效 prefab 约定

- 根节点挂 `EffectDriver`（MonoBehaviour）：`Init(EffectContext ctx)`（携带缩放/颜色等参数）、`event Action<EffectDriver> OnFinished`（duration=0 时由驱动自报）、`OnRecycle()`（池回收时复位状态）。
- **纯视觉**：不含 Collider、不参与任何逻辑查询；渲染层/排序统一约定（战斗场景特效 sortingLayer = `Effects`）。
- 材质参数一律走 `MaterialPropertyBlock`，禁止 `renderer.material` 实例化（沿用爆炸 shader 的既有做法）。
- 占位期 prefab 可以是"代码拼好的视觉原样搬进 prefab"（球体+面片+共享材质），正式资源到位后整 prefab 替换，调用方零改动。

### 3.4 池化策略

- 池按特效地址隔离，`poolCapacity` 到顶后**最老实例强制回收**（特效可丢弃，不可卡顿）。
- 预热：进入战斗流程（`ProcedureBattle`）时对 `EffectSystem.PreloadEffects` 注册列表中、prefab 上 `preload=true` 的特效做异步预热（YooAsset 加载 + 实例化 poolCapacity 的一半）。
- 场景切换时 `EffectSystem.ClearAll()` 全量回收，防跨场景泄漏。

### 3.5 播放 API

```csharp
// 一次性定点播放（枪口、爆炸、命中），address 用 EffectIds 常量
EffectSystem.Play(EffectIds.Explosion, position, rotation);
// 跟随播放（buff 光环、持续燃烧）
EffectInstance handle = EffectSystem.PlayAttached(EffectIds.Explosion, targetTransform);
handle.Stop(); // 提前结束（buff 消失）
// 带参播放（爆炸半径等），参数进 EffectContext 由驱动解释
EffectSystem.Play(EffectIds.Explosion, position, rotation, new EffectContext(radius));
```

逻辑层到视觉层经 `IEffectEvent`（`OnPlayEffect(address, pos, rot, ctx)`，address 为 string）转发——玩法系统只发事件，EffectSystem 订阅执行，与现有 IXXXEvent 惯例一致。测试/GM 场景可换一个空实现订阅器整体屏蔽特效。

### 3.6 资源与打包

- prefab 放 `Assets/AssetRaw/Effects/`，YooAsset 收集器已覆盖该目录，随 SimulateBuild/热更流程走。
- shader 由 prefab 材质引用被 YooAsset 依赖收集自动带入包，**消除**当前 `Shader.Find` + Always Included Shaders 的手动维护点（`ExplosionEffectDriver.s_shaderWarned` 的警告场景随之消失）。

## 4. 迁移路径（分三期）

| 期 | 内容 | 验收 |
|---|---|---|
| M1 | EffectSystem + 池 + IEffectEvent；爆炸特效第一个迁入（ExplosionEffectDriver 视觉搬进 prefab，ProjectileSystem 改发事件） | 101 关火箭筒实测：爆炸表现与现状一致、Console 0 error、重复爆炸无实例化尖刺 |
| M2 | 枪口火焰、命中火花、拾取光效接入；战斗内高频点全部走池 | 连续射击 1 分钟无 GC 尖刺（Profiler 抽查） |
| M3 | 编辑器预览工具（特效列表 + 点击播放）+ 美术替换规范文档 | 策划可独立试特效 |

## 5. 风险与决策点

1. **粒子系统还是 shader 面片**：**爆炸类明确推荐 shader 面片方案**（已验证、CPU 成本低、时间轴精确可控）；粒子系统只用于余烬/尘埃等不需要精确时间轴的氛围效果。选择依据是效果需求，两者在当前体量下都不构成性能瓶颈。
2. **池 vs 实例化开销**：低频大特效（建筑建造完成光柱等）池收益低，允许 `poolCapacity=1` 退化。
3. **与音频联动**：爆炸这类"特效+音效"成对出现的效果，不在 EffectSystem 里做联动——由玩法代码分别发特效事件和音频请求，保持两个系统正交。若后续大量成对出现，再考虑在 prefab 序列化字段上加音频引用由资产驱动。
4. **不做的**：不做时间轴编辑器、不做特效间编排（那是演出系统的事，与对话运镜方案同属演出域，另行立项）。

## 7. 三维评估修订（2026-09-06）

> 从性能开销、特效效果、打包便利性三个层面对 §1~§6 的复评结论，作为方案的强制补充条款。

### 7.1 性能开销

- 真实成本大头是透明混合 overdraw（噪声侵蚀为 discard 型透明），俯视角+小半径天然有界，池上限强制回收最老实例兜底——维持原设计。
- **修订**：`EffectContext` 改为 struct（或可选参数），热路径单次 `Play` 零分配。
- **新增量化验收**：预热后单次 `Play` 0 GC alloc；战斗高峰期特效系统主线程 < 0.5ms/帧（Profiler 抽查为准）。

### 7.2 特效效果（原方案最薄弱层，补质量标准）

- **三段式结构**：预警（压缩/闪光）→ 主体 → 余韵（烟雾/焦痕衰减）。现有爆炸只有主体段，迁入时补齐。
- **联动三件套**：光（shader 发光峰值 ramp 伪造，**不用真实 Point Light**——Built-in 前向渲染逐像素开销不划算）、震（接相机系统已有的抖动 API，近零成本的最大手感提升）、痕（地面焦痕 quad 数秒淡出，作为爆炸 prefab 第三元素）。
- 俯视角可读性原则：地面投影元素（冲击波环、焦痕）优先于体积元素（沿用"冲击波领先火球轮廓"修正的既有结论）。

### 7.3 打包便利性（已查证）

- YooAsset 收集器（`Assets/Editor/AssetBundleCollector/AssetBundleCollectorConfig.xml`）**只收集 `Assets/AssetRaw/*`**；爆炸 shader 当前在 `Assets/AssetArt/Shaders/`，`Shader.Find` 方案在真机包必丢。
- YooAsset 会按 GUID 引用链自动带入依赖，但**前提是引用链上都是资产对象**。当前运行时 `new Material(shader)` 的产物无 GUID，依赖收集摸不到。
- **M1 硬步骤（新增）**：材质落成 `.mat` 资产放 `AssetRaw/Materials/`（目录已注册）；shader 挪 `AssetRaw/Shaders/`（目录已注册）；prefab 引用 .mat 闭环引用链——此后 Always Included Shaders 手动维护点才真正消除。
- prefab 挂热更脚本 EffectDriver 为 HybridCLR+YooAsset 标准流程，Simulate 模式开发无忧；真机 AOT 验证在 M5 清单。
- `EffectIds` 常量必须与收集器寻址规则一致（AddressByFileName，填文件名）。

## 6. 与现有系统的关系

| 系统 | 关系 |
|---|---|
| `infra/pool-system` | 复用其通用池做 GameObject 池底座 |
| `infra/event-system` | 新增 `IEffectEvent`，沿用 TEngine 事件惯例 |
| 命中反馈（UI） | 独立链路，不动 |
| 子弹逻辑/视觉分离方案 | 本方案是该方针在特效域的落地 |
