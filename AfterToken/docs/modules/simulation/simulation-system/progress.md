# 经营总控进度

## 已完成
- [x] `SimulationSystem` 经营总控
- [x] `ProcedureSimulation` 经营流程
- [x] 经营场景加载与清理
- [x] 加载独立经营场景 `SimulationScene`（含 Global Light / Ground / PlayerSpawnPoint / Main Camera，无场景脚本）
- [x] 场景内容初始化：地面运行时生成 1m 网格纹理（`CreateGridTexture`，纯色俯视无参照）；主相机移除战斗 `CameraSystem3D`、挂 `SimulationCameraController`
- [x] 加载玩家角色：`SpawnPlayerAsync` 复用战斗 `Player` prefab（移除 `PlayerEntity`），挂 `SimulationPlayerController`（WASD/面向/±24 边界钳制，移速读 `TbPlayer`）；`Visual` 占位视觉实例放大 5 倍并抬高 0.05m（与地面共面 z-fighting 会导致角色闪烁/消失）；相机跟随玩家
- [x] `SimulationInputSystem`：Esc 键关闭最上层弹窗 / 打开设置面板
- [x] `BuildingPlacementSystem` 挂载到 `SimulationRoot`（建筑 3D 摆放，详见 building-system）

## 实现说明
1. `ProcedureSimulation` 通过 `LoadSceneWithLoadingAsync("SimulationScene")` 加载经营场景，初始化场景内容与 `SimulationSystem`，打开 `SimulationMainUI` 后调用 `Enter()`。
2. `SimulationSystem` 在 `SimulationRoot` GameObject 上挂载 `SimTimeSystem`、`BuildingSystem`、`ProductionSystem`、`OrderSystem`、`BuildingPlacementSystem`。
3. 离开经营场景时调用 `SimulationSystem.Leave()` 暂停时间、取消摆放并清理子系统数据。
4. 相机控制使用 `SimulationCameraController` 跟随玩家（跟随模式下禁用 WASD 平移与右键拖动），战斗 `CameraSystem3D` 在进入经营流程时移除。

## 阻塞
- 🟡 **进入经营流程卡死（疑似已修，待用户确认）**：2026-08-01 实测曾主线程死循环（100% CPU）；静态排查确认卡死在逐帧 Update 阶段而非初始化，已为 `OrderSystem` 刷新 while 加防御后多次进出 Play Mode 未复现（根因未最终定位）。8 个系统仍保留 `[hb]` 心跳日志（SimTimeSystem / OrderSystem / BuildingSystem / ProductionSystem / SimulationInputSystem / CameraSystem3D / SimulationMainUI / BuildingPlacementSystem），用户确认稳定后移除并转 ✅。

---

> 状态说明：
> - 当前总状态：🟡（卡死疑似已修待确认，心跳日志待移除）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
