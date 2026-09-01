# 提案：爆炸效果 Shader 方案

> 提案状态：已实施（2026-08-30）  
> 提出时间：2026-08-30  
> 提案路径：`docs/Proposal/combat/explosion-shader-proposal.md`  
> 关联模块：`combat/projectile-system`（爆炸触发点 `ApplyExplosionDamage` → `SpawnExplosionVisual`）  
> 落地实现：`Assets/AssetArt/Shaders/ExplosionFireball.shader` + `ExplosionShockwave.shader`（Built-in ShaderLab），由 `ProjectileSystem.SpawnExplosionVisual` 驱动（提案时原为橙色球体占位）

> **实施修正（2026-08-30）**：项目实际为 **Built-in 渲染管线**（`GraphicsSettings.m_CustomRenderPipeline` 为空，manifest 无 URP 包），§7 的 URP 风险项不适用，shader 按 Built-in ShaderLab（`UnityCG.cginc`）编写。另两处实施偏差：①视觉网格运行时构建（球体+面片占位），未建 `ExplosionEffect.prefab`——正式美术资源接入时再 prefab 化；②冲击波外扩改为 ease-out 到 **1.5 倍半径**——同心同速会被不透明火球完全遮住（俯视实测确认），圆环必须领先火球轮廓才可见。

---

## 1. 背景

火箭筒爆炸目前是占位视觉：`ProjectileSystem.SpawnExplosionVisual` 生成一个橙色球体，0.3 秒内从 0.2 膨胀到 `explosionRadius*2` 后销毁。功能上能表达"爆炸范围"，但视觉上没有任何爆炸质感（火光、冲击波、烟雾）。

项目为 URP 管线、俯视 3D 视角，爆炸主要在地面（y≈0.5 的弹道视觉层）被俯视观察。

## 2. 目标

- 一个**单次播放、生命周期 < 1s**的爆炸特效，俯视下清晰可读；
- 性能可控：同屏 5~10 个爆炸不能掉帧（overdraw 是主要风险）；
- 代码侧零侵入：`SpawnExplosionVisual` 只替换生成的视觉内容，接口不变。

## 3. 方案选型

| 方案 | 组成 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A. 粒子系统堆叠 | 多个 ParticleSystem（火球+烟+火花） | 无 shader 开发量 | 俯视下粒子公告牌观感差，overdraw 高，调参繁琐 | 不推荐单独用 |
| B. 程序化 shader 球/面片 | 自定义 ShaderLab shader + 单个 Mesh | 开销极小，风格统一，参数可代码驱动 | 需要写 shader | **推荐主方案** |
| C. 序列帧贴图 | 爆炸 flipbook 贴图 + Unlit shader | 效果最好 | 需要美术资源，当前无 | 后续有原画后升级 |

**推荐 B：程序化爆炸 shader**，分两层网格组合：

### 3.1 火球层（核心）

- 网格：半球（朝向摄像机一侧，俯视视角只需要上半球）
- shader：`Unlit + 3D 噪声位移顶点 + 噪声侵蚀边缘`
  - 顶点着色器：按时间对顶点做法线方向噪声位移，形成"翻腾"的火球轮廓；
  - 片元着色器：FBM 噪声采样，随时间提高阈值做 alpha 侵蚀（dissolve），颜色在 `亮黄 → 橙 → 暗红` 三段渐变（黑体辐射色带）；
  - 参数：`_Progress`（0→1）、`_NoiseScale`、`_ColorHot`/`_ColorMid`/`_ColorCold`。

### 3.2 冲击波层（地面）

- 网格：贴地圆环面片（Ring mesh，y≈0.05 防 z-fighting）
- shader：`Unlit Transparent`
  - 圆环 UV 径向渐变，外扩动画直接用 `localScale` 驱动（现有膨胀逻辑可复用）；
  - alpha 随进度衰减，边缘加一条亮环（`smoothstep` 提取窄带）模拟冲击波前锋；
  - 可选：对地面做折射扭曲（URP 下用 `_CameraOpaqueTexture` 采样偏移），俯视下收益低，**建议不做**。

### 3.3 时间轴（总计约 0.5s）

| 阶段 | 时间 | 火球层 | 冲击波层 |
|---|---|---|---|
| 闪光 | 0~0.08s | 全亮（HDR 白）快速膨胀到 60% | 亮环出现 |
| 膨胀 | 0.08~0.3s | 膨胀到 100%，颜色 亮黄→橙 | 外扩到 `explosionRadius` |
| 消散 | 0.3~0.5s | 噪声侵蚀至消失，颜色 橙→暗红 | alpha 淡出 |

## 4. 与代码的集成

- `SpawnExplosionVisual(center, radius)` 改为实例化一个 `ExplosionEffect` prefab（半球 + 圆环两个 MeshRenderer）；
- prefab 上挂 `ExplosionEffectDriver` 组件：`Init(radius)` 后自驱动 `_Progress`，0.5s 后自毁，不需要 `ProjectileSystem` 每帧 Tick（可删掉 `_explosionVisuals` 列表）；
- shader 放 `Assets/AssetArt/Shaders/ExplosionFireball.shader` + `ExplosionShockwave.shader`；
- 颜色/时长等参数后续可挪进 `weapon.xlsx` 或独立 effect 配置表。

## 5. 性能预算

- 每个爆炸：2 个 draw call（火球 + 冲击波），无粒子、无 RT；
- overdraw 主要来自火球的半透明像素，半球网格 + 早期 alpha 裁剪（侵蚀用 `clip` 而非透明混合的区域）可控；
- 同屏 10 个爆炸 = 20 draw call，对 URP 无压力。

## 6. 实施步骤

1. 写 `ExplosionShockwave.shader`（简单，先打通驱动链路）；
2. 写 `ExplosionFireball.shader`（噪声位移 + 侵蚀 + 黑体色带）；
3. 建 `ExplosionEffect.prefab` + `ExplosionEffectDriver.cs`；
4. 替换 `SpawnExplosionVisual` 实现，删除 `_explosionVisuals` Tick 逻辑；
5. 101 关火箭筒实测验收。

## 7. 风险

- URP 下 Unlit shader 需用 `Universal Render Pipeline/Unlit` 兼容的 HLSL 写法（`Packages/com.unity.render-pipelines.universal` 的 include 路径），不要写成 Built-in 管线语法；
- 噪声函数用经典的 hash-based value noise（无需贴图），避免引入额外的噪声纹理资源。
