# Minimap System（小地图系统）

## 概述

战斗场景右上角的小地图，采用**传统 2D 平面缩略图**样式（参考 Apex / 三角洲行动）：入场时对整张地图做一次正交俯视**一次性烘焙**成静态纹理，之后相机停用、不再逐帧渲染 3D 场景；UI 通过 `RawImage.uvRect` 平移一个以玩家为中心的观察窗口来显示局部地图，窗口钳制在地图范围内，**永不显示场景外区域**。玩家与敌人以平面图标投影。按 **M**（`KeyBindAction.Map`，可改绑）切换屏幕居中的**大地图模式**（636×636 显示整幅地图，打开期间屏蔽射击/瞄准）。

## 组成

| 模块 | 文件 | 职责 |
|------|------|------|
| 系统 | `GameLogic/System/MinimapSystem.cs` | 单例（`ProcedureBattle` 挂载）。Awake 建 MinimapCamera（Euler(90,0,0) 北向上、剔除 UI 层）+ 512×512 RenderTexture；Start 时按 `BattleBoundary.Bounds` 一次性烘焙——用内置 `Unlit/Color` 替换 shader 平色输出（无光影），`Camera.Render()` 后相机停用；`WorldToMapUv(worldPos)` 世界坐标→静态纹理 UV |
| UI | `GameLogic/UI/MinimapUI/MinimapUI.cs` + `Assets/AssetRaw/UI/MinimapUI/MinimapUI.prefab` | 220×220 右上角面板（UILayer.Tips，压过狙击镜灰色蒙版，开镜不变灰；菜单类 UI 打开时自动隐藏）。OnUpdate 计算观察窗口（占整图 45%，钳制在 `MapUvMin/Max` 内）赋给 uvRect，并投影玩家/敌人图标 |

## 关键约定

- 相机旋转 Euler(90,0,0)：世界 +Z（北）对应地图上方
- 纹理为方形、按地图长边等比覆盖：`MapUvMin/Max` 标出地图实际范围对应的 UV 子区间，观察窗口只在该区间内平移
- 图标投影：`(uv - window.min) / window.size * 220 - 110`，出窗口即隐藏；玩家靠近地图边缘时标记不再居中（传统小地图行为）
- RenderTexture 为运行时对象，`MinimapSystem.OnDestroy` 负责 Release + Destroy，UI 只绑定不持有
- prefab 结构：`m_rect_Panel`(232×232 底衬，右上角 anchor，偏移 -16,-16) → `m_raw_Map` / `m_rect_IconRoot`（均内缩 6px = 220×220）；`m_img_EnemyDot` 红点模板（默认隐藏，运行时池化克隆），`m_img_Player` 绿色玩家标记（代码驱动位置）
- 图标约定：敌人红点（模板池化）、队友绿点、玩家绿标、标点按类型区分——**目标点(Move) 青色菱形 / 敌人点(Attack) 红色脉冲菱形（1.2±0.25 缩放）/ 物资点(Loot) 黄色圆点**，与 PingSystem 世界标记同色
- 未来美术化方向：烘焙纹理可替换为美术绘制的平面地图图（每关一张），系统/UI 代码无需改动
