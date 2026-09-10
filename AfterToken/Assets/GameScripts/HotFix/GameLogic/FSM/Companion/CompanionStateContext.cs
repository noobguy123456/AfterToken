using GameLogic.Navigation;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友状态黑板。由 CompanionStateMachineDriver 每帧聚合感知结果。
    /// </summary>
    public class CompanionStateContext
    {
        public bool IsDead;

        // ── 玩家 ──
        public bool PlayerExists;
        public Vector2 PlayerPosition;

        // ──  stance（姿态）：跟随开关 / 标点到位后的驻守 ──
        /// <summary>G 键跟随开关（玩家可决定是否让队友跟随）。</summary>
        public bool FollowEnabled = true;
        /// <summary>标点到位后的驻守标记：G 键切回跟随时清除。</summary>
        public bool StanceHold;

        // ── 威胁感知（护主核心）──
        /// <summary>当前处于追击/攻击状态的敌人数（&gt;0 即战斗状态）。</summary>
        public int ThreatCount;
        /// <summary>优先目标：Attack 标点指定的敌人（存活且在威胁集合内才有效）。</summary>
        public EnemyEntity PingTarget;
        /// <summary>最近威胁（无标点目标时的交战对象）。</summary>
        public EnemyEntity NearestThreat;
        /// <summary>主动开火目标：警戒半径内视线通畅的最近敌人（即使它还没进入追击状态）。</summary>
        public EnemyEntity NearestVisibleEnemy;
        /// <summary>队友自身最近是否受击（预留：敌人索敌队友后升级为威胁源）。</summary>
        public bool UnderAttack;

        // ── 标点 ──
        public bool HasPing;
        public PingType PingType;
        public Vector2 PingPos;
        public int PingTargetId;

        // ── LLM 操控指令（M5）：DecisionDriver 写入，驱动器仲裁读取 ──
        // 优先级低于死亡/低血撤退/玩家标点（硬规矩），高于交战与姿态。
        /// <summary>当前 LLM 指令动作（null/empty/none = 无指令）。</summary>
        public string LlmAction;
        /// <summary>move_to/loot/extract 的目标位置。</summary>
        public Vector2 LlmTargetPos;
        /// <summary>engage 目标敌人 InstanceID / loot 目标掉落物 InstanceID。</summary>
        public int LlmTargetId;
        /// <summary>指令过期点（Time.time），过期自动作废，防止陈旧指令卡死状态机。</summary>
        public float LlmDirectiveExpire;
        /// <summary>engage 指令解析后的目标（驱动器每帧解析，死亡自动失效）。</summary>
        public EnemyEntity LlmTarget;

        /// <summary>
        /// 玩家显式指令保护期（Time.time 截止点）：玩家聊天指令（follow/hold）与 G 键跟随开关置位。
        /// 期间 LLM 决策指令不生效（同标点优先级思路：玩家直接指令永远压过 LLM 自主决策），
        /// 防止决策节拍用快照坐标覆盖玩家的"跟着我"意图。
        /// </summary>
        public float PlayerCommandUntil;

        public INavigationSystem NavigationSystem;
        public StateTransitionRequest PendingRequest;

        public bool WantsEngage => ThreatCount > 0 || (PingTarget != null && !PingTarget.IsDead)
                                   || (NearestVisibleEnemy != null && !NearestVisibleEnemy.IsDead);

        /// <summary>是否有未过期的 LLM 指令。</summary>
        public bool HasLlmDirective =>
            !string.IsNullOrEmpty(LlmAction) && LlmAction != "none" && Time.time < LlmDirectiveExpire;

        /// <summary>是否为移动类 LLM 指令（PingMove 状态复用为执行体）。</summary>
        public bool HasLlmMoveDirective =>
            HasLlmDirective && (LlmAction == "move_to" || LlmAction == "loot" || LlmAction == "extract");

        public void ClearLlmDirective()
        {
            LlmAction = null;
            LlmTargetId = 0;
            LlmTarget = null;
            LlmDirectiveExpire = 0f;
        }

        public void ClearPing()
        {
            HasPing = false;
            PingType = PingType.None;
            PingTargetId = 0;
            PingTarget = null;
        }
    }
}
