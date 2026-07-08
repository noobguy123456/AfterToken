# 传送门系统（Portal System）设计文档

> 所属模块：场景模块  
> 用途：场景间切换  
> 状态：基础版已完成，新增多战斗场景测试与门上目的地显示；待完整 Play Mode 验证

---

## 1. 功能目标

实现场景间切换的传送门功能，支持以下三种类型：

| 类型 | 说明 | 示例 |
|------|------|------|
| `portal_return_lobby` | 战斗场景返回关卡选择大厅 | 战斗 → Lobby |
| `portal_next_level` | 进入下一关卡 | 关卡 1 → 关卡 2 |
| `portal_custom_scene` | 任意场景自定义跳转 | 战斗 → 特殊场景（商店/营地/Boss房） |

触发方式：**玩家进入传送门区域后按交互键（默认 `E`）触发**。

---

## 2. 配置表设计

### 2.1 `Configs/GameConfig/Datas/portal.xlsx`

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | int | 是 | 配置 ID，场景 Prefab 上通过 `ConfigId` 关联 |
| `type` | string | 是 | 传送门类型：`portal_return_lobby` / `portal_next_level` / `portal_custom_scene` |
| `targetLevelId` | int | 否 | `portal_next_level` 目标关卡 ID，`0` 表示下一关（当前关卡 +1） |
| `targetSceneName` | string | 否 | `portal_custom_scene` 目标场景名 |
| `spawnCondition` | string | 是 | 出现条件：`none` / `all_enemies_defeated` / `boss_defeated` / `item_required` |
| `conditionParam` | string | 否 | 条件参数，如 Boss 的 enemyConfigId、itemId 等 |
| `keepPlayerState` | bool | 是 | 是否保留玩家状态跨场景 |
| `promptText` | string | 否 | 交互提示文本，默认 "按 E 进入传送门" |
| `transitionType` | string | 是 | 转场类型，第一阶段仅 `fade_to_gray` |
| `transitionDuration` | float | 是 | 转场时长（秒），默认 `0.5` |

> 配置表命名与类型值统一采用 `portal_xxx` 形式。

### 2.2 配置运行时适配

- `PortalConfig`：将 Luban 生成的 `cfg.Portal` 转换为运行时配置。
- `PortalConfigMgr`：管理所有 Portal 配置，提供 `Get(int id)` 接口。

---

## 3. 代码结构

```
Assets/GameScripts/HotFix/GameLogic/
├── Config/
│   ├── PortalConfig.cs          # 运行时 Portal 配置
│   └── PortalConfigMgr.cs       # Portal 配置管理器
├── Entity/Portal/
│   └── PortalEntity.cs          # 场景中的传送门实体
├── IEvent/
│   └── IPortalEvent.cs          # 传送门事件接口
├── Portal/
│   ├── Condition/
│   │   ├── IPortalCondition.cs  # 条件扩展接口
│   │   ├── NoneCondition.cs
│   │   └── AllEnemiesDefeatedCondition.cs
│   └── PortalTransitionMgr.cs   # 转场效果管理
├── Procedure/
│   └── PlayerStateContext.cs (class: PortalPlayerState)   # 传送门玩家状态快照
├── System/
│   └── PortalSystem.cs          # 统一管理所有 PortalEntity
└── UI/
    ├── InteractionPromptUI.cs   # 交互提示 UI
    └── TransitionUI.cs          # 转场遮罩 UI
```

---

## 4. 核心类与接口

### 4.1 `PortalEntity`

挂载在场景 Portal Prefab 上，职责：

- 读取 `ConfigId` 加载配置。
- 管理自身激活状态（Active / Inactive）。
- 检测玩家进入/离开触发区。
- 显示/隐藏交互提示。
- 处理交互输入并触发传送流程。
- **新增**：在子物体 `DestinationLabel`（TextMeshPro）上显示目的地名称。

```csharp
public class PortalEntity : MonoBehaviour
{
    [SerializeField] private int _configId;

    public int ConfigId => _configId;
    public bool IsActivated { get; private set; }

    public void Initialize(PortalConfig config);
    public void Activate();
    public void TryInteract();
}
```

