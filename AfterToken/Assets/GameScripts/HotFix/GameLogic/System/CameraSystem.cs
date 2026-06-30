using UnityEngine;
using TEngine;

namespace GameLogic
{
    /// <summary>
    /// 相机跟随模式。
    /// </summary>
    public enum CameraFollowMode
    {
        /// <summary>
        /// 硬跟随：相机位置与玩家位置完全同步，无延迟。
        /// </summary>
        Hard,

        /// <summary>
        /// 指数平滑：使用无最大速度上限的指数衰减平滑。
        /// </summary>
        Exponential,

        /// <summary>
        /// 平滑阻尼：使用 SmoothDamp，适合希望明显平滑感的场景。
        /// </summary>
        SmoothDamp,
    }

    /// <summary>
    /// 相机系统。
    /// 负责平滑跟随玩家、FOV、震动、狙击镜相机、伤害方向数据。
    /// </summary>
    public class CameraSystem : MonoBehaviour
    {
        public static CameraSystem Instance { get; private set; }

        [Header("跟随")]
        [SerializeField] private CameraFollowMode _followMode = CameraFollowMode.Hard;
        [SerializeField] private float _smoothTime = 0.08f;
        [SerializeField] private float _lookAheadFactor = 0f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 0, -10f);

        [Header("瞄准 FOV")]
        [SerializeField] private float _defaultFov = 60f;
        [SerializeField] private float _fovSmoothTime = 0.15f;

        private float _defaultOrthographicSize = 4f;
        private float _currentOrthographicSizeVelocity;

        [Header("震动")]
        [SerializeField] private float _shakeDamping = 5f;

        [Header("狙击镜")]
        [SerializeField] private int _scopeRenderSize = 512;

        private Camera _mainCamera;
        private Camera _scopeCamera;
        private RenderTexture _scopeRenderTexture;

        private Vector3 _velocity;

        private float _targetFov;
        private float _currentFovVelocity;

        private float _shakeMagnitude;
        private float _shakeDuration;

        private float _damageIndicatorAngle = -1f;
        private float _damageIndicatorDuration;
        private float _damageIndicatorTimer;

        private readonly GameEventMgr _eventMgr = new GameEventMgr();

        public RenderTexture ScopeRenderTexture => _scopeRenderTexture;
        public float DamageIndicatorAngle => _damageIndicatorAngle;
        public bool IsDamageIndicatorActive => _damageIndicatorTimer < _damageIndicatorDuration;

        private void Awake()
        {
            Instance = this;

            _mainCamera = GetComponent<Camera>();
            _targetFov = _defaultFov;
            _defaultOrthographicSize = _mainCamera != null ? _mainCamera.orthographicSize : 4f;

            CreateScopeCamera();

            _eventMgr.AddEvent<Vector3>(IPlayerEvent_Event.OnPlayerPositionChanged, OnPlayerPositionChanged);
            _eventMgr.AddEvent<float, float>(ICameraEvent_Event.OnCameraShake, OnCameraShake);
            _eventMgr.AddEvent<float>(ICameraEvent_Event.OnAimFovChanged, OnAimFovChanged);
            _eventMgr.AddEvent<float, float>(IHitFeedbackEvent_Event.OnDamageIndicator, OnDamageIndicator);
        }

        private void OnDestroy()
        {
            _eventMgr.Clear();
            Instance = null;

            if (_scopeRenderTexture != null)
            {
                _scopeRenderTexture.Release();
                Destroy(_scopeRenderTexture);
            }
        }

        private void CreateScopeCamera()
        {
            if (_mainCamera == null) return;

            var go = new GameObject("ScopeCamera");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _scopeCamera = go.AddComponent<Camera>();
            _scopeCamera.CopyFrom(_mainCamera);
            _scopeCamera.fieldOfView = 25f;
            _scopeCamera.orthographicSize = _defaultOrthographicSize * (25f / _defaultFov);
            _scopeCamera.clearFlags = CameraClearFlags.SolidColor;
            _scopeCamera.backgroundColor = Color.black;

            _scopeRenderTexture = new RenderTexture(_scopeRenderSize, _scopeRenderSize, 16);
            _scopeRenderTexture.Create();
            _scopeCamera.targetTexture = _scopeRenderTexture;
            _scopeCamera.enabled = false;
        }

