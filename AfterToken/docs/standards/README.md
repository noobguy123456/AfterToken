# AfterToken 规范文档

> 项目级权威规范，所有新增代码与资源必须遵循。  
> 若本文档与历史文档冲突，以本目录下规范为准。

---

## 文档导航

| 文档 | 说明 |
|------|------|
| [CODING_STANDARDS.md](./CODING_STANDARDS.md) | C# 代码命名、格式、注释、红线、异步/资源/事件/热更规范 |
| [UI_STANDARDS.md](./UI_STANDARDS.md) | UI Prefab 路径、节点前缀、生命周期、CanvasScaler、TMP 规范 |
| [ASSET_NAMING_STANDARDS.md](./ASSET_NAMING_STANDARDS.md) | `Assets/AssetRaw/` 目录、资源命名、YooAsset 地址规则 |
| [CODE_REVIEW_CHECKLIST.md](./CODE_REVIEW_CHECKLIST.md) | 通用代码 / UI / 资源审查清单 |
| [MIGRATION_PLAN.md](./MIGRATION_PLAN.md) | 现有代码与规范的差异及分批迁移建议 |

---

## 快速红线

1. 私有字段 `_camelCase`，常量 `UPPER_SNAKE_CASE`。
2. 所有 IO / 资源用 `UniTask`，禁止 `Coroutine` / 同步加载。
3. 模块访问用 `GameModule.XXX`。
4. UI Prefab 路径：`Assets/AssetRaw/UI/{Name}/{Name}.prefab`。
5. UI 文字使用 `TextMeshProUGUI`，节点前缀 `m_text_` / `m_tmp_`。
6. 热更资源只放 `Assets/AssetRaw/`，不放 `Assets/Resources/`。
7. `LoadAssetAsync` 必须对应 `UnloadAsset`。