### 4.2 `PortalSystem`

战斗流程中创建，挂载在 `BattleRoot` 上，职责：

- 扫描并收集场景中所有 `PortalEntity`。
- 为每个 Portal 加载配置并初始化。
- 订阅 `IEnemyEvent.OnEnemyDied` 等全局事件，评估出现条件。
- 提供查询接口：`GetPortal(int configId)`、`GetAllPortals()`。

### 4.3 `IPortalEvent`

```csharp
[EventInterface(EEventGroup.GroupLogic)]
public interface IPortalEvent
{
    /// <summary>
    /// 传送门被触发，即将切换场景。
    /// </summary>
    void OnPortalTriggered(int configId, string portalType, string targetScene);

    /// <summary>
    /// 传送门从非激活变为激活（条件达成）。
    /// </summary>
    void OnPortalActivated(int configId);

    /// <summary>
    /// 玩家进入传送门触发区域。
    /// </summary>
    void OnPortalEntered(int configId);

    /// <summary>
    /// 玩家离开传送门触发区域。
    /// </summary>
    void OnPortalExited(int configId);
}
```

### 4.4 `IPortalCondition`

预留扩展接口，未来可同步新增条件实现。

```csharp
public interface IPortalCondition
{
    bool Evaluate(PortalEntity portal);
}
```

第一阶段内置实现：

- `NoneCondition`：始终为 true。
- `AllEnemiesDefeatedCondition`：所有敌人死亡后激活。

预留类型（后续实现）：

- `BossDefeatedCondition`
- `ItemRequiredCondition`

### 4.5 `PortalPlayerState`

跨场景保存玩家状态。第一阶段保存：

- 当前 HP / MaxHp
- 当前武器槽位与弹药
- （后续扩展）背包、道具、位置、朝向

```csharp
public static class PortalPlayerState
{
    public static bool HasSavedState { get; set; }
    public static int Hp { get; set; }
    public static int MaxHp { get; set; }
    // public static WeaponStateData[] Weapons { get; set; }

    public static void Save(PlayerSystem playerSystem, WeaponSystem weaponSystem);
    public static void Restore(PlayerSystem playerSystem, WeaponSystem weaponSystem);
    public static void Clear();
}
```

> 用户要求“先把现在有的做好”，因此 HP 和武器弹药优先实现，背包/道具待后续系统完善后扩展。

### 4.6 `PortalTransitionMgr`

- 协调 `TransitionUI` 的渐入/渐出。
- 转场完成后执行场景切换。

### 4.7 UI

- `InteractionPromptUI`：世界空间或屏幕空间提示“按 E 进入传送门”。
- `TransitionUI`：全屏灰色遮罩，通过 `CanvasGroup` 控制 Alpha 渐变。

---

## 5. 流程设计

### 5.1 初始化流程

```
ProcedureBattle.InitializeBattleSystems
    └── _battleRoot.AddComponent<PortalSystem>()
            ├── 扫描 FindObjectsOfType<PortalEntity>()
            ├── 为每个 PortalEntity 加载 PortalConfig
            ├── 根据 spawnCondition 创建 IPortalCondition
            └── 注册全局事件监听（如敌人死亡）
```

### 5.2 交互流程

```
玩家进入 PortalEntity 触发区
    └── PortalEntity.OnTriggerEnter2D
            ├── 检查 IsActivated
            ├── 触发 IPortalEvent.OnPortalEntered
            └── 显示 InteractionPromptUI

玩家按下 E
    └── InputSystem.HandleInteractInput
            └── IBattleInputEvent.OnInteractPressed
                    └── PortalEntity.TryInteract
                            ├── 触发 IPortalEvent.OnPortalTriggered
                            ├── 若 keepPlayerState=true，保存 PortalPlayerState
                            ├── PortalTransitionMgr.PlayFadeToGray
                            └── 切换流程：
                                    ├── portal_return_lobby → GameApp.ChangeProcedure<ProcedureLobby>()
                                    ├── portal_next_level   → BattleContext.CurrentLevelId = target; GameApp.ChangeProcedure<ProcedureBattle>()
                                    └── portal_custom_scene → 复用 ProcedureBattle 加载 targetSceneName（第一阶段）
```

