# NPC System 进度

## 已完成
- [x] Luban 新表 `TbNpc`（map 模式，字段 id/name/role/dialogueId）+ 2 条英文测试数据（1=Quartermaster/Supply Officer、2=Doc/Field Medic）；`ConfigSystem._tableFiles` 白名单（CustomTemplate 模板与生成拷贝）同步加 `cfg_tbnpc`
- [x] `NpcConfigMgr`（镜像 `NoteConfigMgr`）
- [x] `NpcEntity`（SphereCollider trigger 1.5m + 钢蓝色胶囊占位视觉 + 头顶 TMP 名字牌，`BillboardFaceCamera` 朝向相机；名牌字号 2、缩放 0.75，俯视下可读）
- [x] `NpcSystem`（E 键交谈占位、提示 UI、出区收起提示），`ProcedureSimulation.InitializeSimulationSystems` 已挂载
- [x] `INpcEvent.OnNpcTalked(int npcId)` 事件接口（GroupLogic），对话系统接线点
- [x] SimulationScene 摆放两个测试 NPC（`NPC_Quartermaster` (-3,0,3)、`NPC_Doc` (3,0,-4)），场景已保存
- [x] Play 实测（2026-08-25）：靠近出 "Press E to Talk" 提示 → E 触发 `[NpcSystem] 与 NPC 交谈: Quartermaster (id=1)` 日志 → 名字牌显示正确；Console 0 错误

## 进行中
（无）

## 待办
- [ ] 接对话系统（方案见 `docs/Proposal/narrative/dialogue-system.md`，pending；落地时消费 `INpcEvent.OnNpcTalked` + `dialogueId` 字段）
- [ ] NPC 美术资源替换占位胶囊
- [ ] NPC 朝向玩家（交谈时转身）
- [ ] 统一 IInteractable 仲裁器（Portal/Note/NPC/容器触发区重叠时）

---

> 状态说明：
> - 当前总状态：🟡（底座可用，对话系统未接）
> - 每次更新后同步 `docs/TODO.md`
