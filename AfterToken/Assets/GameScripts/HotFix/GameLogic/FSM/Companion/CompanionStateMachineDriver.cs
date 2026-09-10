using System;
using System.Collections.Generic;
using GameLogic.AI.Llm;
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

        /// <summary>感知节拍（秒）：最近威胁/可见敌人的全量扫描按此节流，不逐帧跑。</summary>
        public const float SenseInterval = 0.2f;

        private float _senseTimer;

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

            // Attack 标点目标：存活且已激活（在威胁集合内）才作为优先交战目标（单条字典查找，留逐帧）
            context.PingTarget = null;
            if (context.HasPing && context.PingType == PingType.Attack
                && EnemyRegistry.TryGet(context.PingTargetId, out var pinged)
                && pinged != null && !pinged.IsDead)
            {
                context.PingTarget = pinged;
            }

            // 感知节拍：最近威胁 + 可见敌人扫描（后者含全敌人遍历 + Linecast，最贵）按 SenseInterval 节流。
            // 0.2s 的反应延迟对交战判定无感，逐帧跑是纯浪费。
            _senseTimer -= Time.deltaTime;
            if (_senseTimer <= 0f)
            {
                _senseTimer = SenseInterval;
                context.NearestThreat = FindNearestThreat(owner.transform.position.ToXZ(), threats);
                context.NearestVisibleEnemy = FindNearestVisibleEnemy(owner.transform.position.ToXZ());
            }

            // LLM 指令（M5）：过期清理 + engage 目标解析（目标死亡/失效则指令作废）
            if (!context.HasLlmDirective)
            {
                if (context.LlmAction != null)
                {
                    context.ClearLlmDirective();
                }
            }
            else if (context.LlmAction == DecisionDriver.ActionEngage)
            {
                context.LlmTarget = null;
                if (EnemyRegistry.TryGet(context.LlmTargetId, out var llmTarget)
                    && llmTarget != null && !llmTarget.IsDead)
                {
                    context.LlmTarget = llmTarget;
                }
            }
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

            // 4. 交战（护主）：有威胁或警戒半径内有可见敌人则交战；清空后回到姿态
            // （交战压过 LLM 指令：护主是硬行为，LLM 只能在非战斗时调度姿态/移动）
            if (context.WantsEngage)
            {
                return typeof(CompanionEngageState);
            }
            if (currentStateType == typeof(CompanionEngageState))
            {
                return GetStanceState(context);
            }

            // 5. LLM 操控指令（M5）：仅 llm 操控模式且链路未断时生效；
            // 死亡/低血撤退/玩家标点/交战护主（上方硬规矩）永远压过它；
            // 玩家显式指令保护期内同样不生效（聊天 follow/hold、G 键跟随）
            if (CompanionSystem.IsLlmControlActive && context.HasLlmDirective
                && Time.time >= context.PlayerCommandUntil)
            {
                switch (context.LlmAction)
                {
                    case DecisionDriver.ActionFollow:
                        return typeof(CompanionFollowState);
                    case DecisionDriver.ActionHold:
                    case DecisionDriver.ActionRetreat: // LLM 撤退建议映射驻守（生存撤退由上方硬规则管）
                        return typeof(CompanionHoldState);
                    case DecisionDriver.ActionMoveTo:
                    case DecisionDriver.ActionLoot:
                    case DecisionDriver.ActionExtract:
                        return typeof(CompanionPingMoveState);
                    case DecisionDriver.ActionEngage:
                        if (context.LlmTarget != null && !context.LlmTarget.IsDead)
                        {
                            return typeof(CompanionEngageState);
                        }
                        context.ClearLlmDirective(); // 目标已死/失效，指令作废
                        break;
                }
            }

            // 6. 姿态
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

        /// <summary>主动开火警戒半径兜底值（TbCompanion.proactiveEngageDist 缺失时用）。</summary>
        public const float ProactiveEngageDistFallback = 12f;

        /// <summary>主动开火警戒半径。</summary>
        public static float ProactiveEngageDist
        {
            get
            {
                var persona = CompanionConfigMgr.Instance.Get();
                return persona != null && persona.ProactiveEngageDist > 0.1f
                    ? persona.ProactiveEngageDist : ProactiveEngageDistFallback;
            }
        }

        private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle");

        /// <summary>
        /// 主动开火感知：警戒半径内离队友最近、且视线通畅（不被 Obstacle 遮挡）的敌人。
        /// 敌人处于 Idle/巡逻也算——队友先发制人，而不是等被追击才还手。
        /// </summary>
        private static EnemyEntity FindNearestVisibleEnemy(Vector2 ownerPos)
        {
            var all = EnemyRegistry.All;
            if (all == null || all.Count == 0) return null;

            float maxSqr = ProactiveEngageDist * ProactiveEngageDist;
            EnemyEntity nearest = null;
            float nearestSqr = float.MaxValue;
            foreach (var enemy in all)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector2 enemyPos = enemy.transform.position.ToXZ();
                float sqr = (enemyPos - ownerPos).sqrMagnitude;
                if (sqr > maxSqr || sqr >= nearestSqr) continue;
                // 视线被挡不算（与交战开火同一语义，隔着墙不主动招惹）
                if (Physics.Linecast(ownerPos.ToWorld(0.5f), enemyPos.ToWorld(0.5f), ObstacleMask)) continue;
                nearestSqr = sqr;
                nearest = enemy;
            }
            return nearest;
        }
    }
}