### 5.3 条件激活流程

```
敌人死亡事件 IEnemyEvent.OnEnemyDied
    └── PortalSystem.EvaluateAllConditions
            └── 对每个未激活 Portal 调用 IPortalCondition.Evaluate
                    └── 条件满足 → PortalEntity.Activate()
                            └── 触发 IPortalEvent.OnPortalActivated
```

### 5.4 状态恢复流程

```
ProcedureBattle.ApplyLevelConfig
    └── 若 PortalPlayerState.HasSavedState 且当前 Portal 配置 keepPlayerState=true
            ├── PortalPlayerState.Restore(playerSystem)
            └── PortalPlayerState.Clear()
```

---

## 6. 输入系统扩展

在 `InputSystem` 中新增交互键：

```csharp
[SerializeField] private KeyCode _interactKey = KeyCode.E;

private void HandleInteractInput()
{
    if (Input.GetKeyDown(_interactKey))
    {
        BattleInputEvent?.OnInteractPressed();
    }
}
```

在 `IBattleInputEvent` 中新增：

```csharp
void OnInteractPressed();
```

### 附录：当前 InputSystem 按键映射

| 功能 | 按键 |
|------|------|
| 移动 | WASD |
| 瞄准 | 鼠标位置 |
| 开火 | 鼠标左键 |
| 瞄准按钮（Hold/Toggle） | 鼠标右键 |
| 换弹 | R |
| 武器切换 | 鼠标滚轮 |
| 武器轮盘 | Tab |
| 闪避 | Space |
| 设置 | Escape |
| 切换准星样式 | C |
| **交互（新增）** | **E** |

---

## 7. 资源规划

### 7.1 占位资源

| 资源 | 路径 | 说明 |
|------|------|------|
| Portal Prefab | `Assets/AssetRaw/Actor/Portal_Placeholder.prefab` | 包含 SpriteRenderer（紫色/蓝色圆圈）、CircleCollider2D（Trigger）、PortalEntity 脚本、**DestinationLabel（TextMeshPro，显示目的地）** |
| InteractionPromptUI Prefab | `Assets/AssetRaw/UI/Prefabs/InteractionPromptUI.prefab` | 屏幕空间提示文本 |
| TransitionUI Prefab | `Assets/AssetRaw/UI/Prefabs/TransitionUI.prefab` | 全屏灰色遮罩，带 CanvasGroup |

### 7.2 后续替换

- Portal Prefab → 正式传送门美术 + 粒子特效
- InteractionPromptUI → 正式美术字/图标
- TransitionUI → 正式转场动画

---

## 8. 开发计划

1. **配置表**：新增 `portal.xlsx`，更新 `__tables__.xlsx`，运行 `gen_code_bin_to_project.bat`。
2. **配置适配**：新增 `PortalConfig`、`PortalConfigMgr`。
3. **事件接口**：新增 `IPortalEvent`。
4. **输入扩展**：`InputSystem` 增加 `Interact` 键，`IBattleInputEvent` 增加 `OnInteractPressed`。
5. **条件系统**：新增 `IPortalCondition` 及内置实现。
6. **状态上下文**：新增 `PortalPlayerState`（先实现 HP 与武器弹药）。
7. **转场与提示**：新增 `TransitionUI`、`InteractionPromptUI`、`PortalTransitionMgr`。
8. **核心逻辑**：新增 `PortalEntity`、`PortalSystem`。
9. **流程集成**：`ProcedureBattle` 初始化 `PortalSystem`。
10. **资源与场景**：创建占位 Prefab，在 `BattleScene` 中摆放一个测试 Portal。
11. **文档与验证**：更新本文档，输出验证清单。
12. **多场景测试**：新增 `BattleScene_L01/L02/L03`，配置连闯关传送门，门上显示目的地。

---

## 9. 验证清单

