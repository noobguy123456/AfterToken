# Note System 进度

## 已完成
- [x] Luban 新表 `TbNote`（map 模式，字段 id/title/content，content 支持 `\n` 换行）+ 2 条英文测试数据；`ConfigSystem._tableFiles` 白名单（CustomTemplate 模板与生成拷贝）同步加 `cfg_tbnote`
- [x] `NoteEntity`（trigger + 纸白色占位视觉，视觉抬高 0.05m 防地面 z-fighting）
- [x] `NoteSystem`（E 键阅读、提示 UI、死亡闸、走开出区自动关面板），`ProcedureBattle` 已挂载
- [x] `NoteUI` 窗口（640x360 ≈ 1/3 屏，标题 + 正文，不暂停游戏），Esc 关闭链接入（顺序 SettingsUI > BattleBagUI > LootContainerUI > NoteUI）

- [x] `NoteUI.prefab` 生成（`Assets/AssetRaw/UI/NoteUI/`，编辑器脚本一次性生成，640x360 纸色面板 + 标题/正文/Close）
- [x] 101 关（BattleScene_3D_L01）放测试纸条 `Note_1`（noteId=1，(-3,0,3)）
- [x] Play 实测（2026-08-19）：靠近出提示→E 打开面板（标题/换行正文/Close 按钮显示正确）→再按 E 关闭；IsMenuUIOpen 拦截门开/关状态正确；Console 0 错误

## 进行中
（无）

## 待办
- [ ] 纸条美术资源替换占位纸块
- [ ] 已读标记（读后视觉变化/收集计数，接入存档）
- [ ] 统一 IInteractable 仲裁器（Portal/Container/Note 触发区重叠时）

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
