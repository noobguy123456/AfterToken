using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友状态机统一驱动器：每帧聚合感知（威胁集合/标点/姿态），并按优先级仲裁期望状态。
    /// 优先级（高→低）：死亡 &gt; 撤退（低血） &gt; 标点（显式指令） &gt; 交战（护主） &gt; 姿态（跟随/驻守）。
    /// LLM 意图建议（M3）未来以 PendingRequest 形式进入，不得覆盖死亡/撤退/标点。
    /// </summary>
    public class CompanionStateMachineDriver
    {
        private static CompanionStateMachineDriver _instance;
        public static CompanionStateMachineDriver Instance => _instance ??= new CompanionStateMachineDriver();

        /// <summary>低血撤退阈值兜底值（TbCompanion.retreatHpRatio 缺失时用）。</summary>
        public const float RetreatHpRatioFallback = 0.3f;

        /// <summary>低血撤退阈值（最大 HP 比例）。</summary>
        public static float RetreatHpRatio
        {
            get
            {
                var persona = CompanionConfigMgr.Instance.Get();
                return persona != null && persona.RetreatHpRatio > 0.001f ? persona.RetreatHpRatio : RetreatHpRatioFallback;
            }
        }

        /// <summary>
        /// 感知聚合：由 CompanionEntity.Update 每帧调用。
        /// 威胁集合由 CompanionSystem 通过 IEnemyEvent 维护（追击/攻击状态的敌人）。
        /// </summary>
        public void UpdateContext(CompanionStateContext context, CompanionEntity owner)
        {
            if (context == null || owner == null) return;
            context.NavigationSystem = Navigation.NavigationSystem.Instance;

            // 玩家位置：战斗场景走 PlayerSystem；经营场景 PlayerEntity 被移除，由 CompanionSystem 提供 Transform
            var playerTf = CompanionSystem.Instance != null ? CompanionSystem.Instance.PlayerTransform : null;
            context.PlayerExists = playerTf != null;
            if (playerTf != null)
            {
                context.PlayerPosition = playerTf.position.ToXZ();
            }

            // 威胁集合：清理失效引用并解析优先目标
            var threats = CompanionSystem.Instance != null ? CompanionSystem.Instance.Threats : null;
            if (threats != null)
            {
                threats.RemoveWhere(id => !EnemyRegistry.TryGet(id, out var e) || e == null || e.IsDead);
                context.ThreatCount = threats.Count;
            }
            else
            {
                context.ThreatCount = 0;
            }

            // Attack 标点目标：存活且已激活（在威胁集合内）才作为优先交战目标
            context.PingTarget = null;
            if (context.HasPing && context.PingType == PingType.Attack
                && EnemyRegistry.TryGet(context.PingTargetId, out var pinged)
                && pinged != null && !pinged.IsDead)
            {
                context.PingTarget = pinged;
            }

            context.NearestThreat = FindNearestThreat(owner.transform.position.ToXZ(), threats);
        }

        /// <summary>
        /// 期望状态仲裁。返回 null 表示维持当前状态。
        /// </summary>
        public Type GetDesiredState(CompanionStateContext context, Type currentStateType)
        {
            if (context == null) return null;

            // 1. 死亡（最高优先，不可逆）
            if (context.IsDead)
            {
                return typeof(CompanionDeadState);
            }

            bool lowHp = CompanionSystem.Instance != null
                         && CompanionSystem.Instance.Companion != null
                         && CompanionSystem.Instance.Companion.MaxHp > 0
                         && (float)CompanionSystem.Instance.Companion.Hp / CompanionSystem.Instance.Companion.MaxHp < RetreatHpRatio;

            // 2. 撤退：低血且有威胁；威胁清空后回到姿态
            if (currentStateType == typeof(CompanionRetreatState))
            {
                if (context.ThreatCount <= 0)
                {
                    return GetStanceState(context);
                }
                return null;
            }
            if (lowHp && context.ThreatCount > 0)
            {
                return typeof(CompanionRetreatState);
            }

            // 3. 标点（显式指令，高于交战）
            if (context.HasPing)
            {
                if (context.PingType == PingType.Attack)
                {
                    // Attack 标点：目标有效则带目标交战；目标失效视为标点过期（PingSystem 会清）
                    if (context.PingTarget != null)
                    {
                        return typeof(CompanionEngageState);
                    }
                }
                else
                {
                    return typeof(CompanionPingMoveState);
                }
            }

            // 4. 交战（护主）：有威胁则交战；威胁清空后回到姿态
            if (context.WantsEngage)
            {
                return typeof(CompanionEngageState);
            }
            if (currentStateType == typeof(CompanionEngageState))
            {
                return GetStanceState(context);
            }

            // 5. 姿态
            return GetStanceState(context);
        }

        /// <summary>
        /// 姿态状态：跟随开关关闭或标点到位驻守 → Hold，否则 Follow。
        /// </summary>
        private static Type GetStanceState(CompanionStateContext context)
        {
            return context.FollowEnabled && !context.StanceHold
                ? typeof(CompanionFollowState)
                : typeof(CompanionHoldState);
        }

        private static EnemyEntity FindNearestThreat(Vector2 ownerPos, HashSet<int> threats)
        {
            if (threats == null || threats.Count == 0) return null;

            EnemyEntity nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (var id in threats)
            {
                if (!EnemyRegistry.TryGet(id, out var enemy) || enemy == null || enemy.IsDead) continue;
                float sqr = (enemy.transform.position.ToXZ() - ownerPos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = enemy;
                }
            }
            return nearest;
        }
    }
}
