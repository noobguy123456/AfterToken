# Cursor System 进度

## 已完成
- [x] `CursorManager` 光标管理器单例
- [x] 引用计数管理 `ShowCursor` / `HideCursor`
- [x] 强制显示/隐藏 `ForceShowCursor` / `ForceHideCursor`
- [x] `Free` / `Locked` 两种锁定模式
- [x] 自定义光标纹理支持：`SetDefaultCursor` / `SetCursor`
- [x] 战斗流程进入时隐藏并锁定光标
- [x] 菜单/大厅流程进入时显示光标
- [x] `MainMenuUI` / `LobbyUI` / `WeaponWheelUI` 按需显示光标
- [x] `MainMenuUI` 中默认光标纹理生成与设置示例

## 待办
- [ ] 默认光标替换为美术资源并通过 YooAsset 加载
- [ ] 设置 UI 接入光标管理
- [ ] 模拟经营 UI 接入光标管理
- [ ] 窗口化/多分辨率下光标边界处理（如需）

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`
