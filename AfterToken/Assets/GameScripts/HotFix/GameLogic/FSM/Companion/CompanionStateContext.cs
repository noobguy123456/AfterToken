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
        /// <summary>队友自身最近是否受击（预留：敌人索敌队友后升级为威胁源）。</summary>
        public bool UnderAttack;

        // ── 标点 ──
        public bool HasPing;
        public PingType PingType;
        public Vector2 PingPos;
        public int PingTargetId;

        public INavigationSystem NavigationSystem;
        public StateTransitionRequest PendingRequest;

        public bool WantsEngage => ThreatCount > 0 || (PingTarget != null && !PingTarget.IsDead);

        public void ClearPing()
        {
            HasPing = false;
            PingType = PingType.None;
            PingTargetId = 0;
            PingTarget = null;
        }
    }
}