        private void OnPlayerPositionChanged(Vector3 position)
        {
            // 位置事件保留给其他系统使用，相机跟随直接在 LateUpdate 读取玩家 Transform。
        }

        private void OnCameraShake(float magnitude, float duration)
        {
            _shakeMagnitude = Mathf.Max(_shakeMagnitude, magnitude);
            _shakeDuration = duration;
        }

        private void OnAimFovChanged(float targetFov)
        {
            _targetFov = targetFov;

            // 狙击枪开镜时启用狙击镜相机
            bool isSniperScope = Mathf.Abs(targetFov - 25f) < 0.1f;
            if (_scopeCamera != null)
            {
                _scopeCamera.enabled = isSniperScope;
            }
        }

        private void OnDamageIndicator(float angle, float duration)
        {
            _damageIndicatorAngle = angle;
            _damageIndicatorDuration = duration;
            _damageIndicatorTimer = 0;
        }

        private void Update()
        {
            // FOV / OrthographicSize 平滑
            if (_mainCamera != null)
            {
                if (_mainCamera.orthographic)
                {
                    float targetSize = _defaultOrthographicSize * (_targetFov / _defaultFov);
                    _mainCamera.orthographicSize = Mathf.SmoothDamp(
                        _mainCamera.orthographicSize,
                        targetSize,
                        ref _currentOrthographicSizeVelocity,
                        _fovSmoothTime);
                }
                else
                {
                    _mainCamera.fieldOfView = Mathf.SmoothDamp(
                        _mainCamera.fieldOfView,
                        _targetFov,
                        ref _currentFovVelocity,
                        _fovSmoothTime);
                }
            }

            // 震动衰减
            if (_shakeDuration > 0)
            {
                _shakeDuration -= Time.deltaTime;
                _shakeMagnitude = Mathf.Max(0, _shakeMagnitude - _shakeDamping * Time.deltaTime);
            }
            else
            {
                _shakeMagnitude = 0;
            }

            // 伤害指示器计时
            if (_damageIndicatorTimer < _damageIndicatorDuration)
            {
                _damageIndicatorTimer += Time.deltaTime;
            }
            else
            {
                _damageIndicatorAngle = -1f;
            }
        }

        private void LateUpdate()
        {
            var player = PlayerSystem.Instance?.GetPlayerEntity();
            if (player == null) return;

            Vector3 playerPos = player.transform.position;
            Vector3 targetPosition = playerPos + _offset;

            // 可选：根据玩家速度加入提前量，让相机略微超前于玩家移动方向
            if (_lookAheadFactor > 0f && player.TryGetComponent<Rigidbody2D>(out var playerRb))
            {
                targetPosition += (Vector3)playerRb.linearVelocity * _lookAheadFactor;
            }

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeMagnitude > 0.001f)
            {
                shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
                shakeOffset.z = 0;
            }

            Vector3 nextPosition = _followMode switch
            {
                CameraFollowMode.Hard => targetPosition,
                CameraFollowMode.Exponential => ExponentialFollow(transform.position, targetPosition, _smoothTime),
                CameraFollowMode.SmoothDamp => Vector3.SmoothDamp(
                    transform.position,
                    targetPosition,
                    ref _velocity,
                    _smoothTime),
                _ => targetPosition
            };

            transform.position = nextPosition + shakeOffset;

            // 狙击镜相机跟随主相机位置与朝向
            if (_scopeCamera != null && _scopeCamera.enabled)
            {
                _scopeCamera.transform.position = transform.position;
                _scopeCamera.transform.rotation = transform.rotation;
            }
        }

        /// <summary>
        /// 指数平滑跟随：无最大速度上限，快速移动时更跟手。
        /// </summary>
        private Vector3 ExponentialFollow(Vector3 current, Vector3 target, float smoothTime)
        {
            if (smoothTime <= 0f) return target;
            float t = 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
            return Vector3.Lerp(current, target, t);
        }
    }
}
