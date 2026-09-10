# 0005. AI 队友 MVP 单队友假设

## 状态

已接受（MVP）

## 背景

AI 队友系统（M1~M6）在立项时只有一名队友（id=1 Exusiai）。代码在多处硬编码了"全场最多一名队友"的假设，review 时识别为扩展性风险，但此时引入多队友抽象属于过度设计。

当前单队友假设的具体位置：

- `CompanionSystem` 单实体持有：`_companion` / `PlayerTransform` / 单 `_brain`，`Companion` 属性只返回一个实例。
- `CompanionConfigMgr.DefaultCompanionId = 1`，`Get()` 默认取 1 号人设卡。
- 字幕/聊天 UI 单呼号归因：`OnCompanionSay(name, text)` 只有名字没有来源 id，多队友时无法区分谁在说话。
- 威胁集合（`Threats`）与感知节拍（`CompanionStateMachineDriver` 单例 `_senseTimer`）按"一个感知者"设计。
- LLM 预算（`LlmBudgetPerRun` / `ControlBudgetPerRun`）按单队友计，多队友时 token 消耗直接翻倍。

## 决策

MVP 阶段维持单队友假设，不做多队友抽象；用本文档记录让位点。

## 后果

- 好处：状态机驱动、字幕、预算控制都无需引入队友 id 维度，逻辑简单可验证。
- 代价：加第二名队友时以下点必须改造——
  1. `CompanionSystem` 改为 `List<CompanionEntity>` + 每队友独立 `CompanionBrain`（或 Brain 支持多 persona）。
  2. `ICompanionEvent.OnCompanionSay` 增加 companionId，字幕按呼号分色/分行。
  3. 感知节拍改为每队友独立计时（当前单例 `_senseTimer` 会被多个实体互相重置）。
  4. LLM 预算需要全局池（否则 N 个队友 = N 倍 token）。
  5. `CompanionConfigMgr.Get()` 调用点全部传入实际 id，废弃 DefaultCompanionId 默认值。
- TbCompanion 表本身已是按 id 多行的结构，数据层无需改动。
