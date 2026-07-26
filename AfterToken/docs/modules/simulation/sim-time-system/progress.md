# 经营时间系统进度

## 已完成
- [x] 经营时间推进
- [x] 加速/暂停控制
- [x] 速度切换事件广播

## 实现说明
1. `TbSimTimeConfig` 配置时间倍率（1x/2x/4x）与订单刷新参数。
2. `SimTimeSystem` 维护 `currentTime`、`speed`、`isPaused`，每帧按 `deltaTime * speed` 推进。
3. 广播 `OnSimulationTimeAdvanced(float deltaTime, float totalTime)` 与 `OnSimulationSpeedChanged(ESimSpeed speed)`。
4. 支持 `Pause()` / `Resume()` / `SetSpeed(ESimSpeed)`。

---

> 状态说明：
> - 当前总状态：✅（MVP 已实现）
> - 每次更新后同步 `docs/TODO.md`
> - 详细方案见 `docs/Proposal/simulation/simulation-mvp.md`
