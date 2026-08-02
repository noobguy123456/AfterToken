# UI 渲染架构方案（修订版）：Overlay 为主 + UI 相机只做特效

> 2026-08-01 起草并修订。起因：经营场景管理面板（SSC 挂角色相机）倾斜/"陷入地下"；进一步讨论后明确约束：**Scene 视图调试体验不能被 UI 污染**（现状场景 50m、UI 平面巨大，调试很碍事）、不为简单 UI 付出多余性能。
> 本文替代初版"全部窗口转 SSC 挂 UICamera"方案（初版会加剧 Scene 视图污染，已废弃，见 §4 影响分析）。

---

## 1. 前提事实

- **渲染管线是 Built-in RP**（`GraphicsSettings.asset` 无自定义管线）。URP 的 Camera Stacking 不可用；Built-in 下的等价物就是"相机 depth + clearFlags + cullingMask 分层"，项目里已有（UICamera depth=2）。
- TEngine 无 UI 模块；`GameLogic.UIModule` 是自研窗口框架（窗口堆栈 + UILayer 五层），`UIRoot.prefab` 结构来自 TEngine。
- 现状渲染链路：
  - 19 个框架窗口 → 被 `UIWindow.FixFullScreenCanvas()` 强制 **Overlay**（屏幕合成，不占场景、Scene 视图不可见）
  - `SimulationMainUI` 自建 `SimulationUIRoot` → **SSC 挂 Main Camera**（透视 60° 俯视跟随玩家）→ 倾斜/陷地 bug，且 Scene 视图里多一块斜板子
  - `UICanvas`（prefab 里是 SSC、planeDistance=100）→ Scene 视图远端一块 100m 外的大平面，"场景很小 UI 很大"的另一个污染源
  - `BuildingEntity` 建筑牌子 → World Space 挂 Main Camera（场景内 UI，合理，不动）

## 2. 性能真相（先纠正一个认知）

**渲染模式（Overlay / SSC）本身对性能的影响可以忽略**，不足以作为选型依据：

- Overlay 不是"更省"，它只是不参与相机渲染，由屏幕合成阶段直接画；SSC/UICamera 多一个只渲 UI 层的相机 pass，几十个 sprite 的开销微乎其微。
- 真正决定 UI 性能的是：**Canvas rebuild 频率**（布局/文本变化触发重建）、**overdraw**（大面积半透明叠层）、**批次合批**（图集）。这三点与渲染模式无关。
- 结论：选型应该由**调试体验、特效能力、架构简单**决定，而不是性能焦虑。

## 3. 最终方案（Built-in RP 业界成熟三层结构）

```
① 界面 UI（菜单/HUD/面板/背包/仓库…全部系统界面）
   → Screen Space - Overlay
   Unity 官方对纯 2D UI 的推荐模式；PC/主机游戏绝对主流。
   不占用场景、Scene 视图完全不可见（调试零污染）、无相机依赖、永不倾斜。

② UI 特效（背包特效框、出货光效等）
   → 首选：美术序列帧，直接在 Overlay 画布内播放（Animator/Sprite 切换）
     ——零插件、零相机依赖，美术已在出序列帧，路线现成。
   → 备选（未来确需 3D 粒子穿插时）：启用 UICamera 特效层
     UICamera（正交、depth=2、clearFlags=Depth、只渲 UI 层）+ 特效物体放 UI 层，
     锚定在远离场景的固定坐标（如 x=1000），屏幕坐标经 UICamera 换算。
     这是手游"UI 特效相机"的标准做法；特效区远离原点，Scene 视图框选场景时不受干扰。

③ 场景内 UI（建筑头顶牌子、场景标牌、伤害数字）
   → World Space Canvas 挂场景相机（现状保持不变）。
   它是场景的一部分，本来就该透视、该被遮挡。
```

相机遮罩分工（配合②③）：

