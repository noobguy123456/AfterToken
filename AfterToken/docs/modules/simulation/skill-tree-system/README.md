# 技能树系统

## 职责

基地技能成长：玩家在经营场景的「技能操作台」NPC 处消耗材料 + 金币升级技能节点，获得战斗/生存/经营三分支的永久加成。

## 资源循环定位

局内掉落/搜刮 → 仓库 → 生产机器加工材料链（木头→强化零件→战术核心→基因样本）→ 技能操作台升级技能。**只能在经营场景升级**（UI 入口只在基地）。

## 结构

| 类/文件 | 说明 |
|---|---|
| `SkillSystem`（Shared/） | 静态类。等级查询 `GetLevel`、加成聚合 `GetEffect(ESkillEffect)`（Σ 等级×每级效果值）、`CanUpgrade`（前置满级/满级/材料/金币四级校验，out 本地化原因）、`TryUpgrade`（双校验后扣材料+金币→升级→落盘→发事件）。存档懒加载 + 变动即存，槽位切换 `InvalidateCache` |
| `SkillConfigMgr`（Config/） | TbSkill 包装：Get / GetByBranch（按 Row/Col 排序）/ All |
| `ISkillEvent`（IEvent/） | `OnSkillUpgraded(skillId, newLevel)` |
| `SkillTreeUI`（UI/Simulation/SkillTreeUI/） | 三分支页签 + 节点网格（Row/Col 摆位 + 前置连线）+ 右侧详情面板（名称/描述/等级/每级效果/当前效果/消耗/升级）+ 失败红字 3 秒。Prefab：`AssetRaw/UI/SkillTreeUI/SkillTreeUI.prefab` |
| 存档段 | `SaveData.skill`（`SkillSaveData{initialized, List<SkillEntry{skillId, level}>}`，只存已学） |

## 配置表

- `skill.xlsx`（TbSkill）：Id/NameKey/DescKey/Branch/MaxLevel/EffectType(ESkillEffect)/EffectValue/CostItems(list ItemExchange)/CostGold/PrereqSkillId(需满级,0=无)/Row/Col/Icon。
- 配套：`item.xlsx` 11001 强化零件/11002 战术核心/11003 基因样本；`production.xlsx` 配方 10-12（工坊产出三种材料）；`battle.xlsx` Drop 5-7（9001/9002 敌人掉落材料）；`npc.xlsx` id=3 Skill Console（`consoleType=1`，场景实体 NpcSkillConsole @(-4,0,-4)）。
- 本地化：`ui.skill.*`（title/branch×3/upgrade/level/effect_per_level/effect_current/cost_title/err.prereq + 12 技能 name/desc 中英）、`ui.interact.skill_console`。

## 效果挂点（ESkillEffect）

| 效果 | 挂点 |
|---|---|
| DamagePct | `BallisticSystem`（Raycast）/`ProjectileSystem`（Projectile）伤害结算，仅玩家开火生效 |
| MaxHpAdd / StaminaMaxAdd | `PlayerSystem.LoadPlayerConfig`（进战斗一次性加到上限） |
| MoveSpeedPct | `PlayerSystem` 每帧移速重算处乘算 |
| DodgeCostPct | `PlayerSystem.GetDodgeStaminaCost`（CanDodge 判定与实际扣耐力同走此窄口） |
| ReloadTimePct / ClipSizeAdd | `WeaponInstance.EffectiveReloadTime` / `EffectiveClipSize` 计算属性；弹药上限/换弹填充/UI 弹药显示/换弹入口判定全部改走 Effective 值（WeaponSystem 为玩家专属单例，安全） |
| GoldGainPct / ExpGainPct | `RewardSystem.Grant` 统一发放口乘算（GM/Narrative 直发不吃加成，有意为之） |
| ProductionSpeedPct | `ProductionSystem.OnTimeAdvanced` 进度推进乘算 |

## 入口与交互

- `NpcSystem` 按 NPC 配置分流：`consoleType==1` → 隐藏交互提示并 `ShowUIAsync<SkillTreeUI>()`（不走对话）；交互提示用 `ui.interact.skill_console`，跟随按键改绑。
- ESC 关闭链接在 `SimulationInputSystem`（BuildingInfoUI 之后）；技能树设计为只在经营场景打开，战斗 ESC 链不含它。

## 坑位

- `GameConfig.cfg` 命名空间自带 `Color` 类，引用 `ESkillEffect` 的文件若已用 `UnityEngine.Color`，要么全限定 `GameConfig.cfg.ESkillEffect`，要么 `using Skill = GameConfig.cfg.Skill;` 别名（SkillTreeUI 用法），不能裸 `using GameConfig.cfg;`。
- npc 类 xlsx 加列后必须在 `__beans__.xlsx` 的 cfg.Npc bean 里补字段行，否则 Luban 静默丢列。
