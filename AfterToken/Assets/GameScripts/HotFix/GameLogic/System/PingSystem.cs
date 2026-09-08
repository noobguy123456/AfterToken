using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 标点系统（APEX 式单击上下文标点）：仅战斗场景挂载（BattleRoot）。
    /// 订阅 IBattleInputEvent.OnPingPressed，以准星指向点做上下文分类
    /// （敌人→Attack / 掉落物与容器→Loot / 地面→Move），维护唯一活动标点
    /// （新标点覆盖旧的，15s 未消费自动过期），并发 IPingEvent 给队友/小地图。
    /// 世界内标记为占位实现：彩色竖面片 + 屏幕对齐 + 脉冲缩放。
    /// </summary>
    public class PingSystem : MonoBehaviour
    {
        public static PingSystem Instance { get; private set; }

        /// <summary>标点有效期（秒）。</summary>
        private const float PING_LIFETIME = 15f;
        /// <summary>上下文判定半径（米）。</summary>
        private const float ENEMY_PROBE_RADIUS = 1.0f;
        private const float LOOT_PROBE_RADIUS = 1.2f;

        private static readonly Color MoveColor = new Color(0.2f, 0.9f, 1f, 0.9f);
        private static readonly Color LootColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        private static readonly Color AttackColor = new Color(1f, 0.25f, 0.2f, 0.9f);

        private readonly GameEventMgr _eventMgr = new GameEventMgr();
        private static readonly Collider[] _probeResults = new Collider[16];

        private PingType _activeType = PingType.None;
        private Vector2 _activePos;
        private int _activeTargetId;
        private float _activeTimer;
        private GameObject _marker;
        private Renderer _markerRenderer;
        private Transform _markerFollowTarget;
        private Camera _mainCamera;

        /// <summary>当前是否有活动标点。</summary>
        public bool HasActivePing => _activeType != PingType.None;
        public PingType ActivePingType => _activeType;
        public Vector2 ActivePingPos => _activeType == PingType.Attack && _markerFollowTarget != null
            ? _markerFollowTarget.position.ToXZ()
            : _activePos;

        private void Awake()
        {
            Instance = this;
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnPingPressed, CreatePing);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            ClearPing();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (_activeType == PingType.None) return;

            _activeTimer -= Time.deltaTime;

            // Attack 标点：目标死亡/消失即清除
            if (_activeType == PingType.Attack)
            {
                if (_markerFollowTarget == null
                    || !EnemyRegistry.TryGet(_activeTargetId, out var enemy)
                    || enemy == null || enemy.IsDead)
                {
                    ClearPing();
                    return;
                }
            }

            if (_activeTimer <= 0f)
            {
                ClearPing();
                return;
            }

            UpdateMarkerVisual();
        }

        /// <summary>
        /// 清除当前标点（过期/覆盖/队友消费）。
        /// </summary>
        public void ClearPing()
        {
            if (_activeType == PingType.None) return;

            _activeType = PingType.None;
            _activeTargetId = 0;
            _markerFollowTarget = null;
            if (_marker != null)
            {
                Destroy(_marker);
                _marker = null;
                _markerRenderer = null;
            }
            GameEvent.Get<IPingEvent>()?.OnPingCleared();
        }

        /// <summary>
        /// 以玩家准星指向点创建上下文标点。
        /// </summary>
        private void CreatePing()
        {
            var player = PlayerSystem.Instance != null ? PlayerSystem.Instance.GetPlayerEntity() : null;
            if (player == null) return;

            Vector2 aimPos = player.AimPosition;

            // 分类：敌人 > 掉落物/容器 > 地面
            if (TryProbeEnemy(aimPos, out EnemyEntity enemy))
            {
                SetPing(PingType.Attack, enemy.transform.position.ToXZ(), enemy.GetInstanceID(), enemy.transform);
                return;
            }

            if (TryProbeLoot(aimPos, out Vector2 lootPos))
            {
                SetPing(PingType.Loot, lootPos, 0, null);
                return;
            }

            // 地面标点：钳制到最近可走点（无导航时保持原样）
            Vector2 movePos = aimPos;
            var nav = Navigation.NavigationSystem.Instance;
            if (nav != null && !nav.IsWalkable(aimPos) && nav.TryGetNearestWalkable(aimPos, out Vector2 walkable))
            {
                movePos = walkable;
            }
            SetPing(PingType.Move, movePos, 0, null);
        }

        private void SetPing(PingType type, Vector2 pos, int targetId, Transform followTarget)
        {
            // 新标点覆盖旧的（ClearPing 会发事件，队友收到后立即收到新标点，时序正确）
            ClearPing();

            _activeType = type;
            _activePos = pos;
            _activeTargetId = targetId;
            _markerFollowTarget = followTarget;
            _activeTimer = PING_LIFETIME;

            CreateMarker(type, pos);
            GameEvent.Get<IPingEvent>()?.OnPingCreated(type, pos, targetId);
        }

        #region 上下文探测

        private static bool TryProbeEnemy(Vector2 aimPos, out EnemyEntity result)
        {
            result = null;
            int count = Physics.OverlapSphereNonAlloc(aimPos.ToWorld(0.5f), ENEMY_PROBE_RADIUS, _probeResults, LayerMask.GetMask("Enemy"), QueryTriggerInteraction.Ignore);
            float nearestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var enemy = _probeResults[i] != null ? _probeResults[i].GetComponentInParent<EnemyEntity>() : null;
                if (enemy == null || enemy.IsDead) continue;
                float sqr = (enemy.transform.position.ToXZ() - aimPos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    result = enemy;
                }
            }
            return result != null;
        }

        private static bool TryProbeLoot(Vector2 aimPos, out Vector2 lootPos)
        {
            lootPos = default;

            // 掉落物（trigger 碰撞体，未挂专门层，按组件判定）
            int count = Physics.OverlapSphereNonAlloc(aimPos.ToWorld(0.5f), LOOT_PROBE_RADIUS, _probeResults, ~0, QueryTriggerInteraction.Collide);
            float nearestSqr = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                var col = _probeResults[i];
                if (col == null) continue;
                var pickup = col.GetComponentInParent<PickupEntity>();
                if (pickup == null) continue;
                float sqr = (pickup.transform.position.ToXZ() - aimPos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    lootPos = pickup.transform.position.ToXZ();
                    found = true;
                }
            }
            if (found) return true;

            // 战利品容器（注册表距离判定）
            foreach (var container in LootContainerRegistry.All)
            {
                if (container == null) continue;
                float sqr = (container.transform.position.ToXZ() - aimPos).sqrMagnitude;
                if (sqr <= LOOT_PROBE_RADIUS * LOOT_PROBE_RADIUS && sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    lootPos = container.transform.position.ToXZ();
                    found = true;
                }
            }
            return found;
        }

        #endregion

        #region 世界内标记（占位视觉）

        private void CreateMarker(PingType type, Vector2 pos)
        {
            _marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _marker.name = $"PingMarker_{type}";
            var col = _marker.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            _markerRenderer = _marker.GetComponent<Renderer>();
            _markerRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _markerRenderer.material.color = type switch
            {
                PingType.Attack => AttackColor,
                PingType.Loot => LootColor,
                _ => MoveColor,
            };

            _marker.transform.position = pos.ToWorld(1.2f);
            _marker.transform.localScale = Vector3.one * 0.5f;
        }

        private void UpdateMarkerVisual()
        {
            if (_marker == null) return;

            // Attack 标记跟随目标头顶
            if (_activeType == PingType.Attack && _markerFollowTarget != null)
            {
                _marker.transform.position = _markerFollowTarget.position + Vector3.up * 1.6f;
            }

            // 脉冲缩放
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.15f;
            _marker.transform.localScale = Vector3.one * (0.5f * pulse);

            // 屏幕对齐 billboard
            if (_mainCamera == null)
            {
                _mainCamera = CameraSystem3D.Instance != null ? CameraSystem3D.Instance.GetMainCamera() : Camera.main;
            }
            if (_mainCamera != null)
            {
                _marker.transform.rotation = _mainCamera.transform.rotation;
            }
        }

        #endregion
    }
}
