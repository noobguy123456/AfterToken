using TEngine;
using UnityEngine;
using GameLogic.Navigation;

namespace GameLogic
{
    /// <summary>
    /// 敌人追击状态。
    /// </summary>
    public class EnemyChaseState : EnemyStateBase
    {
        public override string StateName => "Chase";

        private float MoveSpeed => Owner?.MoveSpeed > 0.01f ? Owner.MoveSpeed : 2f;

        // 路径跟随
        private PathResult _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;
        private const float WAYPOINT_REACHED_THRESHOLD = 0.15f;
        private const float DIRECT_CHASE_DISTANCE = 1.5f;
        private const float SEPARATION_RADIUS = 0.6f;
        private const float SEPARATION_WEIGHT = 0.6f;
        private const float CHASE_RANGE = 8f;
        private const float MAX_INTERVAL_SCALE = 3f; // 远距离最大倍率

        // 静态缓存 LayerMask 与物理查询缓冲，避免 ApplySeparation/HasLineOfSight 每帧的 params 数组与结果数组分配。
        private static readonly int EnemyMask = LayerMask.GetMask("Enemy");
        private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle");
        private static readonly Collider2D[] _separationResults = new Collider2D[16];
        // Unity 6 新物理查询 API：ContactFilter2D 替代废弃的 OverlapCircleNonAlloc；useTriggers 与原默认行为一致。
        private static readonly ContactFilter2D SeparationFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = EnemyMask,
        };

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            RefreshPath();
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            if (Context.WantsToAttack)
            {
                RequestState<EnemyAttackState>();
                return;
            }

            if (!Context.WantsToChase)
            {
                RequestState<EnemyIdleState>();
                return;
            }

            Vector2 ownerPos = Owner.transform.position;
            Vector2 targetPos = Context.PlayerPosition;
            Vector2 toTarget = targetPos - ownerPos;
            float distanceToTarget = toTarget.magnitude;

            // 很近且直线可达时直接冲刺
            if (distanceToTarget <= DIRECT_CHASE_DISTANCE && HasLineOfSight(ownerPos, targetPos))
            {
                MoveTowards(toTarget.normalized, elapse);
                return;
            }

            _pathRefreshTimer += elapse;
            if (_pathRefreshTimer >= GetDynamicRefreshInterval() || _currentPath == null || !_currentPath.Success)
            {
                _pathRefreshTimer = 0f;
                RefreshPath();
            }

            if (_currentPath == null || !_currentPath.Success || _currentPath.Waypoints.Count == 0)
            {
                //  fallback：直接朝玩家移动（可能穿墙，但总比卡住好）
                MoveTowards(toTarget.normalized, elapse);
                return;
            }

            // 跟随路径点
            Vector2 waypoint = _currentPath.Waypoints[_currentWaypointIndex];
            Vector2 toWaypoint = waypoint - ownerPos;
            if (toWaypoint.magnitude <= WAYPOINT_REACHED_THRESHOLD)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _currentPath.Waypoints.Count)
                {
                    // 到达终点附近，直接朝玩家移动
                    MoveTowards(toTarget.normalized, elapse);
                    return;
                }
                waypoint = _currentPath.Waypoints[_currentWaypointIndex];
                toWaypoint = waypoint - ownerPos;
            }

            MoveTowards(toWaypoint.normalized, elapse);
        }

        protected override void OnLeaveState(IFsm<EnemyEntity> fsm, bool isShutdown)
        {
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector2.zero;
            }
            _currentPath = null;
        }

        private void RefreshPath()
        {
            var nav = Context.NavigationSystem;
            if (nav == null) return;

            _currentPath = nav.FindPath(Owner.transform.position, Context.PlayerPosition);
            _currentWaypointIndex = 0;
        }

        /// <summary>
        /// 根据与玩家的距离动态调整路径刷新间隔：近快远慢，降低成群敌人时的 A* 调用频率。
        /// </summary>
        private float GetDynamicRefreshInterval()
        {
            float baseInterval = Owner?.PathRefreshInterval ?? 0.3f;
            float distance = Vector2.Distance(Owner.transform.position, Context.PlayerPosition);
            float t = Mathf.Clamp01(distance / CHASE_RANGE);
            return Mathf.Lerp(baseInterval, baseInterval * MAX_INTERVAL_SCALE, t);
        }

        private void MoveTowards(Vector2 direction, float elapse)
        {
            Vector2 finalDirection = ApplySeparation(direction);
            Owner.SetFacing(finalDirection);
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = finalDirection * MoveSpeed;
            }
            else
            {
                Owner.transform.position += (Vector3)(finalDirection * MoveSpeed * elapse);
            }
        }

        /// <summary>
        /// 简易分离：检测附近敌人并施加反向偏移，缓解拥挤导致的刚体互卡。
        /// </summary>
        private Vector2 ApplySeparation(Vector2 desiredDirection)
        {
            // OverlapCircle（Unity 6 非分配版 API）+ 静态缓冲：结果即刻消费，不产生每帧数组分配；
            // 缓冲 16 个对于 0.6m 分离半径足够（超出部分仅影响分离向量的精度，不影响功能）。
            int hitCount = Physics2D.OverlapCircle(Owner.transform.position, SEPARATION_RADIUS, SeparationFilter, _separationResults);
            if (hitCount <= 1) return desiredDirection;

            Vector2 separation = Vector2.zero;
            int count = 0;
            Vector2 ownerPos = Owner.transform.position;
            for (int i = 0; i < hitCount; i++)
            {
                var col = _separationResults[i];
                if (col == null || col.gameObject == Owner.gameObject) continue;
                Vector2 away = ownerPos - (Vector2)col.transform.position;
                float dist = away.magnitude;
                if (dist > 0.01f)
                {
                    separation += away.normalized / dist;
                    count++;
                }
            }

            if (count == 0) return desiredDirection;
            separation /= count;
            return (desiredDirection + separation * SEPARATION_WEIGHT).normalized;
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, ObstacleMask).collider == null;
        }
    }
}
