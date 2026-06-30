# Level System 进度

## 已完成
- [x] 硬编码 `LevelConfig` / `LevelConfigMgr`
- [x] `TbLevel` Luban 表已定义并生成代码
- [x] `ProcedureLobby` 关卡选择 UI
- [x] `ProcedureBattle` 进入战斗场景

## 进行中
- [ ] 波次生成逻辑接入 `TbWave`
- [ ] 胜负判定

## 待办
- [ ] 替换硬编码 `LevelConfigMgr` 为 `TbLevel`
- [ ] 关卡目标与结算（生存、击杀数量、限时等）
- [ ] 战斗结果事件 `IBattleResultEvent`

## 阻塞
- 等待 `TbWave` 表数据补充；等待 `ILevelEvent` / `IBattleResultEvent` 事件接口补齐。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
