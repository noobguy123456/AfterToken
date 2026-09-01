# 提案：多语言（本地化）完整方案

> 提案状态：P1 已落地（2026-08-31，Play 实测通过：词条表/LocalizationSystem/Loc/Settings 语言切换均生效）；P2 存量迁移待做
> 提出时间：2026-08-31
> 提案路径：`docs/Proposal/infra/localization.md`
> 关联模块：
> - `Assets/TEngine/Runtime/Module/LocalizationModule/`（框架内置 I2，本提案评估后不复用其数据源）
> - `Assets/GameScripts/HotFix/GameProto/ConfigSystem.cs`（Luban 配表加载）
> - `Assets/GameScripts/Procedure/ProcedureLaunch.cs`（启动语言初始化）
> 关联文档：
> - `docs/Proposal/narrative/dialogue-system.md` / `quest-system.md`（内容文本大户）
> - `docs/Proposal/ui/ui-render-architecture.md`（UI 承载层）

---

## 1. 背景

项目当前**所有文本均为硬编码英文**，分两处：

1. **UI 静态文本**：各 UI prefab 上 TMP_Text 直接写死（Settings、Build、Dialogue、任务追踪 HUD 等）。
2. **Luban 内容文本**：配表列直接写死，如 `quest.xlsx` 的 `name/desc`、`dialoguenode.xlsx` 的 `speaker/text`、`item.xlsx` 的 `name/desc`。

现状无语言切换能力。开发约定目前是"游戏内一律英文"，但任务/对话/道具内容会持续膨胀，多语言框架必须先立起来，否则后期回填成本随词条数线性增长。

### 1.1 框架现状盘点（TEngine 内置 I2）

TEngine 自带一套裁剪版 I2 Localization（非官方插件），机制如下：

| 组件 | 位置 | 作用 |
|---|---|---|
| `LocalizationManager`（MonoBehaviour） | `Module/LocalizationModule/LocalizationManager.cs` | 挂框架游戏物体，Awake 注册 `ILocalizationModule`；启动时 `LoadLanguage(_defaultLanguage, fromInit:true)` 读内置 CSV |
| `ILocalizationModule` | 同目录 | 对游戏层暴露：`Language` 读写、`SetLanguage(enum/string/id)`、`LoadLanguage(name)`、`CheckLanguage` |
| `Localize` 组件 | `Core/Localize.cs` | 挂在 TMP_Text 等旁边，填 `Term` 即按当前语言自动刷新（支持 TMP，见 `Core/Targets/LocalizeTarget_TextMeshPro_UGUI.cs`） |
| `I2Languages.asset` | `Assets/Editor/I2Localization/` | 词条总源（ScriptableObject），Inspector 可增删语言/词条，Spreadsheet 页导入导出 CSV |
| 运行时取词 API | `Core/Manager/LocalizationManager_Translation.cs` | `Localization.LocalizationManager.GetTermTranslation(term)` 静态调用 |
| 分表热更 | 资源名 `I2_<语言名>`（`LocalizationUtility.I2ResAssetNamePrefix`） | 经 YooAsset 加载 CSV 分表 `UseLocalizationCSV` 合入 |

**已存在的游戏侧接线**：`ProcedureLaunch.InitLanguageSettings()` 已从 `PlayerPrefs(Constant.Setting.Language)` 读存档语言并 `localizationModule.Language = language` 设置；不支持的语言回退 English。

**当前缺口**：

- `LocalizationManager.innerLocalizationCsv` **为空**（启动有警告"请使用I2Localization.asset导出CSV创建内置多语言"），词条源资产里没有任何词条；
- GameLogic **零调用**：没有任何代码用 `Localize` 组件或 `GetTermTranslation`；
- Luban 内容表完全在 I2 体系之外。

### 1.2 数据源选型决策

两条可行路线：

| 路线 | 词条维护位置 | 与项目惯例契合度 | 结论 |
|---|---|---|---|
| A. 复用 I2 数据源 | `I2Languages.asset`（编辑器内手工维护，CSV 导入导出） | 与"xlsx → Luban → JSON → YooAsset"工作流**不一致**；词条编辑入口和其他所有配置分离 | 不采用 |
| **B. Luban 词条表** | `localization.xlsx`，和其他表同管线导出热更 | **完全一致**，策划零新工具学习成本 | **采用** |

