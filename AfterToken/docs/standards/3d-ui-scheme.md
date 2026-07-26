# 3D 空间 UI 方案

> 本文档定义了固定俯视角 3D 游戏的 UI 方案，参考 Escape from Duckov 的风格。
> 技术栈：TEngine + HybridCLR + YooAsset + UniTask。

---

## 一、方案概述

针对固定俯视角 3D 游戏，采用 **Overlay HUD + World Space 标签 + Overlay 交互提示** 的方案。

### 核心原则

1. **HUD 和菜单始终可见**：使用 Screen Space - Overlay，不会被 3D 对象遮挡
2. **建筑标签融入场景**：使用 World Space，完全融入 3D 场景
3. **交互提示在屏幕上层**：使用 Screen Space - Overlay，指向场景对象

---

## 二、UI 类型与渲染方式

| UI 类型 | 渲染方式 | 说明 |
|---------|----------|------|
| **HUD**（资源栏、建筑列表、订单板） | Screen Space - Overlay | 渲染在屏幕最上层，始终可见 |
| **菜单**（设置菜单、建筑选择界面） | Screen Space - Overlay | 渲染在屏幕最上层，始终可见 |
| **建筑标签**（建筑名称、等级、状态） | World Space | 作为 3D 对象存在于场景中，跟随建筑 |
| **交互提示**（点击建造、升级提示） | Screen Space - Overlay | 渲染在屏幕最上层，指向场景对象 |

---

## 三、渲染层级

```
3D 场景（远） 
  ↓
UI（World Space）：建筑标签
  ↓
3D 场景（近）
  ↓
UI（Overlay）：HUD、菜单、交互提示
```

---

## 四、实现细节

### 4.1 Screen Space - Overlay（HUD、菜单、交互提示）

**特点**：
- UI 始终渲染在屏幕最上层
- 不受 3D 场景影响
- 适合 HUD、菜单、对话框、交互提示

**实现**：
- 使用 `Canvas` 的 `Render Mode = Screen Space - Overlay`
- 使用 `CanvasScaler` 适配不同分辨率
- 使用 `GraphicRaycaster` 处理输入

**适用**：
- 经营主界面（资源栏、建筑列表、订单板）
- 建筑选择界面
- 设置菜单
- 交互提示（点击建造、升级提示）

### 4.2 World Space（建筑标签）

**特点**：
- UI 作为 3D 对象存在于场景中
- 受 3D 场景光照、遮挡影响
- 适合建筑标签、场景内交互面板

**实现**：
- 使用 `Canvas` 的 `Render Mode = World Space`
- 将 Canvas 放置在建筑上方
- 使用 `LookAt` 使标签始终面向相机

**适用**：
- 建筑名称标签
- 建筑等级标签
- 建筑状态标签（建造中/升级中/空闲）

---

## 五、调用方法

| UI 类型 | 调用方法 | 说明 |
|---------|----------|------|
| **经营主界面** | `GameModule.UI.ShowUIAsync<SimulationMainUI>()` | 显示 HUD |
| **建筑选择界面** | `GameModule.UI.ShowUIAsync<BuildingSelectionUI>()` | 显示建筑选择菜单 |
| **设置菜单** | `GameModule.UI.ShowUIAsync<SettingsUI>()` | 显示设置菜单 |
| **建筑标签** | `BuildingEntity.CreateLabel()` | 创建建筑标签 |
| **交互提示** | `GameModule.UI.ShowUIAsync<InteractionPromptUI>()` | 显示交互提示 |

---

## 六、现有 UI 调整

### 6.1 经营主界面（SimulationMainUI）

**当前实现**：使用 `TestUI` 作为模板，代码动态创建 UI 元素。

**调整方案**：
- 保持 `Screen Space - Overlay` 渲染方式
- 禁用 `GraphicRaycaster` 的 `Block Raycasts`，避免拦截场景中的鼠标输入
- 优化 UI 布局，确保不遮挡场景内容

### 6.2 建筑选择界面（BuildingSelectionUI）

**当前实现**：使用 `TestUI` 作为模板，代码动态创建 UI 元素。

**调整方案**：
- 保持 `Screen Space - Overlay` 渲染方式
- 禁用 `GraphicRaycaster` 的 `Block Raycasts`，避免拦截场景中的鼠标输入
- 优化 UI 布局，确保不遮挡场景内容

### 6.3 建筑标签（BuildingEntity）

**当前实现**：未实现建筑标签。

**调整方案**：
- 在 `BuildingEntity` 中添加 `CreateLabel()` 方法
- 创建 World Space Canvas，放置在建筑上方
- 使用 `LookAt` 使标签始终面向相机

### 6.4 交互提示（InteractionPromptUI）

**当前实现**：已存在，使用 `Screen Space - Overlay` 渲染方式。

**调整方案**：
- 保持 `Screen Space - Overlay` 渲染方式
- 优化 UI 布局，确保指向场景对象

---

## 七、渲染层级管理

### 7.1 UI 层级

| UI 类型 | 层级 | 说明 |
|---------|------|------|
| **经营主界面** | 100 | 最上层，始终可见 |
| **建筑选择界面** | 90 | 次上层，显示建筑选择菜单 |
| **设置菜单** | 80 | 中上层，显示设置选项 |
| **交互提示** | 70 | 中下层，显示交互提示 |
| **建筑标签** | 0 | 最下层，作为 3D 对象存在于场景中 |

### 7.2 场景层级

| 对象类型 | 层级 | 说明 |
|----------|------|------|
| **地面** | 0 | 最下层，用于点击检测 |
| **建筑** | 10 | 中下层，建筑实体 |
| **虚拟玩家** | 20 | 中上层，相机跟随目标 |
| **相机** | 30 | 最上层，渲染场景和 UI |

---

## 八、性能优化

### 8.1 UI 优化

- **减少 UI 元素数量**：只显示必要的 UI 元素，避免过度渲染
- **使用对象池**：复用 UI 元素，减少创建和销毁开销
- **优化 UI 更新频率**：降低 UI 更新频率，避免每帧更新

### 8.2 渲染优化

- **使用 Overlay 渲染**：HUD、菜单、交互提示使用 Overlay，性能最好
- **限制 World Space UI 数量**：建筑标签数量有限，避免性能问题
- **优化 Canvas 设置**：使用合适的 `CanvasScaler` 设置，避免过度缩放

---

## 九、后续建议

1. **场景文件化**：将 `SimulationScene` 的基本组件（地面、光照、虚拟玩家）移到场景文件中，减少代码动态创建。
2. **建筑标签实现**：在 `BuildingEntity` 中实现建筑标签，使用 World Space Canvas。
3. **交互提示优化**：优化交互提示的布局和指向，确保指向场景对象。
4. **UI 性能优化**：使用对象池复用 UI 元素，减少创建和销毁开销。

---

## 十、总结

针对固定俯视角 3D 游戏，采用 **Overlay HUD + World Space 标签 + Overlay 交互提示** 的方案。

**核心优势**：
1. **实现简单**：HUD、菜单、交互提示都使用 Overlay，实现简单
2. **性能最好**：Overlay 性能最好，适合大量 UI 元素
3. **符合参考游戏风格**：与 Escape from Duckov 一致

**适用场景**：
- 固定俯视角 3D 游戏
- 3D 场景 + 3D 模型
- 需要大量 UI 元素的游戏
