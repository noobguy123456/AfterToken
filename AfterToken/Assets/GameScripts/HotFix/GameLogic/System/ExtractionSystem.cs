using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 撤离系统（搜打撤核心规则）。
    /// 玩家在撤离点（<see cref="ExtractionPointEntity"/>）范围内停留触发倒计时
    /// （时长 = TbLevel.extractionTime，默认 10s），圈内有存活敌人时倒计时暂停，
    /// 玩家离开范围或死亡则倒计时重置；倒计时归零且玩家存活时执行撤离结算
    /// （复用 <see cref="GameLogic.Portal.PortalSystem.ExtractToBase"/>，回经营场景）。
    /// 由 ProcedureBattle 挂载到 BattleRoot。
    /// </summary>
    public class ExtractionSystem : MonoBehaviour
    {
        public static ExtractionSystem Instance { get; private set; }

        private ExtractionPointEntity _activePoint;
        private float _remaining;
        private float _total = 10f;
        private bool _extracting;

        /// <summary>
        /// 是否正在撤离倒计时（玩家在撤离圈内）。
        /// </summary>
        public bool IsCountingDown => _activePoint != null;

        /// <summary>
        /// 倒计时是否被圈内存活敌人暂停。
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 剩余倒计时秒数。
        /// </summary>
        public float RemainingSeconds => _remaining;

        /// <summary>
        /// 倒计时总时长秒数（来自 TbLevel.extractionTime）。
        /// </summary>
        public float TotalSeconds => _total;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            var cfg = LevelConfigMgr.Instance?.Get(BattleContext.CurrentLevelId);
            if (cfg != null && cfg.extractionTime > 0f)
            {
                _total = cfg.extractionTime;
            }
        }

        private void Update()
        {
            if (_extracting) return;

            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null || player.IsDead)
            {
                ResetCountdown();
                return;
            }

            var point = FindContainingPoint(player.transform.position);
            if (point == null)
            {
                // 玩家不在任何撤离圈内：重置倒计时
                ResetCountdown();
                return;
            }

            if (_activePoint != point)
            {
                _activePoint = point;
                _remaining = _total;
                Log.Info($"[ExtractionSystem] 进入撤离圈，开始倒计时 {_total:F0}s");
            }

            IsPaused = HasAliveEnemyInRange(point);
            if (IsPaused)
            {
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                Extract();
            }
        }

        private void ResetCountdown()
        {
            _activePoint = null;
            IsPaused = false;
        }

        /// <summary>
        /// 玩家当前所在的撤离点（XZ 平面距离判定，忽略高度）。
        /// </summary>
        private static ExtractionPointEntity FindContainingPoint(Vector3 playerPos)
        {
            var points = ExtractionPointEntity.Instances;
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p == null) continue;
                Vector3 d = playerPos - p.transform.position;
                d.y = 0f;
                if (d.magnitude <= p.Radius)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>
        /// 撤离圈内是否有存活敌人（有则暂停倒计时）。
        /// </summary>
        private static bool HasAliveEnemyInRange(ExtractionPointEntity point)
        {
            Vector3 center = point.transform.position;
            foreach (var enemy in EnemyRegistry.All)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 d = enemy.transform.position - center;
                d.y = 0f;
                if (d.magnitude <= point.Radius)
                {
                    return true;
                }
            }
            return false;
        }

        private void Extract()
        {
            _extracting = true;
            _activePoint = null;
            IsPaused = false;
            Log.Info("[ExtractionSystem] 倒计时完成，执行撤离");
            // 转场期间玩家死亡则中止撤离（与传送门一致的中止判定）
            Portal.PortalTransitionMgr.PlayAsync(
                "fade_to_gray",
                0.5f,
                IsPlayerDead,
                null,
                Portal.PortalSystem.ExtractToBase).Forget();
        }

        private static bool IsPlayerDead()
        {
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            return player == null || player.IsDead;
        }
    }
}
