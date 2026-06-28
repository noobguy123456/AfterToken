# 08 游戏数据联通与调试方法

本文档说明 AfterToken 项目中**配置数据**与**运行时战斗数据**的流转路径，以及常用的调试手段。

---

## 一、配置数据流

所有静态数值（武器、关卡、敌人、掉落等）都通过 Luban 配置表管理，数据流转如下：

```
策划修改 Excel
    │
    ▼
Configs/GameConfig/Datas/*.xlsx
    │
    ▼
运行 gen_code_bin_to_project.bat
    │
    ├──► C# 代码
    │    Assets/GameScripts/HotFix/GameProto/GameConfig/cfg/
    │    例如：TbWeapon.cs / Weapon.cs / WeaponType.cs
    │
    └──► JSON 数据
         Assets/AssetRaw/Configs/json/
         例如：cfg_tbweapon.json
    │
    ▼
YooAsset 打包
    （AssetBundleCollector 的 Configs 组已收集 AssetRaw/Configs）
    │
    ▼
游戏运行时
    │
    ▼
ConfigSystem.Instance.Tables
    │
    ▼
业务代码读取（WeaponConfigMgr / LevelConfigMgr / 各 System）
```

### 懒加载

`ConfigSystem` 在**首次访问 `Tables` 属性**时，才会通过 YooAsset 的 `IResourceModule` 加载所有 JSON 文件。因此如果游戏启动后立刻查看配置表，需要确保有代码先触发了一次访问。

---

## 二、运行时战斗数据流

以**玩家开枪命中敌人**为例：

```
InputSystem（收集输入）
    │
    ▼
WeaponSystem（处理开火、切换、瞄准）
    │
    ├──► WeaponInstance（判断 CD、弹药、换弹）
    │
    └──► 触发 IWeaponEvent.OnFire
              │
              ▼
        BallisticSystem / ProjectileSystem
              │
              ├──► 射线检测或生成投掷物
              │
              └──► 命中后触发 IDamageEvent.OnDamage
                          │
                          ▼
                    BattleSystem（计算最终伤害）
                          │
                          ▼
                    EnemyEntity（扣血、播放受击动画）
                          │
                          ▼
                    触发 UI 事件
                    DamageNumberUI / HitFeedbackUI
```

所有跨系统通信都通过 `GameEvent.Get<T>()` 事件总线完成，数据以事件参数形式传递。

---

## 三、调试配置数据的方法

### 1. 直接查看生成的 JSON

JSON 文件是配置数据的最终形态，路径：

```
Assets/AssetRaw/Configs/json/
```

用 VS Code / Rider 直接打开即可验证数值，例如 `cfg_tbweapon.json`：

```json
{
  "id": 1001,
  "name": "Pistol",
  "damage": 15,
  "fireRate": 5,
  ...
}
```

### 2. 运行时打印配置

在任意业务代码中：

```csharp
var weapon = ConfigSystem.Instance.Tables.TbWeapon.Get(1001);
Log.Debug($"[Debug] 武器 {weapon.Name} 伤害 {weapon.Damage}");

var level = ConfigSystem.Instance.Tables.TbLevel.DataList[0];
Log.Debug($"[Debug] 关卡 {level.DisplayName} 敌人数量 {level.EnemyCount}");
```

### 3. 通过适配管理器打印

武器和关卡保留了旧的访问接口：

```csharp
var weapon = WeaponConfigMgr.Instance.Get(1001);
Log.Debug($"[Debug] 武器名 {weapon.name}, 射速 {weapon.fireRate}");

var level = LevelConfigMgr.Instance.Get(1);
Log.Debug($"[Debug] 关卡 {level.displayName}, 玩家血量 {level.playerMaxHp}");
```

### 4. Unity 断点调试

热更代码在 Editor 模式下**可以直接打断点**：

