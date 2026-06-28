# AfterToken 代码规范（权威版）

> **状态**：项目级权威代码规范  
> **适用范围**：所有 C# 脚本（`Assembly-CSharp`、`GameLogic`、`GameProto`、`TEngine.*`、`Launcher`、`Editor`）  
> **关联文档**：
> - [规范索引](./README.md)
> - [UI 规范](./UI_STANDARDS.md)
> - [资源命名规范](./ASSET_NAMING_STANDARDS.md)
> - [代码审查清单](./CODE_REVIEW_CHECKLIST.md)
> - [迁移计划](./MIGRATION_PLAN.md)
> - 详细命名参考：`.claude/skills/tengine-dev/references/naming-rules.md`（已被本规范收编，仅作细节速查）

---

## 1. 命名约定

### 1.1 类型与接口

| 类型 | 规范 | 示例 |
|------|------|------|
| 类 / 结构体 / 枚举 | `PascalCase` | `PlayerEntity`、`WeaponType` |
| 接口 | `I` + `PascalCase` | `IPlayerEvent`、`IResourceModule` |
| 枚举成员 | `PascalCase` | `FireMode.Single`、`UILayer.Top` |
| 泛型参数 | `T`、`T1`、`T2`… | `Debug<T1, T2>` |
| 命名空间 | `PascalCase`，按程序集分层 | `GameLogic`、`TEngine` |

### 1.2 成员

| 成员 | 规范 | 示例 |
|------|------|------|
| 方法 | `PascalCase` | `TakeDamage`、`LoadDataAsync` |
| 属性 | `PascalCase` | `CurrentWeapon`、`MoveDirection` |
| 事件 | `PascalCase`，`OnXxx` 前缀 | `OnDead`、`OnPlayerCreated` |
| 常量 | `UPPER_SNAKE_CASE` | `MAX_WEAPON_SLOTS`、`LAYER_DEEP` |
| 公共字段 | 推荐改为属性；若必须公开，用 `PascalCase` | `public int Damage;`（仅在数据/序列化类中允许） |
| 私有 / 受保护实例字段 | `_camelCase` | `_animator`、`_currentSlot` |
| 私有 / 受保护静态字段 | `_camelCase` | `_singletons`、`_instance` |
| 公共静态字段 / 单例 | `PascalCase` | `WeaponSystem.Instance` |
| `readonly` 字段 | 与对应字段规则一致 | `private readonly GameEventMgr _eventMgr` |
| 参数 | `camelCase` | `damageInfo`、`slot` |
| 局部变量 | `camelCase` | `weapon`、`config` |
| 异步方法 | `PascalCase` + `Async` 后缀 | `LoadDataAsync`、`CreatePlayerAsync` |
| 事件回调 | `PascalCase` + `On` 前缀 | `OnHpChanged`、`OnMoveInput` |

### 1.3 UI 节点绑定变量

- Prefab 节点名使用 `m_` 前缀（与 `ScriptGeneratorSetting.asset` 的 regex 保持一致），例如 `m_text_Title`、`m_btn_Start`。
- 脚本中绑定变量使用 `_camelCase` 去掉 `m_` 前缀，例如：

```csharp
private TextMeshProUGUI _textTitle;
private Button _btnStart;

protected override void ScriptGenerator()
{
    _textTitle = FindChildComponent<TextMeshProUGUI>("m_text_Title");
    _btnStart = FindChildComponent<Button>("m_btn_Start");
}
```

- 旧代码中残留的 `m_textTitle` 等字段名属于历史债务，新代码必须遵循 `_camelCase`。

### 1.4 TEngine 特定类型命名

| 类型 | 规范 | 示例 |
|------|------|------|
| 模块接口 | `IXxxModule` | `IResourceModule` |
| 模块实现 | `XxxModule` | `ResourceModule` |
| 事件接口 | `IXxxEvent` / `IXxxUI` + `[EventInterface]` | `IPlayerEvent` |
| UIWindow 子类 | `XxxUI` / `XxxWindow` | `BattleMainUI` |
| UIWidget 子类 | `XxxWidget` / `XxxItem` | `SkillSlotWidget` |
| Procedure 流程 | `ProcedureXxx` | `ProcedureMainMenu` |
| 状态机状态 | `XxxState` | `IdleState` |
| 系统类 | `XxxSystem` | `PlayerSystem` |
| 配置表（Luban） | `TbXxx`（表）/ `Xxx`（行） | `TbItem` / `Item` |
| 内存池对象 | 实现 `IMemory` | `DamageInfo : IMemory` |

---

## 2. 格式与排版

### 2.1 基本规则