- [x] `portal.xlsx` 生成成功，`cfg.Portal` 与 `cfg.TbPortal` 可用。
- [x] `PortalConfigMgr` 能正确读取配置。
- [x] `InputSystem` 中新增 `E` 键交互输入。
- [x] 场景中放置 `Portal_Placeholder` 并填入 `ConfigId`。
- [x] `PortalSystem` 初始化时扫描到所有 Portal。
- [ ] `none` 条件 Portal 开局激活；`all_enemies_defeated` Portal 在敌人全灭后激活。
- [ ] 玩家进入 Portal 区域显示 `InteractionPromptUI`。
- [ ] 玩家离开 Portal 区域隐藏 `InteractionPromptUI`。
- [ ] 按下 `E` 触发 `IPortalEvent.OnPortalTriggered`。
- [ ] 触发后播放灰色渐变 `TransitionUI`。
- [ ] `portal_return_lobby` 正确切换到 `ProcedureLobby`。
- [ ] `portal_next_level` 正确设置 `BattleContext.CurrentLevelId` 并切换到 `ProcedureBattle`。
- [ ] `keepPlayerState=true` 时玩家 HP 与武器弹药被保留并在新场景恢复。
- [ ] 传送过程中无报错、无资源泄漏。

> 注：涉及 Play Mode 实际运行验证（手动走进传送门、观察场景切换）需由用户在编辑器或真机中完成。

---

## 10. 多战斗场景测试配置

为验证传送门连闯流程，已新增三个测试战斗场景：

| 场景 | 关卡 ID | 传送门 ConfigId | 目的地 | 说明 |
|------|--------|----------------|--------|------|
| `BattleScene_L01` | 1 | 1003 | `BattleScene_L02` | 初始训练场，直接可见激活的传送门 |
| `BattleScene_L02` | 2 | 1004 | `BattleScene_L03` | 废弃工厂，直接可见激活的传送门 |
| `BattleScene_L03` | 3 | 1001 | 返回大厅 | Boss 竞技场，直接可见激活的传送门 |

### 10.1 关卡配置（`level.xlsx`）

| 关卡 ID | 显示名称 | 场景名 | 敌人数量 |
|--------|---------|--------|---------|
| 1 | Training Ground L1 | `BattleScene_L01` | 10 |
| 2 | Abandoned Factory L2 | `BattleScene_L02` | 15 |
| 3 | Boss Arena L3 | `BattleScene_L03` | 20 |

### 10.2 传送门配置（`portal.xlsx`）

| ConfigId | 类型 | 目标 | 出现条件 | 提示文本 | 保留玩家状态 |
|---------|------|------|---------|---------|------------|
| 1003 | `portal_custom_scene` | `BattleScene_L02` | `none` | 按 E 前往关卡2 | false |
| 1004 | `portal_custom_scene` | `BattleScene_L03` | `none` | 按 E 前往关卡3 | false |
| 1001 | `portal_return_lobby` | - | `none` | 按 E 返回大厅 | false |

### 10.3 门上目的地显示

`PortalEntity` 在 `Initialize` 时会为传送门创建/更新 `DestinationLabel` 子物体：

- `portal_return_lobby` → 显示 **"返回大厅"**
- `portal_next_level` → 显示 **"关卡 {targetLevelId}"**
- `portal_custom_scene` → 显示 **"关卡 X"**（将 `BattleScene_LXX` 中的数字提取并本地化显示）

### 10.4 测试路径

1. 从大厅（`LobbyScene`）选择 **Level 1**，进入 `BattleScene_L01`。
2. 移动到右侧传送门，门上显示 **"关卡 2"**，按 **E** 进入 `BattleScene_L02`。
3. 移动到传送门，门上显示 **"关卡 3"**，按 **E** 进入 `BattleScene_L03`。
4. 移动到传送门，门上显示 **"返回大厅"**，按 **E** 回到大厅。

> 当前三个测试场景中的传送门均为 `none` 条件直接激活，便于快速验证场景切换；后续可将 L03 的传送门改为 `all_enemies_defeated` 以测试条件激活。
