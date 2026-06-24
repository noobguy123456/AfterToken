# 编辑器工具

## 职责

提供开发期辅助工具，加速场景搭建、资源生成和代码刷新。

## 核心工具

| 工具 | 路径/菜单 | 说明 |
|---|---|---|
| `BattleSceneSetup` | `Assets/Editor/BattleSetup/BattleSceneSetup.cs` | `Battle/Setup Battle Scene & Resources` |
| `ForceRecompile` | `Assets/Editor/ForceRecompile.cs` | `Tools/Force Recompile` |
| `TMPPrefabMigrator` | `Assets/Editor/TMPMigration/TMPPrefabMigrator.cs` | `Tools/Migration/Migrate UI Prefabs to TMP` |

## 设计要点

- `BattleSceneSetup` 非破坏性更新已有 Prefab 和场景。
- `ForceRecompile` 用于热更 DLL 修改后强制刷新编辑器编译。