- **缩进**：4 个空格，禁用 Tab。
- **换行符**：LF。
- **编码**：UTF-8，无 BOM。
- **行尾空格**：自动裁剪（`trim_trailing_whitespace = true`）。
- **文件末尾**：保留一个空行（`insert_final_newline = true`）。
- **行宽**：建议 ≤ 180 字符（`.editorconfig` 已配置 ReSharper）。

### 2.2 大括号与换行

- 使用 **Allman 风格**：开括号独占一行。

```csharp
public void PlayAnimation(string stateName)
{
    // ...
}

if (damageInfo == null)
{
    return;
}
```

- 单行 `if` 守卫允许省略大括号，但推荐保留大括号以降低维护成本：

```csharp
// 允许
if (damageInfo == null) return;

// 推荐
if (damageInfo == null)
{
    return;
}
```

### 2.3 空格

- 控制流关键字后加空格：`if (`、`for (`、`while (`、`switch (`、`catch (`、`using (`。
- 二元运算符两侧加空格：`=`、`==`、`+`、`-`、`*`、`/`、`??`、`=>`。
- 逗号后加空格：

```csharp
public void Foo(int a, int b, int c) { }
```

- 空成员体可写在一行：`public void Foo() { }`

### 2.4 访问修饰符顺序

```csharp
private static List<Assembly> _hotfixAssembly;
private static readonly List<ISingleton> _singletons = new();
private readonly GameEventMgr _eventMgr = new();
public const int MAX_WEAPON_SLOTS = 3;
```

顺序：**访问修饰符 → `static` → `readonly` / `const` → 类型 → 名称**。

### 2.5 属性写法

- 自动属性统一顺序：`get; private set;` / `protected set;`。

```csharp
public Vector2 MoveDirection { get; private set; }
public bool IsAiming { get; protected set; }
```

- 只读属性优先使用表达式体：

```csharp
public WeaponInstance CurrentWeapon => _slots[_currentSlot];
public static WeaponConfigMgr Instance => _instance ??= new();
```

---

## 3. `using` 顺序

```csharp
// 1. System
using System;
using System.Collections.Generic;

// 2. Unity / 第三方
using UnityEngine;
using UnityEditor;
using TMPro;
using Cysharp.Threading.Tasks;

// 3. 项目命名空间
using TEngine;
using GameLogic;

// 4. 别名（放在最后）
using Object = UnityEngine.Object;
```

- 按 System → Unity/第三方 → 项目命名空间 → 别名分组。
- 组内按字母顺序排序。
- 条件编译 `#if` 包裹的 using 可放在对应分组内。

---

## 4. 注释与文档

### 4.1 XML 文档

以下公共 API 必须写 `///` 注释：

- `GameModule` 及其自定义模块的公共方法/属性。
- `UIWindow` / `UIWidget` 子类的公共方法（生命周期方法除外，但鼓励写）。
- 事件接口 `IXxxEvent` 及其所有方法。
- `XxxSystem` 的公共方法。
- 配置表/工具类的公共字段/方法。

示例：

```csharp
/// <summary>
/// 玩家实体。
/// 负责玩家表现、动画、物理移动；逻辑由 PlayerSystem 驱动。
/// </summary>
public class PlayerEntity : MonoBehaviour
{
    /// <summary>
    /// 当前移动方向，由输入系统每帧写入。
    /// </summary>
    public Vector2 MoveDirection { get; private set; }
}
```

### 4.2 行内注释

- 使用中文注释。
- 复杂逻辑必须说明“为什么”，而非重复“做了什么”。
- TODO/FIXME 使用统一格式，并尽量关联 issue 或模块进度：

```csharp
// TODO: 接入 Luban 配置表后移除硬编码
// FIXME: 闪避期间不覆盖速度；由 StartDodge/EndDodge 管理
```

---

## 5. C# 语言风格

### 5.1 推荐使用

```csharp
// var 仅在类型显而易见时使用
var player = GetPlayer();
var weaponList = new List<WeaponInstance>();

// 表达式体成员
public int GetSelectedSlot() => _selectedSlot;

// 模式匹配 / switch 表达式
string animName = stateName switch
{
    "Idle" => "Player_Idle",
    "Move" => "Player_Run",
    _ => null,
};

// 空合并赋值
_instance ??= new WeaponConfigMgr();

// 字符串插值
Debug.Log($"[BattleSystem] 命中非敌人目标: {damageInfo.TargetGameObject.name}");
```

### 5.2 限制使用

- `var` 不用于基础类型字面量：`int count = 0` 优于 `var count = 0`。
- 避免过度使用表达式体，复杂逻辑仍使用常规方法体。

### 5.3 空检查

```csharp
// 推荐
if (damageInfo == null) return;

// 可选（Unity 对象判断）
if (target == null) return;

// 仅在逻辑对象时使用
if (config?.Id == null) return;
```

