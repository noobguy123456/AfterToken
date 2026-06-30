# 提案目录

> 本目录存放项目的技术提案、架构演进方案与待评审设计。
> 提案按功能模块分类，与 `docs/modules/` 的目录结构保持一致。
> 已评审通过的提案应同步到对应模块的正式文档（README.md / progress.md）后归档。

---

## 目录结构

```
docs/Proposal/
├── combat/          # 战斗系统相关提案
├── infra/           # 基础设施相关提案
├── pipeline/        # 管线与工具相关提案
├── shared/          # 共享系统 / 跨玩法相关提案
├── simulation/      # 模拟经营相关提案
└── ui/              # UI 相关提案
```

---

## 当前提案列表

### 战斗系统

| 提案 | 路径 | 状态 | 关联模块 |
|---|---|---|---|
| 逻辑子弹与视觉表现分离方案 | [`combat/bullet-logic-visual-separation.md`](./combat/bullet-logic-visual-separation.md) | 待评审 | `combat/projectile-system`、`combat/ballistic-system` |

---

## 提案生命周期

1. **新建提案**：在对应模块子目录下创建 `<proposal-name>.md`，使用本目录下的模板格式。
2. **评审与讨论**：在提案文档中记录评审意见与决策。
3. **同步落地**：将提案中确定的内容同步到：
   - 对应模块的 `docs/modules/<category>/<module>/README.md`
   - 对应模块的 `docs/modules/<category>/<module>/progress.md`
   - 相关总览文档（如 `docs/射击模块实现文档.md`）
4. **归档**：已完全落地且无争议的提案可移动到 `docs/Proposal/archive/<category>/<date>-<proposal-name>.md`。

---

## 提案模板

新建提案时可参考以下结构：

```markdown
# 提案：<提案标题>

> 提案状态：待评审 / 已接受 / 已拒绝 / 已归档
> 提出时间：YYYY-MM-DD
> 提案路径：docs/Proposal/<category>/<proposal-name>.md
> 关联模块：
> 关联文档：

## 1. 背景

## 2. 目标

## 3. 详细设计

## 4. 风险与决策

## 5. 实施步骤

## 6. 结论
```
