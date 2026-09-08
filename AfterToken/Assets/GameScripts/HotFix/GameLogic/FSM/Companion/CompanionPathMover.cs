using GameLogic.Navigation;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 队友路径移动助手：Follow/PingMove/Engage/Retreat 四态共用的寻路跟随逻辑
    /// （EnemyChaseState 同款模式的精简版：路径刷新 + 路径点跟随 + 直达冲刺 + 导航缺失直走）。
    /// 有刚体走速度驱动；无导航网格（经营场景）时直接朝目标移动。
    /// </summary>
    public class CompanionPathMover
    {
        private const float WAYPOINT_REACHED_THRESHOLD = 0.15f;
        private const float DIRECT_MOVE_DISTANCE = 1.5f;
        private const float PATH_REFRESH_INTERVAL = 0.4f;
        /// <summary>目标移动超过该距离才重寻路，避免跟随移动目标时每帧跑 A*。</summary>
        private const float REPATH_TARGET_DELTA = 1.2f;

        private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle");

        private PathResult _path;
        private int _waypointIndex;
        private float _refreshTimer;
        private Vector2 _lastPathTarget;
        private bool _hasPathTarget;

        /// <summary>
        /// 朝目标移动一帧。返回 true 表示已到达（距离 ≤ arriveThreshold）。
        /// </summary>
        public bool MoveTowards(CompanionEntity owner, INavigationSystem nav, Vector2 target, float elapse, float arriveThreshold)
        {
            Vector2 ownerPos = owner.transform.position.ToXZ();
            Vector2 toTarget = target - ownerPos;
            float distance = toTarget.magnitude;
            if (distance <= arriveThreshold)
            {
                Stop(owner);
                return true;
            }

            // 近距离直线可达：直接移动
            if (distance <= DIRECT_MOVE_DISTANCE && HasLineOfSight(ownerPos, target))
            {
                Move(owner, toTarget.normalized, owner.MoveSpeed);
                return false;
            }

            if (nav == null)
            {
                // 无导航网格（经营场景）：直走
                Move(owner, toTarget.normalized, owner.MoveSpeed);
                return false;
            }

            _refreshTimer += elapse;
            bool targetMoved = !_hasPathTarget || Vector2.Distance(target, _lastPathTarget) > REPATH_TARGET_DELTA;
            if (_refreshTimer >= PATH_REFRESH_INTERVAL || targetMoved || !IsPathValid())
            {
                _refreshTimer = 0f;
                _path = nav.FindPath(ownerPos, target);
                _waypointIndex = 0;
                _lastPathTarget = target;
                _hasPathTarget = true;
                SkipPassedWaypoints(ownerPos);
            }

            if (!IsPathValid() || _path.Waypoints.Count == 0)
            {
                // 寻路失败：有视线直走，无视线原地等下次刷新
                if (HasLineOfSight(ownerPos, target))
                {
                    Move(owner, toTarget.normalized, owner.MoveSpeed);
                }
                else
                {
                    Stop(owner);
                }
                return false;
            }

            if (_waypointIndex >= _path.Waypoints.Count)
            {
                Move(owner, toTarget.normalized, owner.MoveSpeed);
                return false;
            }

            Vector2 waypoint = _path.Waypoints[_waypointIndex];
            Vector2 toWaypoint = waypoint - ownerPos;
            if (toWaypoint.magnitude <= WAYPOINT_REACHED_THRESHOLD)
            {
                _waypointIndex++;
                if (_waypointIndex >= _path.Waypoints.Count)
                {
                    Move(owner, toTarget.normalized, owner.MoveSpeed);
                    return false;
                }
                waypoint = _path.Waypoints[_waypointIndex];
                toWaypoint = waypoint - ownerPos;
            }

            Move(owner, toWaypoint.normalized, owner.MoveSpeed);
            return false;
        }

        /// <summary>停下并清空路径状态（切状态/到位时调用）。</summary>
        public void Stop(CompanionEntity owner)
        {
            if (owner?.Rigidbody != null)
            {
                owner.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        /// <summary>状态退出时复位，下次进入重新寻路。</summary>
        public void Reset()
        {
            _path = null;
            _waypointIndex = 0;
            _refreshTimer = 0f;
            _hasPathTarget = false;
        }

        private bool IsPathValid()
        {
            return _path != null && _path.Success;
        }

        /// <summary>
        /// 跳过已越过/可直达的前置路径点（路径起点吸附格中心，waypoint[0] 可能落在身后，
        /// 不跳过会周期性往回抖——与敌人追击同款修正）。
        /// </summary>
        private void SkipPassedWaypoints(Vector2 ownerPos)
        {
            if (!IsPathValid()) return;
            var waypoints = _path.Waypoints;
            while (_waypointIndex < waypoints.Count - 1 &&
                   HasWideLineOfSight(ownerPos, waypoints[_waypointIndex + 1]))
            {
                _waypointIndex++;
            }
        }

        private static bool HasWideLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 0.001f) return true;
            Vector3 origin = from.ToWorld(0.5f);
            return !Physics.SphereCast(origin, ColliderGridBuilder.AgentRadius * 0.9f, direction.normalized.ToWorld(), out _, distance, ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        private static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            return !Physics.Linecast(from.ToWorld(0.5f), to.ToWorld(0.5f), ObstacleMask);
        }

        private static void Move(CompanionEntity owner, Vector2 direction, float speed)
        {
            owner.SetFacing(direction);
            if (owner.Rigidbody != null)
            {
                owner.Rigidbody.linearVelocity = new Vector3(direction.x, 0f, direction.y) * speed;
            }
            else
            {
                owner.transform.position += (direction * speed * Time.deltaTime).ToWorld();
            }
        }
    }
}
