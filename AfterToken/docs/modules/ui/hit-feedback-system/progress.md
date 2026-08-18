# Hit Feedback System 进度

## 已完成
- [x] DamageNumberUI 伤害飘字
- [x] HitFeedbackUI 8 方向受击指示
- [x] 命中标记
- [x] 修复：开镜（或任意全屏窗口遮挡）时命中标记冻结残留——根因是框架"隐藏"仅切 Ignore Raycast 层（UI 相机仍渲染）且 OnUpdate 停走。现 OnSetVisible(false) 立即清空命中标记与方向指示，ShowHitMarker/ShowDamageIndicator 在隐藏态直接拒绝；HitFeedbackSystem 在开镜狙击时拦截命中标记（由 SniperScopeUI 镜内自绘）

---

> 状态说明：
> - 当前总状态：✅
> - 每次更新后同步 `docs/TODO.md`