| 调试目标 | 推荐断点位置 |
|---------|-------------|
| 配置是否加载 | `WeaponConfigMgr.EnsureLoaded()` / `LevelConfigMgr.EnsureLoaded()` |
| 开火事件 | `BallisticSystem.OnFire()` / `ProjectileSystem.OnFire()` |
| 伤害计算 | `BattleSystem` 伤害处理函数 |
| 敌人扣血 | `EnemyEntity.TakeDamage()` |
| UI 更新 | `DamageNumberUI.ShowDamage()` / `HitFeedbackUI.ShowHit()` |

### 5. 检查 YooAsset 收集

打开收集器配置：

```
Assets/Editor/AssetBundleCollector/AssetBundleCollectorSetting.asset
```

确认：

- `Configs` 组包含 `Assets/AssetRaw/Configs`
- `FilterRuleName` 为 `CollectAll`
- `AddressRuleName` 为 `AddressByFileName`

这样 `cfg_tbweapon.json` 等文件会被自动打包，运行时地址为 `cfg_tbweapon`（ConfigSystem 已做 `.json` 后缀回退）。

### 6. 通过 Inspector 临时暴露

配置类不是 `MonoBehaviour`，无法直接在 Inspector 查看。但可以在关键系统上加 `[SerializeField]` 字段临时查看：

```csharp
public class WeaponSystem : MonoBehaviour
{
    [SerializeField] private int _debugWeaponId = 1001;

    private void Start()
    {
        var weapon = WeaponConfigMgr.Instance.Get(_debugWeaponId);
        Log.Debug(weapon != null ? weapon.name : "武器配置为 null");
    }
}
```

### 7. 快速验证改表是否生效

1. 修改 `Configs/GameConfig/Datas/` 下对应 Excel。
2. 运行 `Configs/GameConfig/gen_code_bin_to_project.bat`。
3. 回到 Unity 等待编译完成。
4. 运行游戏进入战斗。
5. 在 `BallisticSystem` 或 `BattleSystem` 处打断点，查看 `config.damage` 等字段。

---

## 四、调试运行时战斗数据的方法

### 1. 事件监听

在任意 MonoBehaviour 中订阅事件，打印中间数据：

```csharp
private void OnEnable()
{
    GameEvent.Get<IWeaponEvent>().Subscribe(IWeaponEvent_Event.OnFire, OnFire);
    GameEvent.Get<IDamageEvent>().Subscribe(IDamageEvent_Event.OnDamage, OnDamage);
}

private void OnDisable()
{
    GameEvent.Get<IWeaponEvent>().Unsubscribe(IWeaponEvent_Event.OnFire, OnFire);
    GameEvent.Get<IDamageEvent>().Unsubscribe(IDamageEvent_Event.OnDamage, OnDamage);
}

private void OnFire(Vector2 origin, Vector2 direction, int weaponConfigId, int ownerId)
{
    Log.Debug($"[Debug] 开火 origin={origin}, weaponId={weaponConfigId}");
}

private void OnDamage(int targetId, int attackerId, int damage, bool isCritical)
{
    Log.Debug($"[Debug] 伤害 target={targetId}, damage={damage}, crit={isCritical}");
}
```

### 2. Gizmos 可视化

在 `BallisticSystem`、`EnemySpawnSystem` 等系统中开启 `OnDrawGizmos`，可直观看到射线、范围等：

```csharp
private void OnDrawGizmos()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, _spawnRadius);
}
```

### 3. 日志过滤

使用 `[Debug]` 前缀统一日志，方便在 Console 中过滤：

```csharp
Log.Debug($"[Debug-Weapon] {weapon.name} 射击");
Log.Debug($"[Debug-Damage] {damage}");
```

---

## 五、GM 调试工具

项目内置了一套 GM 控制台和 GM 面板，方便运行时快速调试。

### 使用条件

GM 工具仅在以下环境编译和生效：

- **Unity Editor**
- **Development Build**（构建设置中勾选 Development Build）

Release Build 中不会包含任何 GM 代码。

实现方式：所有 GM 相关代码用 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 条件编译包裹。

### 文件位置

