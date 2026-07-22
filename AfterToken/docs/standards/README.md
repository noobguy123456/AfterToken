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

1. **新功能先设计配置表**：任何涉及数值、表现参数、动画名、颜色、层级、文本格式、Prefab 路径等可变更内容的新功能，先在 `Configs/GameConfig/` 完成 Luban 配置表设计，再编写业务代码；禁止新增硬编码。
2. 私有字段 `_camelCase`，常量 `UPPER_SNAKE_CASE`。
3. 所有 IO / 资源用 `UniTask`，禁止 `Coroutine` / 同步加载。
4. 模块访问用 `GameModule.XXX`。
5. UI Prefab 路径：`Assets/AssetRaw/UI/{Name}/{Name}.prefab`。
6. UI 文字使用 `TextMeshProUGUI`，节点前缀 `m_text_` / `m_tmp_`。
7. 热更资源只放 `Assets/AssetRaw/`，不放 `Assets/Resources/`。
8. `LoadAssetAsync` 必须对应 `UnloadAsset`。
9. Luban 配置工程：`Configs/GameConfig/`；生成脚本：`Configs/GameConfig/gen_code_bin_to_project.bat`；本机 Luban 主程序：`D:\U3D_project\AfterToken\Tools\Luban\Luban.exe`。
