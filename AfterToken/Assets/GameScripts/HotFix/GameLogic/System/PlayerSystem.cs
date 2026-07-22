using System.Threading;
using Cysharp.Threading.Tasks;
using GameConfig;
using GameLogic.Portal;
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
        [SerializeField] private int _playerConfigId = 1;

        private PlayerEntity _playerEntity;
        private IFsm<PlayerEntity> _playerFsm;
        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        private int _maxHp;
        private int _currentHp;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;

        private int _maxStamina;
        private int _currentStamina;
        private int _dodgeStaminaCost;
        private float _staminaRecoveryRate;

        public int CurrentStamina => _currentStamina;
        public int MaxStamina => _maxStamina;

        /// <summary>
        /// 玩家出生位置（世界坐标）。
        /// </summary>
        public Vector2 SpawnPosition => _spawnPoint != null ? (Vector2)_spawnPoint.position : Vector2.zero;

        private GameConfig.cfg.Player _playerConfig;

        #region Fallback Defaults

        // 当 TbPlayer 配置缺失时的兜底值。正常流程应始终能从配置表读取，此处兜底仅用于容错。
        private const int DEFAULT_MAX_HP = 100;
        private const int DEFAULT_MAX_STAMINA = 100;
        private const int DEFAULT_DODGE_STAMINA_COST = 25;
        private const float DEFAULT_STAMINA_RECOVERY_RATE = 30f;
        private const float DEFAULT_MOVE_SPEED = 5f;
        private const float DEFAULT_DODGE_SPEED = 15f;
        private const float DEFAULT_DODGE_DURATION = 0.4f;

        #endregion

        /// <summary>
        /// 设置玩家最大血量（需在创建玩家前调用）。
        /// </summary>
        public void SetMaxHp(int maxHp)
        {
            _maxHp = maxHp;
            _currentHp = maxHp;
        }

        /// <summary>
        /// 设置玩家最大体力（需在创建玩家前调用）。
        /// </summary>
        public void SetMaxStamina(int maxStamina)
        {
            _maxStamina = maxStamina;
            _currentStamina = maxStamina;
        }

        private void Awake()
        {
            Instance = this;

            _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnMoveInput, OnMoveInput);
            _eventMgr.AddEvent<Vector2>(IBattleInputEvent_Event.OnAimInput, OnAimInput);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnReloadPressed, OnReloadPressed);
            _eventMgr.AddEvent(IBattleInputEvent_Event.OnDodgePressed, OnDodgePressed);
            _eventMgr.AddEvent<int, Vector2>(IPlayerEvent_Event.OnPlayerDamaged, HandlePlayerDamaged);
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
            CreatePlayerAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTask CreatePlayerAsync(CancellationToken cancellationToken)
        {
            var go = await GameModule.Resource.LoadGameObjectAsync("Player", _spawnPoint, cancellationToken);
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

            LoadPlayerConfig();
            ApplyPlayerConfig(_playerEntity);

            _playerEntity.ResetEntity();
            _playerEntity.Context = new PlayerStateContext();

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

            WeaponSystem.Instance?.SetOwner(_playerEntity);
            CameraSystem.Instance?.SetTarget(go.transform);

            GameEvent.Get<IPlayerEvent>().OnPlayerCreated(go.transform.position);
            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
            GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);

            if (PortalPlayerState.HasSavedState)
            {
                PortalPlayerState.Restore(this);
            }
        }

        private void OnMoveInput(Vector2 direction)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            _playerEntity.SetMoveDirection(direction);
            if (_playerEntity.Context != null)
            {
                _playerEntity.Context.MoveInput = direction;
            }
        }

        private void OnAimInput(Vector2 worldPosition)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;
            _playerEntity.SetAimPosition(worldPosition);
            if (_playerEntity.Context != null)
            {
                _playerEntity.Context.AimInput = worldPosition;
            }
        }

        private void OnReloadPressed()
        {
            if (_playerEntity?.Context == null) return;
            _playerEntity.Context.ReloadPressed = true;
        }

        private void OnDodgePressed()
        {
            if (_playerEntity?.Context == null) return;
            _playerEntity.Context.DodgePressed = true;
        }

        private void Update()
        {
            if (_playerEntity?.Context == null) return;

            // 同步当前武器引用到黑板
            _playerEntity.Context.CurrentWeapon = WeaponSystem.Instance?.CurrentWeapon;

            // 更新黑板意图（由 Driver 根据输入和当前状态计算）
            PlayerStateMachineDriver.Instance.UpdateContext(_playerEntity.Context);

            // 体力恢复：未死亡且未闪避时恢复；仅在数值实际变化时派发事件，避免恢复期间每帧无效派发。
            if (!_playerEntity.IsDead && !_playerEntity.IsDodging && _currentStamina < _maxStamina)
            {
                int newStamina = _currentStamina + Mathf.RoundToInt(_staminaRecoveryRate * Time.deltaTime);
                if (newStamina > _maxStamina) newStamina = _maxStamina;
                if (newStamina != _currentStamina)
                {
                    _currentStamina = newStamina;
                    GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);
                }
            }

            // 同步能否闪避到黑板
            _playerEntity.Context.CanDodge = _currentStamina >= _dodgeStaminaCost && _playerEntity.Context.CanMove;

            // 更新移动速度
            if (!_playerEntity.IsDead && _playerFsm?.CurrentState is not PlayerDodgeState)
            {
                float multiplier = WeaponSystem.Instance?.GetCurrentMoveSpeedMultiplier() ?? 1f;
                _playerEntity.MoveSpeed = _playerEntity.BaseMoveSpeed * multiplier;
            }
        }

        /// <summary>
        /// 处理玩家受伤。
        /// </summary>
        private void HandlePlayerDamaged(int damage, Vector2 hitDirection)
        {
            if (_playerEntity == null || _playerEntity.IsDead) return;

            _currentHp -= damage;
            if (_currentHp < 0) _currentHp = 0;

            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);

            // 计算伤害来源相对玩家朝向的角度（0-360，0 为前方）
            float playerAngle = Mathf.Atan2(_playerEntity.transform.up.y, _playerEntity.transform.up.x) * Mathf.Rad2Deg;
            float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            float relativeAngle = (hitAngle - playerAngle + 360f) % 360f;

            GameEvent.Get<IHitFeedbackEvent>().OnDamageIndicator(relativeAngle, 1f);
            GameEvent.Get<ICameraEvent>().OnCameraShake(0.2f, 0.1f);

            if (_currentHp <= 0 && _playerEntity != null)
            {
                _playerEntity.Context.IsDead = true;
                _playerEntity.SetDead();
            }
        }

        /// <summary>
        /// 消耗体力。
        /// </summary>
        public void ConsumeStamina(int amount)
        {
            if (amount <= 0) return;
            _currentStamina -= amount;
            if (_currentStamina < 0) _currentStamina = 0;
            GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);
        }

        /// <summary>
        /// 获取闪避所需体力。
        /// </summary>
        public int GetDodgeStaminaCost() => _dodgeStaminaCost;

        /// <summary>
        /// 加载玩家配置。
        /// </summary>
        private void LoadPlayerConfig()
        {
            try
            {
                _playerConfig = ConfigSystem.Instance.Tables.TbPlayer.Get(_playerConfigId);
            }
            catch (System.Exception e)
            {
                Log.Warning($"[PlayerSystem] 读取玩家配置 {_playerConfigId} 失败，使用默认值: {e.Message}");
                _playerConfig = null;
            }

            // 关卡可能覆盖血量
            var levelConfig = LevelConfigMgr.Instance.Get(BattleContext.CurrentLevelId);
            int levelMaxHp = levelConfig?.playerMaxHp ?? 0;

            if (_playerConfig != null)
            {
                _maxHp = levelMaxHp > 0 ? levelMaxHp : _playerConfig.MaxHp;
                _maxStamina = _playerConfig.MaxStamina;
                _dodgeStaminaCost = _playerConfig.DodgeStaminaCost;
                _staminaRecoveryRate = _playerConfig.StaminaRecoveryRate;
            }
            else
            {
                _maxHp = levelMaxHp > 0 ? levelMaxHp : DEFAULT_MAX_HP;
                _maxStamina = DEFAULT_MAX_STAMINA;
                _dodgeStaminaCost = DEFAULT_DODGE_STAMINA_COST;
                _staminaRecoveryRate = DEFAULT_STAMINA_RECOVERY_RATE;
            }

            _currentHp = _maxHp;
            _currentStamina = _maxStamina;
        }

        /// <summary>
        /// 将配置应用到玩家实体。
        /// </summary>
        private void ApplyPlayerConfig(PlayerEntity entity)
        {
            if (entity == null) return;

            float moveSpeed = _playerConfig?.MoveSpeed ?? DEFAULT_MOVE_SPEED;
            float dodgeSpeed = _playerConfig?.DodgeSpeed ?? DEFAULT_DODGE_SPEED;
            float dodgeDuration = _playerConfig?.DodgeDuration ?? DEFAULT_DODGE_DURATION;

            entity.BaseMoveSpeed = moveSpeed;
            entity.MoveSpeed = moveSpeed;
            entity.DodgeSpeed = dodgeSpeed;
            entity.DodgeDuration = dodgeDuration;
        }

        /// <summary>
        /// 获取玩家实体。
        /// </summary>
        public PlayerEntity GetPlayerEntity() => _playerEntity;

        /// <summary>
        /// 恢复玩家血量与体力（用于跨场景状态保留）。
        /// </summary>
        public void RestoreHpAndStamina(int hp, int maxHp, int stamina, int maxStamina)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _currentHp = Mathf.Clamp(hp, 0, _maxHp);
            _maxStamina = Mathf.Max(1, maxStamina);
            _currentStamina = Mathf.Clamp(stamina, 0, _maxStamina);

            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
            GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// GM：设置当前血量。
        /// </summary>
        public void GM_SetHp(int hp)
        {
            _currentHp = Mathf.Clamp(hp, 0, _maxHp);
            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
        }

        /// <summary>
        /// GM：设置最大血量并回满。
        /// </summary>
        public void GM_SetMaxHp(int maxHp)
        {
            _maxHp = Mathf.Max(1, maxHp);
            _currentHp = _maxHp;
            GameEvent.Get<IPlayerEvent>().OnHpChanged(_currentHp, _maxHp);
        }

        /// <summary>
        /// GM：设置当前体力。
        /// </summary>
        public void GM_SetStamina(int stamina)
        {
            _currentStamina = Mathf.Clamp(stamina, 0, _maxStamina);
            GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);
        }

        /// <summary>
        /// GM：设置最大体力并回满。
        /// </summary>
        public void GM_SetMaxStamina(int maxStamina)
        {
            _maxStamina = Mathf.Max(1, maxStamina);
            _currentStamina = _maxStamina;
            GameEvent.Get<IPlayerEvent>().OnStaminaChanged(_currentStamina, _maxStamina);
        }
#endif

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
            sr.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            sr.color = Color.cyan;
            sr.sortingOrder = 5;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;

            return go;
        }
    }
}
