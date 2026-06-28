# 配置表系统

> 本项目使用 **Luban** 作为配置表工具链。
> 数据输出格式为 **JSON**（`cs-newtonsoft-json` + `json`），便于调试和版本控制。
> 详细配置梳理、已知问题与使用说明见：
> `docs/modules/pipeline/luban-config-system/README.md`

---

## 快速入口

| 内容 | 路径 |
|------|------|
| 配置工程 | `Configs/GameConfig/` |
| Luban 工具链 | `Tools/Luban/` |
| 生成脚本 | `Configs/GameConfig/gen_code_bin_to_project.bat` |
| 生成代码输出 | `Assets/GameScripts/HotFix/GameProto/GameConfig/` |
| 生成数据输出 | `Assets/AssetRaw/Configs/json/` |
| 运行时加载器 | `Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs` |

---

## 使用流程

```
1. 策划在 Configs/GameConfig/Datas/ 下编辑 Excel 配置表
2. 运行 Configs/GameConfig/gen_code_bin_to_project.bat
3. 生成 C# 代码到 GameProto/GameConfig/
4. 生成 .json 数据到 AssetRaw/Configs/json/
5. Unity 自动刷新编译
6. 业务代码通过 ConfigSystem.Instance.Tables.TbXxx 访问配置
```

---

## 注意事项

- 生成代码目录 `GameProto/GameConfig/` **不要手动修改**，下次生成会覆盖。
- `ConfigSystem.cs` 和 `ExternalTypeUtil.cs` 是模板文件，位于 `Configs/GameConfig/CustomTemplate/`，生成时会复制到 `GameProto/`。
- 当前生成脚本使用 `cs-newtonsoft-json` 代码目标，需要项目已安装 `com.unity.nuget.newtonsoft-json`（已安装）。
- 当前 `Tools/Luban/` 需为完整工具链（含 `Luban.runtimeconfig.json` 等）才能运行生成脚本。