- 场景相机（战斗/经营各自的 Main Camera）：`cullingMask` **剔除 UI 层**——场景里永远不该出现 UI 层物体，防止未来特效物体被场景相机透视重渲。
- UICamera：只渲 UI 层，平时 **disabled**（没有 3D 特效需求时完全空转都不必），启用特效时打开。

## 4. 影响分析（对初版方案的修正）

| 项 | 初版（全部窗口转 SSC 挂 UICamera） | 修订版（Overlay 为主） |
|----|----------------------------------|------------------------|
| Scene 视图污染 | **加剧**——所有 UI 变成场景里的实体平面，正是你讨厌的现状 | **彻底消除**——Overlay 在 Scene 视图不存在 |
| 改动面 | 19 个窗口全量改 + 全量回归 | 19 个窗口**零改动**（本来就是 Overlay） |
| 性能 | 多一个全屏 UI 相机 pass | 维持现状（最优） |
| 倾斜/陷地类 bug | 不再发生（正交）但架构更复杂 | 从根上不存在（无相机） |
| 特效能力 | UICamera 直接可用 | 序列帧直接可用；3D 粒子按需启用 UICamera，能力不丢 |
| 风险 | 高（全窗口回归） | 低（改动集中在经营 UI 一处） |

对现有项目的影响：

- **19 个框架窗口：零改动、零影响**（`FixFullScreenCanvas` 保持不动）。
- **经营 UI**：拆 `SimulationUIRoot`，HUD/面板挪回框架 UIRoot 下走 Overlay——倾斜/陷地 bug 修复，Scene 视图干净。之前选 SSC 是为了"3D 粒子穿插"，现由序列帧 + UICamera 特效层（备选）覆盖该需求。
- **Scene 视图调试**：`UIRoot.prefab` 的 UICanvas 由 SSC 改为 Overlay（与实际用法对齐），远端 100m 大平面消失；Scene 视图只剩场景本身。
- **战斗场景**：无任何影响（战斗 UI 本来就全 Overlay）。

## 5. 改动清单（三步，改动面小）

### 步骤 1：经营 UI 回归框架（修 bug 本体）
- `SimulationMainUI`：删除 `SimulationUIRoot` 自建 Canvas 代码，HUD 与管理面板建在窗口自身的框架节点下（Overlay）;`OnDestroy` 对应清理。
- 验证：进经营场景，任何移动/缩放下 HUD 与面板贴屏平整；Tab/按钮切换正常；Scene 视图里不再看到 UI 板子。

### 步骤 2：UIRoot.prefab 对齐实际用法
- `UICanvas` renderMode：ScreenSpaceCamera → **ScreenSpaceOverlay**（窗口本来就全 Overlay，父 Canvas 不该是 SSC）;UICamera 设为 **inactive 备用**（未来做 3D 特效时再启用并锚定到 x=1000）。
- 验证：所有窗口显示/交互不变；Scene 视图远端大平面消失。

### 步骤 3：场景相机剔除 UI 层（防御性）
- `ProcedureBattle` / `ProcedureSimulation` 初始化相机时：`cullingMask &= ~(1 << UI层)`。
- 验证：Game 视图显示不变；建筑牌子（Default 层、World Space）不受影响。

### 不做的事（相对初版）
- 不动 `FixFullScreenCanvas`、不动 19 个窗口、不动 UILayer/sortingOrder 体系。
- CanvasScaler 参考分辨率 750x1334（竖屏默认值）→ 1920x1080 的修正**单独再做**，不混入本次（避免 UI 缩放变化和渲染调整互相干扰排查）。

## 6. 验收标准

- 经营场景 UI 在 Game/Scene 视图、任意移动/缩放下贴屏平整，无倾斜/陷地。
- Scene 视图只有场景内容（无 UI 平面、无远端大板子），框选/调试不受 UI 干扰。
- 全部窗口功能正常，Console 0 报错。
- 新增 UI 规范（沿用既定规则）：系统界面默认 Overlay；需要 3D 特效时先评估序列帧，不够再启用 UICamera 特效层；场景内标牌用 World Space。
