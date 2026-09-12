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
        // 分离半径需明显大于接触距离（两胶囊 0.3+0.3=0.6m 才互相推开），
        // 让转向在接触发生前就生效，避免挤作一团后物理位置修正把敌人瞬间弹开（追击时的抽搐感）
        private const float SEPARATION_RADIUS = 0.9f;
        private const float SEPARATION_WEIGHT = 0.8f;
        private const float SEPARATION_PROBE_DISTANCE = 0.3f; // 分离方向预判步长（≈敌人半径）
        private const float CHASE_RANGE_FALLBACK = 5f; // 未配置仇恨范围时的回退值
        private const float MAX_INTERVAL_SCALE = 3f; // 远距离最大倍率

        // 卡住检测：一段时间内位移过小判定顶墙，强制重寻路；连续卡住则短暂待机再重试
        private const float STUCK_CHECK_INTERVAL = 0.8f;
        private const float STUCK_DISTANCE_THRESHOLD = 0.1f;
        private const int STUCK_RECOVER_THRESHOLD = 3;
        private const float STUCK_RECOVER_TIME = 0.5f;
        // 寻路失败退避：重试间隔递增封顶，避免每帧跑 A*
        private const float BASE_FAIL_RETRY_INTERVAL = 0.5f;
        private const float MAX_FAIL_RETRY_INTERVAL = 2f;

        private float _stuckTimer;
        private Vector2 _stuckCheckPos;
        private int _stuckCount;
        private float _stuckRecoverTimer;
        private float _pathFailInterval = BASE_FAIL_RETRY_INTERVAL;

        // 脚步音效：追击移动时按固定间隔播放 3D 脚步（占位音，由 AudioSystem 做距离衰减）
        private const float FOOTSTEP_INTERVAL = 0.45f;
        private float _footstepTimer;

        // 静态缓存 LayerMask 与物理查询缓冲，避免 ApplySeparation/HasLineOfSight 每帧的结果数组分配。
        private static readonly int EnemyMask = LayerMask.GetMask("Enemy");
        private static readonly int ObstacleMask = LayerMask.GetMask("Obstacle");
        private static readonly Collider[] _separationResults = new Collider[16];

        protected override void OnEnterState(IFsm<EnemyEntity> fsm)
        {
            _currentPath = null;
            _currentWaypointIndex = 0;
            _pathRefreshTimer = 0f;
            _stuckTimer = 0f;
            _stuckCheckPos = Owner.transform.position.ToXZ();
            _stuckCount = 0;
            _stuckRecoverTimer = 0f;
            _pathFailInterval = BASE_FAIL_RETRY_INTERVAL;
            _footstepTimer = 0f;
            RefreshPath();
        }

        protected override void OnUpdateState(IFsm<EnemyEntity> fsm, float elapse, float real)
        {
            if (Context.WantsToAttack)
            {
                RequestState<EnemyAttackState>();
                return;
            }

            // 丢失目标（玩家超出追踪距离）由 EnemyWanderInterceptor 统一切到 Wander；
            // chaseRange ~ pursuitRange 滞回区间内 WantsToChase 为 false 时仍继续追击

            Vector2 ownerPos = Owner.transform.position.ToXZ();
            Vector2 targetPos = Context.PlayerPosition;
            Vector2 toTarget = targetPos - ownerPos;
            float distanceToTarget = toTarget.magnitude;

            // 卡住恢复中：原地待机，倒计时结束后再行动
            if (_stuckRecoverTimer > 0f)
            {
                _stuckRecoverTimer -= elapse;
                StopMoving();
                return;
            }

            UpdateStuckDetection(ownerPos, elapse);

            // 很近且直线可达时直接冲刺
            if (distanceToTarget <= DIRECT_CHASE_DISTANCE && HasLineOfSight(ownerPos, targetPos))
            {
                MoveTowards(toTarget.normalized, elapse);
                return;
            }

            _pathRefreshTimer += elapse;
            // 路径无效时按退避间隔（递增封顶）重试，避免寻路失败每帧跑 A*
            float refreshInterval = IsPathValid() ? GetDynamicRefreshInterval() : _pathFailInterval;
            if (_pathRefreshTimer >= refreshInterval)
            {
                _pathRefreshTimer = 0f;
                RefreshPath();
                _pathFailInterval = IsPathValid()
                    ? BASE_FAIL_RETRY_INTERVAL
                    : Mathf.Min(_pathFailInterval * 2f, MAX_FAIL_RETRY_INTERVAL);
            }

            if (!IsPathValid() || _currentPath.Waypoints.Count == 0)
            {
                // 寻路失败 fallback：有视线才直追，无视线原地等待退避重试，避免顶墙死锁
                if (HasLineOfSight(ownerPos, targetPos))
                {
                    MoveTowards(toTarget.normalized, elapse);
                }
                else
                {
                    StopMoving();
                }
                return;
            }

            // 跟随路径点
            // 上一帧走完最后一个路径点后索引已越界（直接冲刺分支不回退索引），下次刷新前直接朝玩家移动
            if (_currentWaypointIndex >= _currentPath.Waypoints.Count)
            {
                MoveTowards(toTarget.normalized, elapse);
                return;
            }
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
                Owner.Rigidbody.linearVelocity = Vector3.zero;
            }
            _currentPath = null;
        }

        private void RefreshPath()
        {
            var nav = Context.NavigationSystem;
            if (nav == null) return;

            _currentPath = nav.FindPath(Owner.transform.position.ToXZ(), Context.PlayerPosition);
            _currentWaypointIndex = 0;
            SkipPassedWaypoints();
        }

        /// <summary>
        /// 跳过已越过/可直达的前置路径点。
        /// 路径起点会吸附到导航格中心（缓存命中还有 0.5m 量化误差），waypoint[0] 可能落在敌人身后；
        /// 不跳过的话每次刷新路径敌人都会先回头走向身后的格中心，表现为追击时的周期性往回抖。
        /// 判定从敌人实际位置对下下个路径点做带宽度视线检查，与 A* 平滑的 SphereCast 语义一致，避免穿角。
        /// </summary>
        private void SkipPassedWaypoints()
        {
            if (!IsPathValid()) return;
            var waypoints = _currentPath.Waypoints;
            Vector2 ownerPos = Owner.transform.position.ToXZ();
            while (_currentWaypointIndex < waypoints.Count - 1 &&
                   HasWideLineOfSight(ownerPos, waypoints[_currentWaypointIndex + 1]))
            {
                _currentWaypointIndex++;
            }
        }

        /// <summary>
        /// 带宽度视线检查：与 AStarNavigationSystem 路径平滑同一半径（AgentRadius × 0.9）。
        /// </summary>
        private static bool HasWideLineOfSight(Vector2 from, Vector2 to)
        {
            Vector2 direction = to - from;
            float distance = direction.magnitude;
            if (distance < 0.001f) return true;
            Vector3 origin = from.ToWorld(0.5f);
            return !Physics.SphereCast(origin, ColliderGridBuilder.AgentRadius * 0.9f, direction.normalized.ToWorld(), out _, distance, ObstacleMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// 根据与玩家的距离动态调整路径刷新间隔：近快远慢，降低成群敌人时的 A* 调用频率。
        /// </summary>
        private float GetDynamicRefreshInterval()
        {
            float baseInterval = Owner?.PathRefreshInterval ?? 0.3f;
            float chaseRange = Owner?.ChaseRange > 0.01f ? Owner.ChaseRange : CHASE_RANGE_FALLBACK;
            float distance = Vector2.Distance(Owner.transform.position.ToXZ(), Context.PlayerPosition);
            float t = Mathf.Clamp01(distance / chaseRange);
            return Mathf.Lerp(baseInterval, baseInterval * MAX_INTERVAL_SCALE, t);
        }

        private bool IsPathValid()
        {
            return _currentPath != null && _currentPath.Success;
        }

        /// <summary>
        /// 位移监控：窗口内位移小于阈值判定卡住，立即强制重寻路；
        /// 连续卡住达到上限则短暂待机后再重试，避免顶墙死锁。
        /// </summary>
        private void UpdateStuckDetection(Vector2 ownerPos, float elapse)
        {
            _stuckTimer += elapse;
            if (_stuckTimer < STUCK_CHECK_INTERVAL) return;

            float moved = Vector2.Distance(ownerPos, _stuckCheckPos);
            _stuckCheckPos = ownerPos;
            _stuckTimer = 0f;
            if (moved >= STUCK_DISTANCE_THRESHOLD)
            {
                _stuckCount = 0;
                return;
            }

            _stuckCount++;
            // 玩家长期不可达时每个敌人每 STUCK_CHECK_INTERVAL 触发一次，日志降级为 Debug 避免刷屏
            Log.Debug($"[EnemyChase] 敌人 {Owner.GetInstanceID()} 疑似卡住（连续 {_stuckCount} 次），强制重寻路");
            if (IsPathValid())
            {
                // 仅路径有效时强制重寻路；路径无效交给 Update 里的退避重试，避免绕过 _pathFailInterval 每 0.8s 跑一次 A*
                _pathRefreshTimer = 0f;
                RefreshPath();
            }
            if (_stuckCount >= STUCK_RECOVER_THRESHOLD)
            {
                _stuckCount = 0;
                _stuckRecoverTimer = STUCK_RECOVER_TIME;
            }
        }

        private void StopMoving()
        {
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = Vector3.zero;
            }
        }

        private void MoveTowards(Vector2 direction, float elapse)
        {
            Vector2 finalDirection = ApplySeparation(direction);
            Owner.SetFacing(finalDirection);
            if (Owner.Rigidbody != null)
            {
                Owner.Rigidbody.linearVelocity = new Vector3(finalDirection.x, 0f, finalDirection.y) * MoveSpeed;
            }
            else
            {
                Owner.transform.position += (finalDirection * MoveSpeed * elapse).ToWorld();
            }

            // 追击脚步声（3D 空间音，随距离衰减；AudioSystem 未就绪时静默跳过）
            _footstepTimer += elapse;
            if (_footstepTimer >= FOOTSTEP_INTERVAL)
            {
                _footstepTimer = 0f;
                AudioSystem.Instance?.Play3D("sfx_footstep", Owner.transform.position);
            }
        }

        /// <summary>
        /// 简易分离：检测附近敌人并施加反向偏移，缓解拥挤导致的刚体互卡。
        /// </summary>
        private Vector2 ApplySeparation(Vector2 desiredDirection)
        {
            // OverlapSphereNonAlloc + 静态缓冲：结果即刻消费，不产生每帧数组分配；
            // 缓冲 16 个对于 0.6m 分离半径足够（超出部分仅影响分离向量的精度，不影响功能）。
            int hitCount = Physics.OverlapSphereNonAlloc(Owner.transform.position, SEPARATION_RADIUS, _separationResults, EnemyMask, QueryTriggerInteraction.Collide);
            if (hitCount <= 1) return desiredDirection;

            Vector2 separation = Vector2.zero;
            int count = 0;
            Vector2 ownerPos = Owner.transform.position.ToXZ();
            for (int i = 0; i < hitCount; i++)
            {
                var col = _separationResults[i];
                if (col == null || col.gameObject == Owner.gameObject) continue;
                Vector2 away = ownerPos - col.transform.position.ToXZ();
                float dist = away.magnitude;
                if (dist > 0.01f)
                {
                    separation += away.normalized / dist;
                    count++;
                }
            }

            if (count == 0) return desiredDirection;
            separation /= count;
            Vector2 combined = (desiredDirection + separation * SEPARATION_WEIGHT).normalized;
            // 分离不能把敌人推进障碍：预判前方位置不可走则丢弃分离分量，只保留路径方向
            var nav = Context.NavigationSystem;
            if (nav != null && !nav.IsWalkable(ownerPos + combined * SEPARATION_PROBE_DISTANCE))
            {
                return desiredDirection;
            }
            return combined;
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            // 检测高度取 0.5f，与敌人胶囊中心对齐
            return !Physics.Linecast(from.ToWorld(0.5f), to.ToWorld(0.5f), ObstacleMask);
        }
    }
}
