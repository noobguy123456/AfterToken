using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 玩家系统。
    /// 负责创建玩家、处理输入、管理玩家状态机。
    /// </summary>
    public class PlayerSystem : MonoBehaviour
    {
        public static PlayerSystem Instance { get; private set; }

        [SerializeField] private Transform _spawnPoint;

        private PlayerEntity _playerEntity;
        private IFsm<PlayerEntity> _playerFsm;
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private int _maxHp = 100;
        private int _currentHp = 100;

        /// <summary>
        /// 设置玩家最大血量（需在创建玩家前调用）。
        /// </summary>
        public void SetMaxHp(int maxHp)
        {
            _maxHp = maxHp;
            _currentHp = maxHp;
        }

        private void Awake()
        {
            Instance = this;

            _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnMoveInput, OnMoveInput);
            _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnAimInput, OnAimInput);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnReloadPressed, OnReloadPressed);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnDodgePressed, OnDodgePressed);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();

            if (_playerFsm != null)
            {
                GameModule.Fsm.DestroyFsm<PlayerEntity>("PlayerFsm");
                _playerFsm = null;
            }
        }

        private void Start()
        {
            CreatePlayerAsync().Forget();
        }

        private async UniTask CreatePlayerAsync()
        {
            var go = await GameModule.Resource.LoadGameObjectAsync("Player", _spawnPoint);
            if (go == null)
            {
                Log.Warning("[PlayerSystem] 加载 Player Prefab 失败，使用占位玩家");
                go = CreatePlaceholderPlayer();
            }

            _playerEntity = go.GetComponent<PlayerEntity>();
            if (_playerEntity == null)
            {
                _playerEntity = go.AddComponent<PlayerEntity>();
            }

            _playerEntity.ResetEntity();

            _playerFsm = GameModule.Fsm.CreateFsm<PlayerEntity>(
                "PlayerFsm",
                _playerEntity,
                new PlayerIdleState(),
                new PlayerMoveState(),
                new PlayerDodgeState(),
                new PlayerReloadState(),
                new PlayerDeadState()
            );

            _playerFsm.Start<PlayerIdleState>();

            GameEvent.Get<IPlayerEvent>().OnPlayerCreated(go.transform.position);
            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
        }

        private void OnMoveInput(Vector2 direction)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            _playerEntity.SetMoveDirection(direction);
        }

        private void OnAimInput(Vector2 worldPosition)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            _playerEntity.SetAimPosition(worldPosition);
        }

        private void OnReloadPressed()
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            if (!CanSwitchFromCurrentState()) return;

            _playerFsm?.SetData("NextState", "Reload");
        }

        private void OnDodgePressed()
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            if (!CanSwitchFromCurrentState()) return;

            _playerFsm?.SetData("NextState", "Dodge");
        }

        private void Update()
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            if (_playerFsm?.CurrentState is PlayerDodgeState) return;

            float multiplier = WeaponSystem.Instance?.GetCurrentMoveSpeedMultiplier() ?? 1f;
            _playerEntity.MoveSpeed = _playerEntity.BaseMoveSpeed * multiplier;
        }

        private bool CanSwitchFromCurrentState()
        {
            if (_playerFsm == null) return false;
            var current = _playerFsm.CurrentState;
            return current is not PlayerDodgeState &&
                   current is not PlayerReloadState &&
                   current is not PlayerDeadState;
        }

        /// <summary>
        /// 玩家受伤。
        /// </summary>
        public void TakeDamage(int damage, Vector2 hitDirection)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;

            _currentHp -= damage;
            if (_currentHp < 0) _currentHp = 0;

            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
            GameEvent.Get<IPlayerEvent>().OnPlayerDamaged(damage, hitDirection);

            // 计算伤害来源相对玩家朝向的角度（0-360，0 为前方）
            float playerAngle = Mathf.Atan2(_playerEntity.transform.up.y, _playerEntity.transform.up.x) * Mathf.Rad2Deg;
            float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            float relativeAngle = (hitAngle - playerAngle + 360f) % 360f;

            GameEvent.Get<IHitFeedbackEvent>().OnDamageIndicator(relativeAngle, 1f);
            GameEvent.Get<ICameraEvent>().OnCameraShake(0.2f, 0.1f);

            if (_currentHp <= 0)
            {
                _playerFsm?.SetData("NextState", "Dead");
            }
        }

        /// <summary>
        /// 获取玩家实体。
        /// </summary>
        public PlayerEntity GetPlayerEntity() => _playerEntity;

        /// <summary>
        /// 获取玩家位置。
        /// </summary>
        public Vector3 GetPlayerPosition() => _playerEntity != null ? _playerEntity.transform.position : Vector3.zero;

        private GameObject CreatePlaceholderPlayer()
        {
            var go = new GameObject("Player_Placeholder");
            go.tag = "Player";
            go.layer = LayerMask.NameToLayer("Player");
            if (_spawnPoint != null)
            {
                go.transform.position = _spawnPoint.position;
            }

            var sr = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
            sr.color = Color.cyan;
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;

            return go;
        }
    }
}