路线 B 下，I2 模块保留在框架层不动（TEngine 自带，无害），游戏层自建一个等价的薄封装：`LocalizationSystem`（运行时取词 + 语言切换事件）+ `LocTextBinder` 组件（等价 I2 `Localize`，但数据源是 Luban 表）。`ProcedureLaunch` 已有的存档读语言逻辑直接复用，只把设置目标从 I2 换成我们的系统。

---

## 2. 目标

1. 所有面向玩家的文本（UI 静态文本 + 配表内容文本）集中由一张 Luban 词条表驱动，key 化引用。
2. 运行时语言切换即时生效（不重载场景），语言选择持久化。
3. 词条缺失有明确兜底与告警，不炸流程。
4. 工作流与现有配表管线完全一致：改 xlsx → 导表 → SimulateBuild。

非目标：RTL 语言、图片/音频本地化、字体动态切换（中文落进现有 TMP 字体即可，后续有需要再议）。

---

## 3. 详细设计

### 3.1 词条表 `localization.xlsx`

位置：`Configs/GameConfig/Datas/localization.xlsx`， Luban 定义加 `Localization` bean + `TbLocalization`，导出 `cfg_tblocalization.json`，登记进 `ConfigSystem._tableFiles`。

列结构：

| 列 | 类型 | 说明 |
|---|---|---|
| `key` | string（主键） | 词条 key，见 3.3 命名规范 |
| `en` | string | 英文（**必填**，兜底语言） |
| `zh_cn` | string | 简体中文（当前可留空） |
| `zh_tw` | string | 繁体（预留，可留空） |
| `ja` / `ko` | string | 预留列，可留空 |

加新语言 = 加一列 + 在枚举映射里注册，**不需要改任何代码逻辑**。

### 3.2 运行时 `LocalizationSystem`

新建 `Assets/GameScripts/HotFix/GameLogic/System/LocalizationSystem.cs`：

```csharp
public enum GameLanguage { English, ChineseSimplified, ChineseTraditional, Japanese, Korean }

public class LocalizationSystem
{
    public static LocalizationSystem Instance;
    public GameLanguage Current { get; private set; }
    public event Action OnLanguageChanged;          // UI 刷新钩子

    public void Initialize();                        // 游戏启动：读 PlayerPrefs 存档语言
    public void SetLanguage(GameLanguage lang);      // 切换：写存档 + 触发事件
    public string GetText(string key);               // 取词：当前语言列 → 空则回退 en → 再空回退 key 本身并 Log.Warning
    public string GetText(string key, params object[] args);  // string.Format 参数化，支持 {0} {1}
}
```

- 数据源：`ConfigSystem.Instance.Tables.TbLocalization`，启动时建 `Dictionary<key, row>` 索引。
- 语言→列名映射表集中一处（`GameLanguage.ChineseSimplified → "zh_cn"`）。
- `SetLanguage` 写 `PlayerPrefs(Constant.Setting.Language)`，与 `ProcedureLaunch` 现有存档 key 保持一致。

### 3.3 词条 key 命名规范

小写点分，第一段为模块域：

| 前缀 | 用途 | 示例 |
|---|---|---|
| `ui.<panel>.<name>` | UI 静态文本 | `ui.settings.title`、`ui.build.place`、`ui.common.close` |
| `quest.<id>.name/.desc` | 任务名/描述 | `quest.1001.name` |
| `dialogue.node.<id>` / `dialogue.choice.<id>` | 对话节点文本/选项 | `dialogue.node.9001` |
| `item.<id>.name/.desc` | 道具 | `item.10000.name` |
| `npc.<id>.name` | NPC 名 | `npc.1.name` |
| `note.<id>.text` | 小纸条 | `note.1.text` |
| `sys.<name>` | 系统提示/飘字 | `sys.placement.blocked` |

### 3.4 UI 静态文本接入：`LocTextBinder` 组件

新建 `Assets/GameScripts/HotFix/GameLogic/UI/LocTextBinder.cs`（MonoBehaviour，挂在 TMP_Text 同物体）：

- 序列化字段 `key`；`OnEnable` 时 `text = LocalizationSystem.GetText(key)`；
- 订阅 `OnLanguageChanged`，切换时刷新；`OnDisable` 退订；
- 可选 `formatArgs` 不支持（动态文本走代码 `GetText(key, args)`）。

存量 UI prefab 迁移 = 给每个 TMP 文本挂 binder 填 key + 词条表补行。编辑器侧可以补一个小工具扫描 prefab 里所有硬编码 TMP 文本辅助批量回填（P1 可选，量不大手工也行）。