```
Assets/GameScripts/HotFix/GameLogic/GM/
├── GMController.cs
```

`ProcedureBattle` 在创建 `BattleRoot` 时会自动挂载 `GMController`。

### 快捷键

| 按键 | 功能 |
|------|------|
| `` ` ``（反引号） | 打开/关闭 GM 控制台 |
| `F1` | 打开/关闭 GM 面板 |
| `ESC` | 关闭所有 GM 窗口 |
| `Enter` | 控制台中执行命令 |

### GM 控制台命令

| 命令 | 示例 | 说明 |
|------|------|------|
| `help` | `help` | 显示所有命令 |
| `weapon <id>` | `weapon 1001` | 装备指定武器 |
| `hp <value>` | `hp 1000` | 设置当前血量 |
| `maxhp <value>` | `maxhp 99999` | 设置最大血量并回满 |
| `ammo <value>` | `ammo 999` | 设置当前弹药 |
| `god` | `god` | 开启无敌模式 |
| `killall` | `killall` | 杀死所有敌人 |
| `spawn <enemyId>` | `spawn 9001` | 在玩家附近生成敌人 |
| `time <scale>` | `time 0.2` | 设置时间缩放 |
| `level <id>` | `level 2` | 切换关卡 |
| `reload` | `reload` | 重新加载配置表 |
| `clear` | `clear` | 清空控制台 |

### GM 面板

按 `F1` 打开可视化按钮面板，包含常用功能：

- **玩家**：满血、无敌
- **武器**：手枪 / 冲锋枪 / 步枪 / 狙击枪 / 火箭筒、满弹药
- **战斗**：杀死所有敌人、生成敌人
- **时间 / 关卡**：慢动作、正常速度、切换关卡
- **配置**：重载配置表

### 重载配置

点击面板上的「重载配置」或输入 `reload`，会重新执行：

```csharp
ConfigSystem.Instance.Reload();
WeaponConfigMgr.Instance.Reload();
LevelConfigMgr.Instance.Reload();
```

> 注意：重载配置只会影响后续读取，当前已创建的对象（如正在持有的武器、已生成的敌人）不会自动刷新。

---

## 六、常见问题排查

| 现象 | 可能原因 | 排查方法 |
|------|---------|---------|
| 配置读出来是 null | `ConfigSystem` 未触发加载；YooAsset 未收集 JSON | 先访问一次 `ConfigSystem.Instance.Tables`；检查收集器配置 |
| 改表后游戏数值没变 | 未重新生成代码/数据；未重新打包 | 运行生成脚本；查看 JSON 是否更新 |
| 数值和 Excel 不一致 | 适配类转换错误；字段名大小写问题 | 检查 `WeaponConfig` / `LevelConfig` 构造函数 |
| 找不到某张表 | `__tables__.xlsx` 注册错误；input 路径/Sheet 名错误 | 检查表注册和 `Sheet名@文件名` 格式 |
| 事件没触发 | 未订阅事件；System 未挂载 | 检查 `_eventMgr.AddEvent` 和 `AddComponent` |
| 敌人不生成 | `EnemySpawnSystem.Initialize` 未调用；YooAsset 找不到敌人 Prefab | 检查 `ProcedureBattle.ApplyLevelConfig`；确认 `Enemy` 资源已收集 |
| GM 面板没反应 | 当前不是 Development Build | 检查构建设置；Editor 模式下一定可用 |

---

## 七、推荐调试流程

当发现一个数据异常时，建议按以下顺序排查：

1. **Excel 数据是否正确** → 打开 `.xlsx` 检查。
2. **JSON 是否生成正确** → 打开 `Assets/AssetRaw/Configs/json/` 对应文件。
3. **代码中读取是否正确** → 在 `ConfigSystem` 或对应 `Mgr` 处打断点。
4. **运行时事件是否正常** → 订阅相关事件打印日志。
5. **UI 表现是否正确** → 在 UI 类中打印接收到的数值。
6. **需要快速验证** → 使用 GM 控制台或面板修改数值。
