# 奖励系统

## 职责

meta 奖励（账号级、直接发放到账）的统一分发入口：撤离结算、任务、订单等奖励都必须经 `RewardSystem.Grant` 发放。

**边界**：局内奖励（开箱/掉落/拾取 → 临时背包 → 撤离成功入仓库、死亡全丢）是搜打撤玩法本体，走 `RunInventory`/`Warehouse` 链路，不经过本系统。

## 已实现

| 类/文件 | 说明 |
|---|---|
| `GameLogic/Shared/RewardSystem.cs` | `RewardSystem.Grant(RewardData, source)` 唯一发放入口；`RewardSystem.Parse("gold:200\|exp:50\|item:10001:2")` 配置串解析 |
| `RewardData` | 奖励内容：Gold / Exp / Items（链式 `AddGold/AddExp/AddItem`） |
| `RewardResult` | 发放结果：实际发放量 + `ItemsGranted` / `ItemsLost`（溢出丢失）+ 来源标识，`ToString()` 汇总文本（供日志与后续结算 UI） |

## 接入方

| 调用方 | 来源标识 | 内容 |
|---|---|---|
| `CrossPlayLink.OnBattleExtracted` | `level:{id}:extract` | TbLevel 的 rewardGold/rewardExp |
| `QuestSystem.TryTurnIn` | `quest:{id}` | TbQuest.Rewards 串（经 `RewardSystem.Parse`） |
| `OrderSystem.TryDeliverOrder` | `order:{configId}` | RewardGold/RewardExp/RewardItems（ItemExchange 逐条转换） |

## 设计要点

- 统一溢出策略：物品经 `InventorySystem` 窄口入仓库，入库失败（仓库满）计入 `RewardResult.ItemsLost` 并告警，不再各处自处理。
- `Grant` 对空奖励直接返回空结果、不打日志。
- 发放即落盘（底层 CurrencySystem/PlayerProfileSystem/Warehouse 各自变动即存）。
- 后续撤离结算画面只需收集各环节 `RewardResult` 做汇总展示（待胜负判定/结算 UI 立项后接入）。
