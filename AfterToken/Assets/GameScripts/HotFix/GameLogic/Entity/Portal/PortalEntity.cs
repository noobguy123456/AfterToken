using TEngine;
using UnityEngine;

namespace GameLogic.Portal
{
    /// <summary>
    /// 传送门实体。
    /// 挂载在场景中的 Portal Prefab 上，负责触发区检测与交互反馈。
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class PortalEntity : MonoBehaviour
    {
        [SerializeField] private int _configId;
        [SerializeField] private SpriteRenderer _visualRenderer;

        private PortalConfig _config;
        private bool _isActivated;
        private bool _playerInside;

        public int ConfigId => _configId;
        public PortalConfig Config => _config;
        public bool IsActivated => _isActivated;
        public bool PlayerInside => _playerInside;

        private void Awake()
        {
            var collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            EnsureVisualRenderer();
            UpdateVisual();
        }

private void EnsureVisualRenderer()
        {
            if (_visualRenderer != null) return;
            _visualRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_visualRenderer != null) return;

            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            visualGo.transform.localScale = Vector3.one * 1.5f;
            _visualRenderer = visualGo.AddComponent<SpriteRenderer>();
            _visualRenderer.sprite = PlaceholderSpriteProvider.GetWhiteSprite16();
            _visualRenderer.sortingOrder = 1;
        }

        /// <summary>
        /// 创建或更新传送门目的地标签。
        /// </summary>
        private void EnsureDestinationLabel()
        {
            var labelTransform = transform.Find("DestinationLabel");
            TextMeshPro label;
            if (labelTransform == null)
            {
                var labelGo = new GameObject("DestinationLabel");
                labelGo.transform.SetParent(transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                label = labelGo.AddComponent<TextMeshPro>();
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 3f;
                label.color = Color.white;
                label.font = TMPFontProvider.DefaultFont;
            }
            else
            {
                label = labelTransform.GetComponent<TextMeshPro>();
            }

            if (label != null)
            {
                label.text = GetDestinationText();
            }
        }

        /// <summary>
        /// 根据地配置生成目的地显示文本。
        /// </summary>
        private string GetDestinationText()
        {
            if (_config == null) return "???";
            switch (_config.portalType)
            {
                case PortalType.ReturnToLobby:
                    return "返回大厅";
                case PortalType.NextLevel:
                    return $"关卡 {_config.targetLevelId}";
                case PortalType.CustomScene:
                    return string.IsNullOrEmpty(_config.targetSceneName)
                        ? "未知场景"
                        : _config.targetSceneName.Replace("BattleScene_", "关卡 ");
                default:
                    return _config.portalType;
            }
        }


        /// <summary>
        /// 初始化传送门配置。
        /// </summary>
        public void Initialize(PortalConfig config)
        {
            _config = config;
            _isActivated = false;
            _playerInside = false;
            EnsureDestinationLabel();
            UpdateVisual();
        }

        /// <summary>
        /// 激活传送门。
        /// </summary>
        public void Activate()
        {
            if (_isActivated) return;
            _isActivated = true;
            EnsureDestinationLabel();
            UpdateVisual();
            if (_config != null)
            {
                GameEvent.Get<IPortalEvent>()?.OnPortalActivated(_config.id);
            }
        }

        /// <summary>
        /// 尝试与传送门交互（触发传送）。
        /// </summary>
        public void TryInteract()
        {
            if (!_isActivated || _config == null) return;

            GameEvent.Get<IPortalEvent>()?.OnPortalTriggered(_config.id, _config.portalType, _config.targetSceneName);
            PortalSystem.Instance?.ExecuteTransition(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = true;
            PortalSystem.Instance?.OnPlayerEnteredPortal(this);
            if (_config != null)
            {
                GameEvent.Get<IPortalEvent>()?.OnPortalEntered(_config.id);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            _playerInside = false;
            PortalSystem.Instance?.OnPlayerExitedPortal(this);
            if (_config != null)
            {
                GameEvent.Get<IPortalEvent>()?.OnPortalExited(_config.id);
            }
        }

        private void UpdateVisual()
        {
            if (_visualRenderer == null) return;
            _visualRenderer.color = _isActivated
                ? new Color(0.4f, 0.8f, 1f, 1f)
                : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
    }
}
