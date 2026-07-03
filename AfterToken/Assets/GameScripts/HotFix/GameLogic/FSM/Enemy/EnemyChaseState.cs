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

        // 占位移动速度，后续接入 TbEnemy 配置表
        private float _moveSpeed = 2f;

        // 路径跟随
        private PathResult _currentPath;
        private int _currentWaypointIndex;
        private float _pathRefreshTimer;
        private const float PATH_REFRESH_INTERVAL = 0.3f;
        private const float WAYPOINT_REACHED_THRESHOLD = 0.15f;
        private const float DIRECT_CHASE_DISTANCE = 1.5f;

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
                MoveTowards(toTarget.normalized);
                return;
            }

            _pathRefreshTimer += elapse;
            if (_pathRefreshTimer >= PATH_REFRESH_INTERVAL || _currentPath == null || !_currentPath.Success)
            {
                _pathRefreshTimer = 0f;
                RefreshPath();
            }

            if (_currentPath == null || !_currentPath.Success || _currentPath.Waypoints.Count == 0)
            {
                //  fallback：直接朝玩家移动（可能穿墙，但总比卡住好）
                MoveTowards(toTarget.normalized);
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
                    MoveTowards(toTarget.normalized);
                    return;
                }
                waypoint = _currentPath.Waypoints[_currentWaypointIndex];
                toWaypoint = waypoint - ownerPos;
            }

            MoveTowards(toWaypoint.normalized);
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
            var nav = NavigationSystem.Instance;
            if (nav == null) return;

            _currentPath = nav.FindPath(Owner.transform.position, Context.PlayerPosition);
            _currentWaypointIndex = 0;
        }

        private void MoveTowards(Vector2 direction)
        {
            Owner.SetFacing(direction);
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = direction * _moveSpeed;
            }
            else
            {
                Owner.transform.position += (Vector3)(direction * _moveSpeed * Time.deltaTime);
            }
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            return Physics2D.Linecast(from, to, LayerMask.GetMask("Obstacle")).collider == null;
        }
    }
}