Unity 对象销毁后 `== null` 已被重载，优先使用 `== null` 而非 `is null` / `ReferenceEquals`。

---

## 6. 核心编码红线

1. **异步优先**：所有 IO / 资源 / 网络 / 耗时操作用 `UniTask`，禁止 `Task`、`Coroutine`、同步加载。
2. **模块访问**：使用 `GameModule.XXX`，禁止直接 `ModuleSystem.GetModule<T>()`。
3. **资源必须释放**：
   - `LoadAssetAsync` 对应 `UnloadAsset`。
   - `LoadGameObjectAsync` 实例化的 GameObject，Destroy 时自动释放，无需手动 `UnloadAsset`。
   - 禁止静态持有 Asset 引用。
4. **热更边界**：
   - `Assets/GameScripts/Main`（`Assembly-CSharp`）不热更。
   - `Assets/GameScripts/HotFix/`（`GameLogic` / `GameProto`）全部热更。
5. **事件解耦**：
   - 模块间通信用 `GameEvent`。
   - UI 内部用 `AddUIEvent`。
   - 禁止跨模块强引用、禁止外部访问 UI 私有组件。
6. **UI Prefab 路径**：所有热更域 UI Prefab 放在 `Assets/AssetRaw/UI/{Name}/{Name}.prefab`，禁止放 `Assets/Resources/`。
7. **禁止模式**：
   - `Resources.Load` → `GameModule.Resource`
   - `Instantiate(prefab)` → `LoadGameObjectAsync`
   - `FindObjectOfType` / `FindObjectsOfType` → `GameModule` / `FindObjectsByType`（仅在 Editor 工具中允许）
   - `Update` 中 `new` 对象 → `MemoryPool`
   - 忽略 `UniTask` 返回值 → 必须 `await` 或 `.Forget()`

---

## 7. 异步与资源编程

### 7.1 UniTask

```csharp
public async UniTask<int> GetDataAsync() { }
public async UniTaskVoid StartBattleAsync() { } // 调用方加 .Forget()

// 取消令牌防止销毁后回调
private CancellationTokenSource _cts = new();
protected override void OnDestroy()
{
    _cts.Cancel();
    _cts.Dispose();
}

// 并发加载
var (a, b, c) = await UniTask.WhenAll(LoadA(), LoadB(), LoadC());
```

### 7.2 资源加载

```csharp
// 实例化 GameObject（自动管理生命周期）
var go = await GameModule.Resource.LoadGameObjectAsync("Player", _spawnPoint);

// 加载资源
var asset = await GameModule.Resource.LoadAssetAsync<Texture2D>("Icon");

// 卸载
GameModule.Resource.UnloadAsset(asset);
GameModule.Resource.UnloadUnusedAssets();
```

---

## 8. 程序集与目录边界

| 程序集 | 目录 | 用途 |
|--------|------|------|
| `Assembly-CSharp` | `Assets/GameScripts/` | 启动入口、流程加载、热更加载器 |
| `GameLogic` | `Assets/GameScripts/HotFix/GameLogic/` | 游戏逻辑、UI、系统、事件接口 |
| `GameProto` | `Assets/GameScripts/HotFix/GameProto/` | 配置表/协议基础库（含 Luban 生成代码） |
| `TEngine.Runtime` | `Assets/TEngine/Runtime/` | 框架运行时 |
| `TEngine.Editor` | `Assets/TEngine/Editor/` | 框架编辑器工具 |
| `Launcher` | `Assets/Launcher/` | 启动器 |

- `GameScripts/Main` 代码不能引用 `GameScripts/HotFix/` 类型。
- `GameLogic` 可以引用 `TEngine`、`GameProto`、Unity 引擎 API。

---

## 9. 文件组织

- **一个文件一个主要类/接口/枚举**。
- 小型关联接口可放在同一文件（如 `ISingleton`、`IUpdate` 等）。
- `partial class` 按功能拆分文件时，命名格式：`ClassName.Feature.cs`。
- 命名空间必须显式声明，不使用文件作用域命名空间（`namespace X;`）。

```csharp
namespace GameLogic
{
    public class PlayerEntity : MonoBehaviour
    {
        // ...
    }
}
```

---

## 10. 与旧规范的冲突处理

- 若本文档与 `repowiki/zh/content/开发者指南/代码规范与标准.md`、`naming-rules.md` 或其他历史文档冲突，**以本文档为准**。
- 旧代码中的不一致（`m_` 字段前缀、`private set; get;` 顺序、公共字段命名等）按 [迁移计划](./MIGRATION_PLAN.md) 逐步改造，不在单次提交中大规模重写。
