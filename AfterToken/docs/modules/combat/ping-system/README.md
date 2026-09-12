# 标点系统（Ping System）

## 职责

玩家在战斗场景中快速标记战场信息（位置/敌人/物资/指令），驱动 AI 队友行为、小地图图标、音效与语音反馈。对标 APEX Legends 的标点体系：**单击上下文快标 + 长按轮盘全类型标点**。

## 现状（已实现）

- `System/PingSystem.cs`（仅战斗场景 BattleRoot 挂载）：中键（`KeyBindAction.Ping`）单击 → 准星指向点上下文分类（敌人 1.0m > 掉落/容器 1.2m > 地面钳制可走点）→ 生成 Move/Loot/Attack 三型标点
- 唯一活动标点（新覆盖旧）、15s 过期、Attack 跟随目标头顶（目标死亡即清）
- 世界标记为占位视觉：彩色 Quad + 脉冲缩放 + billboard
- `IPingEvent.OnPingCreated/OnPingCleared` → AI 队友消费（PingMove/Engage）、小地图消费
- `KeyBindAction.Ping` 可改绑（默认 MMB）

## 目标设计（APEX 式）

### 标点类型

| PingType | 名称（en/zh） | 颜色 | 性质 | AI 队友反应 | 持续时间 |
|---|---|---|---|---|---|
| Move | Go Here / 前往 | 青 | 标记型 | 前往标点（现有 PingMove） | 15s |
| Attack | Enemy / 有敌人 | 红 | 标记型（跟随目标） | 优先交战该目标（现有 Engage） | 目标死亡/15s |
| Loot | Loot Here / 有物资 | 黄 | 标记型 | 语音确认 + 小地图标记（拾取寻路 M3） | 15s |
| Watch | Watch Here / 盯住这里 | 紫 | 指令+标记 | 驻守当前位置并朝向标点（Hold+朝向） | 15s |
| Defend | Defend Here / 防守这里 | 蓝 | 指令+标记 | 前往标点并驻守（Hold at point） | 15s |
| Help | Help / 支援我 | 橙 | 即时指令 | 立即回防玩家（最高优先级召回，打断标点/驻守） | 瞬时（8s 标记） |
| Regroup | Regroup / 集合 | 绿 | 即时指令 | 解除驻守/标点态，恢复跟随 | 瞬时 |
| Retreat | Retreat / 撤退 | 灰 | 即时指令 | 与玩家一同撤离当前交战（压制 Engage 自主开火 10s） | 瞬时 |

分类原则（与 APEX 一致）：**标记型**在世界中留标记供队友/小地图消费；**即时指令型**本质是一条语音+行为命令，标记短暂展示后可消失。

### 轮盘设计（Ping Wheel）

- **触发**：按住标点键 ≥ 0.25s 弹出轮盘；松开确认；单击（<0.25s）保持现有上下文快标行为不变（APEX 同款手感）
- **布局**：8 等分扇形轮盘，以屏幕中心（准星位置）为圆心；中心死区 = 取消；鼠标方向选择扇区（无需点击），高亮+放大选中扇区
- **内容**：8 个标点类型一一对应扇区，图标 + 文字（走 Loc 词条 `ui.ping.*`）；类型色着色
- **游戏不暂停**（TimeScale 不变），轮盘打开期间抑制开火/标点单击判定，鼠标显示并自由移动
- **确认语义**：轮盘选中的标点作用于**轮盘打开瞬间的准星指向点**（敌人/物资上下文探测结果保留：选中 Attack 但准星无敌人时落为地面 Attack 标记点）
- UI 形态：`PingWheelUI`（prefab，UILayer.Top 之下 HUD 之上），8 扇区用 Image fillAmount 或 8 个旋转矩形占位，后续换美术扇形图

### 与其他系统的接口

- **AI 队友**：`IPingEvent.OnPingCreated` 扩展新类型；CompanionSystem 按类型映射 FSM 行为（PingMove/Engage/Hold/召回/撤退压制）；每型配 bark 语音确认词条（`companion.bark.ping_*`）
- **小地图**：`MinimapUI` 已消费标点事件，新类型加图标映射即可
- **音频**：audio.xlsx 每型一条 `sfx_ping_*`（占位音程序生成）；Help/Attack 音调更急促
- **多人预留**：事件签名已含 targetId，后续加 senderId 即可支持多玩家标点来源区分（当前单机+AI 不需要）

### 不做的事（明确边界）

- 不做 APEX 的"需要某物品"背包联动标点（仓库/UI 链路复杂，价值低）
- 不做多活动标点共存（AI 仲裁语义会模糊；多人化时再评估）
- 轮盘不做物品栏式拖放自定义（8 型固定）

## 里程碑

| 里程碑 | 内容 | 依赖 |
|---|---|---|
| M1 | PingWheelUI 轮盘（按住弹出/方向选择/松开确认/中心取消）+ 输入 hold/tap 判定改造 | 现有 PingSystem |
| M2 | PingType 扩展 8 型 + 队友行为映射（Watch/Defend/Help/Regroup/Retreat）+ bark 语音 | companion FSM、localization |
| M3 | 轮盘/标记美术图标、per-type 音效、小地图图标、Loot 拾取寻路 | 美术资源 |
