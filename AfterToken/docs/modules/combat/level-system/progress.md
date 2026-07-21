# Level System 进度

## 已完成
- [x] 硬编码 `LevelConfig` / `LevelConfigMgr`（已改为 Luban 表驱动）
- [x] `TbLevel` Luban 表已定义并生成代码
- [x] `ProcedureLobby` 关卡选择 UI
- [x] `ProcedureBattle` 进入战斗场景
- [x] `LevelConfigMgr` 已替换为 `TbLevel` 驱动
- [x] `ProcedureBattle` 通过 `TbLevel` 配置应用默认武器、玩家血量、敌人数量/半径/配置 ID

## 进行中
- [ ] 波次生成逻辑接入 `TbWave`
- [ ] 胜负判定

## 待办
- [ ] 关卡目标与结算（生存、击杀数量、限时等）
- [ ] 战斗结果事件 `IBattleResultEvent`

## 阻塞
- 等待 `TbWave` 表接入；等待 `ILevelEvent` / `IBattleResultEvent` 事件接口补齐。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