### 3.5 Luban 内容文本接入

内容表的文本列**改存词条 key**，显示层统一过 `GetText`：

| 表 | 改动列 | 读取处改动 |
|---|---|---|
| `quest.xlsx` | `name/desc` 存 `quest.<id>.name/.desc` | `QuestTrackerUI`、任务确认窗、对话任务枢纽显示处加 `GetText` |
| `dialoguenode.xlsx` | `speaker/text/choices` 存 key | `DialogueSystem` 渲染前统一 `GetText` |
| `item.xlsx` | `name/desc` 存 key | 背包/物品提示显示处 |
| `note.xlsx` | `text` 存 key | `NoteSystem` |
| `npc.xlsx` | `name` 存 key | NPC 头顶/对话框 |

要点：**改动集中在显示层的少数几个读取点**，表结构只改列含义不改列数，Luban bean 定义不用动。`choices` 列是 `选项1|选项2` 复合格式，key 化后存 `key1|key2`，解析后逐个 `GetText`。

### 3.6 语言切换 UI 与持久化

- Settings → General 页签加 Language 下拉（English / 简体中文 / 繁體中文 / 日本語 / 한국어，按词条表实际有内容的列动态过滤）。
- 选择即 `LocalizationSystem.SetLanguage`，全 UI 经 `OnLanguageChanged` 即时刷新，不需要关面板。
- `ProcedureLaunch.InitLanguageSettings` 改造：读存档后同时设置 `LocalizationSystem`（原 I2 设置保留无妨）。

### 3.7 兜底与告警

- key 不存在：`Log.Warning("[Loc] missing key: xxx")`，返回 `key` 原文（开发期一眼可辨）。
- 当前语言列为空：回退 `en`；`en` 也空：同上按缺 key 处理。
- 编辑器下可加一个校验菜单项：扫描全部 prefab 的 `LocTextBinder.key` + 内容表文本列，对词条表做全量存在性检查，导表后跑一遍。

### 3.8 工作流（策划视角）

1. 在 `localization.xlsx` 加一行：key + 英文（中文后补）。
2. 内容表文本列填 key。
3. 跑 Luban 导表 → `YooAsset.EditorSimulateModeHelper.SimulateBuild("DefaultPackage")`。
4. 进游戏即生效；切语言即时刷新。

与现有配表流程零差异。

---

## 4. 风险与决策

| 风险 | 应对 |
|---|---|
| 中文列暂空时切中文看到一片英文 | 兜底回退 en 是设计行为；中文列随内容补充逐步回填 |
| 动态拼接文本（"Kill 3/5"）参数顺序跨语言不同 | `GetText(key, args)` 用 `{0}` 占位，词条里可调整顺序 |
| 词条量增长后 xlsx 单行过长 | 对话长文本允许 `\n`；超长剧情文本后续再评估独立表 |
| I2 与本方案并存造成困惑 | 文档明确：游戏层禁用 `Localize` 组件与 `GetTermTranslation`，统一走 `LocalizationSystem`；I2 仅作框架底层保留 |
| TMP 中文缺字 | 已解决：思源黑体 `Assets/Fonts/SourceHanSans-Regular SDF.asset`（Dynamic 图集）已设为 TMP 默认字体，中英文均覆盖 |

---

## 5. 实施步骤

- **P1 框架落地**：`localization.xlsx` + Luban 定义 + 导表登记；`LocalizationSystem`；`LocTextBinder`；Settings 语言下拉；`ProcedureLaunch` 接线。验收：造 2~3 个测试词条，运行时切换语言看到 UI 文本即时变化。
- **P2 存量迁移**：UI prefab 静态文本全部 key 化（Settings/Build/Dialogue/QuestTracker/HUD 等）；quest/dialoguenode/item/note/npc 内容列改 key。验收：游戏内无硬编码文本残留（grep 检查），英文表现与迁移前一致。
- **P3 中文回填**：补 `zh_cn` 列；验证 TMP 字体中文渲染；设置里切中文全量走查。

---

## 6. 结论

采用**路线 B：Luban 词条表 + 自建薄封装**，理由是与项目配表驱动惯例完全一致、热更链路现成、策划工作流零变化。TEngine 内置 I2 保留在框架层但不作为游戏数据源。P1 落地后即具备完整多语言骨架，后续加语言只是加列。
