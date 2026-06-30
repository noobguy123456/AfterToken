# Enemy System 进度

## 已完成
- [x] `EnemyEntity` 基础表现、血量、受伤、死亡
- [x] `EnemySpawnSystem` 占位生成（固定数量/半径）
- [x] `IEnemyEvent` 事件接口
- [x] 敌人头顶血条：背景 + 填充，随血量变色，Prefab/运行时双支持
- [x] `BattleSceneSetup` 创建带血条结构的 Enemy Prefab

## 进行中
- [ ] 敌人 FSM：Idle / Chase / Attack / Dead
- [ ] 敌人 AI 类型与配置接入 `TbEnemy`

## 待办
- [ ] 敌人掉落物 `PickupEntity`
- [ ] 敌人攻击行为与技能
- [ ] 精英/BOSS 差异化行为
- [ ] 血条样式后续可接入配置（颜色、高度、宽度）

## 阻塞
- 等待 `TbEnemy` 表数据补充；敌人 AI 完成后才能接入波次系统。

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
