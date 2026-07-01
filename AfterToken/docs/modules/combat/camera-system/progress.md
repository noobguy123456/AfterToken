# Camera System 进度

## 已完成
- [x] 相机平滑跟随（`LateUpdate` 直接读取玩家 `Transform` + `Rigidbody2D.Interpolate` 插值 + 三种跟随模式：Hard/Exponential/SmoothDamp）
- [x] 边界限制（基础框架）
- [x] FOV / OrthographicSize 动态调整
- [x] 狙击镜 `RenderTexture` 基础集成
- [x] 受击方向指示数据计算
- [x] 受击/开火相机抖动基础接入
- [x] 开火后坐力相机抖动：根据武器 `recoilIntensity` 计算幅度，以上跳为主，指数衰减

## 进行中
- [ ] 狙击镜 RenderTexture 集成优化

## 待办
- [ ] 死亡/胜利镜头表现
- [ ] 多分辨率适配细节
- [ ] 根据实际关卡尺寸实现边界限制

---

> 状态说明：
> - 当前总状态：🟡
> - 每次更新后同步 `docs/TODO.md`
