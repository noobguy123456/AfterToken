# Extraction System 进度

## 已完成
- [x] Luban `cfg.Level` 新增 `extractionTime` 列（float，每关可配，默认 10s），CustomTemplate 与生成拷贝同步
- [x] `ExtractionPointEntity`（场景撤离点：绿色贴地圆盘占位视觉 + 半径序列化可调 + Gizmos 线框圆，静态 `Instances` 注册表）
- [x] `ExtractionSystem`（挂 BattleRoot，由 `ProcedureBattle.InitializeBattleSystems` 添加）：
  - 玩家进入撤离圈开始倒计时（`TbLevel.extractionTime`），离开圈重置
  - 圈内存活敌人（`EnemyRegistry.All` 中 `!IsDead`）时暂停倒计时
  - 倒计时归零且玩家存活 → 转场撤离到经营场景（复用 `PortalSystem.ExtractToBase`）
- [x] `PortalSystem.ExtractToBase()` 公共撤离结算（临时背包转仓库 + `CrossPlayLink.OnBattleExtracted` + 切 `ProcedureSimulation`），RETURN_BASE 传送门同用
- [x] `BattleMainUI` 顶部居中倒计时文本 `m_text_Extraction`（默认隐藏；倒计时中绿字 `EXTRACTING xx.x s`，暂停时红字 `EXTRACTION PAUSED - ENEMY IN ZONE`）
- [x] 101 关（BattleScene_3D_L01）摆放撤离点于 (18, 0, 18)，半径 4m
- [x] Play 实测：进圈倒计时→归零→转场撤离到 SimulationScene，端到端全通（重复验证 3 次）
- [x] Play 实测：圈内生成测试敌人 → `IsPaused=True` 且剩余时间冻结；敌人移除 → 恢复倒计时并正常归零撤离

## 设计决策
- **离开撤离圈 = 倒计时重置**（标准搜打撤做法，回圈从头计时）
- 半径是场景序列化字段（每个撤离点单独可调）；倒计时时长走 `TbLevel.extractionTime`（每关配置）
- 撤离结算与 RETURN_BASE 传送门完全复用同一份代码，保证两条离开路径的结算一致

## 如何新增撤离点 / 调整时长
1. **时长**：编辑 `Configs/GameConfig/Datas/level.xlsx` 的 `extractionTime` 列（秒），跑 `Tools/Luban` 导表
2. **点位**：在战斗场景新建空 GameObject，挂 `ExtractionPointEntity`，Inspector 调 `Radius`；场景视口有 Gizmos 线框圈辅助摆放

## 进行中
（无）

## 待办
- [ ] 撤离点美术资源替换占位圆盘（光圈特效/传送门模型）
- [ ] 多撤离点与点位激活规则（定时开放/条件开放）
- [ ] 撤离倒计时音效与屏幕边缘警示反馈

---

> 状态说明：
> - 当前总状态：✅（核心链路完成并实测通过，美术/规则待迭代）
> - 每次更新后同步 `docs/TODO.md`
